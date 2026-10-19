using System;
using System.Collections.Generic;
using System.Linq;
using CourtOfNi.Core.Cards;
using CourtOfNi.Core.Chat;
using CourtOfNi.Core.Events;
using CourtOfNi.Core.Match;
using CourtOfNi.Core.Missions;
using CourtOfNi.Core.Phases;
using CourtOfNi.Core.Players;
using CourtOfNi.Core.Roles;
using CourtOfNi.Data;
using Unity.Netcode;
using UnityEngine;

namespace CourtOfNi.Net
{
    /// <summary>
    /// 호스트 권한(host-authoritative) 게임 진행기. 씬에 NetworkObject와 함께 하나만 둔다.
    /// - 서버: GameSession(규칙 코드)을 돌리고, 나온 이벤트를 RPC로 필요한 사람에게만 보낸다.
    /// - 클라이언트: RPC를 받아 상태를 캐시하고 GameEventBus로 발행해서 연출이 반응하게 한다.
    /// 플레이어 id는 NGO의 clientId를 int로 바꾼 값이다.
    /// </summary>
    public sealed class CourtNetworkGame : NetworkBehaviour
    {
        [SerializeField] GameRulesetAsset ruleset;

        public static CourtNetworkGame Instance { get; private set; }

        public NetworkList<NetPlayer> Players { get; private set; }
        public readonly NetworkVariable<MatchPhase> Phase = new NetworkVariable<MatchPhase>(MatchPhase.NotStarted);
        public readonly NetworkVariable<int> Round = new NetworkVariable<int>();
        public readonly NetworkVariable<int> ActivePlayerId = new NetworkVariable<int>(-1);
        /// <summary>현재 페이즈가 끝나는 서버 시각. 0이면 타이머 없음.</summary>
        public readonly NetworkVariable<double> PhaseEndsAt = new NetworkVariable<double>();

        GameSession _session;

        // 클라이언트 캐시 (호스트 포함 모든 클라이언트)
        readonly List<EvidenceCard> _caseEvidence = new List<EvidenceCard>();
        readonly Dictionary<int, List<EvidenceCard>> _evidenceByPlayer = new Dictionary<int, List<EvidenceCard>>();
        readonly List<MissionDealtEvent> _myMissions = new List<MissionDealtEvent>();

        public int LocalPlayerId => (int)NetworkManager.LocalClientId;
        public RoleAssignment Assignment { get; private set; }
        public Role LocalRole => Assignment?.RoleOf(LocalPlayerId) ?? Role.None;
        public string CaseDescription { get; private set; }
        public IReadOnlyList<EvidenceCard> CaseEvidence => _caseEvidence;
        public IReadOnlyList<MissionDealtEvent> MyMissions => _myMissions;
        public bool IsGameRunning => Phase.Value != MatchPhase.NotStarted;
        public bool IsGameOver { get; private set; }
        public bool CanHostStartNextMatch => IsServer && _session != null && _session.CanStartNextMatch && _session.MatchesPlayed > 0;

        public IReadOnlyList<EvidenceCard> EvidenceOf(int playerId) =>
            _evidenceByPlayer.TryGetValue(playerId, out var list) ? list : (IReadOnlyList<EvidenceCard>)Array.Empty<EvidenceCard>();

        public float SecondsRemaining =>
            PhaseEndsAt.Value <= 0 ? 0f : Mathf.Max(0f, (float)(PhaseEndsAt.Value - NetworkManager.ServerTime.Time));

        public string NameOf(int playerId)
        {
            foreach (var p in Players)
                if (p.Id == playerId) return p.Name.ToString();
            return $"#{playerId}";
        }

        void Awake()
        {
            Players = new NetworkList<NetPlayer>();
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            if (IsServer) NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager != null) NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (IsServer && _session != null) _session.Tick(Time.deltaTime);
        }

        // ───────────────────────── 클라이언트 → 서버 요청 ─────────────────────────

        /// <summary>접속 후 자기 이름과 속성(나이 등)을 등록한다. 게임 시작 전에만 받는다.</summary>
        [Rpc(SendTo.Server)]
        public void SetProfileRpc(string displayName, float age, RpcParams rpcParams = default)
        {
            if (_session != null) return;
            var id = (int)rpcParams.Receive.SenderClientId;
            var entry = new NetPlayer { Id = id, Name = Truncate(displayName, 15), Age = age };
            var index = IndexOfPlayer(id);
            if (index >= 0) Players[index] = entry;
            else Players.Add(entry);
        }

        [Rpc(SendTo.Server)]
        public void StartGameRpc(RpcParams rpcParams = default)
        {
            if (_session != null || rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId) return;
            if (ruleset == null)
            {
                Debug.LogError("[CourtNetworkGame] GameRulesetAsset이 지정되지 않았습니다.");
                return;
            }
            if (Players.Count < RoleRotation.MinimumPlayers)
            {
                Debug.LogWarning($"[CourtNetworkGame] 최소 {RoleRotation.MinimumPlayers}명이 필요합니다.");
                return;
            }

            var seats = new List<PlayerInfo>();
            foreach (var p in Players)
            {
                var info = new PlayerInfo(p.Id, p.Name.ToString());
                info.Attributes["age"] = p.Age;
                seats.Add(info);
            }

            _session = new GameSession(seats, ruleset.ToMatchRules(), ruleset.CreateEvidenceCards(),
                ruleset.CreateMissions(), Environment.TickCount, ruleset.Laps);
            _session.EventRaised += OnServerEvent;
            _session.StartNextMatch();
        }

        [Rpc(SendTo.Server)]
        public void NextMatchRpc(RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId) return;
            if (_session != null && _session.CanStartNextMatch) _session.StartNextMatch();
        }

        [Rpc(SendTo.Server)]
        public void RequestAdvanceRpc(RpcParams rpcParams = default) =>
            _session?.CurrentMatch?.RequestAdvance((int)rpcParams.Receive.SenderClientId);

        [Rpc(SendTo.Server)]
        public void DefineCaseRpc(string description, RpcParams rpcParams = default) =>
            _session?.CurrentMatch?.DefineCase((int)rpcParams.Receive.SenderClientId, Truncate(description, 300));

        [Rpc(SendTo.Server)]
        public void CastVoteRpc(int defendantId, RpcParams rpcParams = default) =>
            _session?.CurrentMatch?.CastVote((int)rpcParams.Receive.SenderClientId, defendantId);

        [Rpc(SendTo.Server)]
        public void SendChatRpc(ChatChannel channel, string text, RpcParams rpcParams = default) =>
            _session?.CurrentMatch?.SendChat((int)rpcParams.Receive.SenderClientId, channel, text);

        // ───────────────────────── 서버: 규칙 이벤트 → RPC ─────────────────────────

        void OnServerEvent(IGameEvent gameEvent)
        {
            switch (gameEvent)
            {
                case MatchStartedEvent e:
                    MatchStartedRpc(e.MatchIndex, e.TotalMatches, e.Assignment.JudgeId,
                        e.Assignment.DefendantIds.ToArray(), e.Assignment.JurorIds.ToArray());
                    break;

                case PhaseChangedEvent e:
                    Phase.Value = e.Current.Phase;
                    Round.Value = e.Current.Round;
                    ActivePlayerId.Value = e.Current.ActivePlayerId;
                    PhaseEndsAt.Value = e.Current.HasTimer ? NetworkManager.ServerTime.Time + e.Current.Duration : 0;
                    PhaseChangedRpc(e.Previous.Phase, e.Previous.Round, e.Previous.ActivePlayerId,
                        e.Current.Phase, e.Current.Round, e.Current.ActivePlayerId, e.Current.Duration);
                    break;

                case EvidenceDrawnEvent e:
                    EvidenceDrawnRpc(e.PlayerId, e.Card.Id, e.ForCase);
                    break;

                case CaseDefinedEvent e:
                    CaseDefinedRpc(e.Description);
                    break;

                case MissionDealtEvent e:
                    MissionDealtRpc(e.JurorId, e.MissionId, e.Description,
                        RpcTarget.Single((ulong)e.JurorId, RpcTargetUse.Temp));
                    break;

                case VoteCastEvent e:
                    VoteProgressRpc(e.Round, e.JurorId, e.VotesCastThisRound, e.VotesNeededThisRound);
                    break;

                case VerdictEvent e:
                    var ids = e.Result.TotalVotes.Keys.ToArray();
                    VerdictRpc(e.Result.ConvictedId, ids, ids.Select(id => e.Result.TotalVotes[id]).ToArray());
                    break;

                case ScoreChangedEvent e:
                    var index = IndexOfPlayer(e.PlayerId);
                    if (index >= 0)
                    {
                        var p = Players[index];
                        p.Score = e.NewScore;
                        Players[index] = p;
                    }
                    ScoreChangedRpc(e.PlayerId, e.Delta, e.NewScore, e.Reason ?? "");
                    break;

                case ChatMessageEvent e:
                    var recipients = e.Recipients.Select(id => (ulong)id).ToArray();
                    ChatRpc(e.Message.SenderId, e.Message.Channel, e.Message.Text,
                        RpcTarget.Group(recipients, RpcTargetUse.Temp));
                    break;

                case GameOverEvent e:
                    GameOverRpc(e.WinnerIds.ToArray());
                    break;
            }
        }

        void OnClientDisconnected(ulong clientId)
        {
            // 게임 중 이탈 처리(대기, 봇 대체 등)는 추후 규칙으로 정한다. 지금은 로비에서만 목록에서 뺀다.
            if (_session != null) return;
            var index = IndexOfPlayer((int)clientId);
            if (index >= 0) Players.RemoveAt(index);
        }

        // ───────────────────────── 서버 → 클라이언트 (캐시 + 이벤트 버스) ─────────────────────────

        [Rpc(SendTo.ClientsAndHost)]
        void MatchStartedRpc(int matchIndex, int totalMatches, int judgeId, int[] defendantIds, int[] jurorIds)
        {
            Assignment = new RoleAssignment(judgeId, defendantIds, jurorIds);
            CaseDescription = null;
            _caseEvidence.Clear();
            _evidenceByPlayer.Clear();
            _myMissions.Clear();
            GameEventBus.Publish(new MatchStartedEvent { MatchIndex = matchIndex, TotalMatches = totalMatches, Assignment = Assignment });
        }

        [Rpc(SendTo.ClientsAndHost)]
        void PhaseChangedRpc(MatchPhase prevPhase, int prevRound, int prevActive,
            MatchPhase phase, int round, int activePlayerId, float duration)
        {
            GameEventBus.Publish(new PhaseChangedEvent
            {
                Previous = new PhaseState(prevPhase, prevRound, prevActive, 0f),
                Current = new PhaseState(phase, round, activePlayerId, duration),
            });
        }

        [Rpc(SendTo.ClientsAndHost)]
        void EvidenceDrawnRpc(int playerId, string cardId, bool forCase)
        {
            var asset = ruleset != null ? ruleset.FindEvidence(cardId) : null;
            var card = asset != null ? asset.ToRuntime() : new EvidenceCard(cardId, cardId);
            if (forCase) _caseEvidence.Add(card);
            else
            {
                if (!_evidenceByPlayer.TryGetValue(playerId, out var list)) _evidenceByPlayer[playerId] = list = new List<EvidenceCard>();
                list.Add(card);
            }
            GameEventBus.Publish(new EvidenceDrawnEvent { PlayerId = playerId, Card = card, ForCase = forCase });
        }

        [Rpc(SendTo.ClientsAndHost)]
        void CaseDefinedRpc(string description)
        {
            CaseDescription = description;
            GameEventBus.Publish(new CaseDefinedEvent { Evidence = _caseEvidence.ToArray(), Description = description });
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void MissionDealtRpc(int jurorId, string missionId, string description, RpcParams rpcParams)
        {
            var e = new MissionDealtEvent { JurorId = jurorId, MissionId = missionId, Description = description };
            _myMissions.Add(e);
            GameEventBus.Publish(e);
        }

        [Rpc(SendTo.ClientsAndHost)]
        void VoteProgressRpc(int round, int jurorId, int cast, int needed) =>
            GameEventBus.Publish(new VoteCastEvent { Round = round, JurorId = jurorId, VotesCastThisRound = cast, VotesNeededThisRound = needed });

        [Rpc(SendTo.ClientsAndHost)]
        void VerdictRpc(int convictedId, int[] defendantIds, int[] totalVotes)
        {
            var totals = new Dictionary<int, int>();
            for (var i = 0; i < defendantIds.Length; i++) totals[defendantIds[i]] = totalVotes[i];
            var innocent = defendantIds.Where(id => id != convictedId).ToArray();
            // 개별 투표 내역은 비공개라서 클라이언트에는 보내지 않는다.
            var result = new VerdictResult(convictedId, innocent, totals, Array.Empty<VoteRecord>());
            GameEventBus.Publish(new VerdictEvent { Result = result });
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ScoreChangedRpc(int playerId, int delta, int newScore, string reason) =>
            GameEventBus.Publish(new ScoreChangedEvent { PlayerId = playerId, Delta = delta, NewScore = newScore, Reason = reason });

        [Rpc(SendTo.SpecifiedInParams)]
        void ChatRpc(int senderId, ChatChannel channel, string text, RpcParams rpcParams) =>
            GameEventBus.Publish(new ChatMessageEvent
            {
                Message = new ChatMessage { SenderId = senderId, Channel = channel, Text = text },
                Recipients = new[] { LocalPlayerId },
            });

        [Rpc(SendTo.ClientsAndHost)]
        void GameOverRpc(int[] winnerIds)
        {
            IsGameOver = true;
            var scores = new Dictionary<int, int>();
            foreach (var p in Players) scores[p.Id] = p.Score;
            GameEventBus.Publish(new GameOverEvent { WinnerIds = winnerIds, FinalScores = scores });
        }

        int IndexOfPlayer(int id)
        {
            for (var i = 0; i < Players.Count; i++)
                if (Players[i].Id == id) return i;
            return -1;
        }

        static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = text.Trim();
            return text.Length > max ? text.Substring(0, max) : text;
        }
    }
}
