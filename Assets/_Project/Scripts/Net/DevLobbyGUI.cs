using System;
using System.Collections.Generic;
using System.Linq;
using CourtOfNi.Core.Chat;
using CourtOfNi.Core.Events;
using CourtOfNi.Core.Phases;
using CourtOfNi.Core.Roles;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CourtOfNi.Net
{
    /// <summary>
    /// 정식 UI가 나오기 전까지 쓰는 개발용 화면(IMGUI). 호스트/접속, 게임 시작, 사건 입력, 투표, 채팅을 모두 버튼으로 테스트할 수 있다.
    /// Multiplayer Play Mode나 빌드를 여러 개 띄워서 한 PC에서 4명 이상을 테스트한다.
    /// </summary>
    public sealed class DevLobbyGUI : MonoBehaviour
    {
        string _address = "127.0.0.1";
        string _name = "Player";
        string _age = "20";
        string _caseText = "";
        string _chatText = "";
        ChatChannel _chatChannel = ChatChannel.Court;
        Vector2 _logScroll;
        readonly List<string> _log = new List<string>();
        readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        bool _profileSent;

        void OnEnable()
        {
            _subscriptions.Add(GameEventBus.Subscribe<MatchStartedEvent>(e => Log($"── {e.MatchIndex + 1}/{e.TotalMatches}판 시작 ──")));
            _subscriptions.Add(GameEventBus.Subscribe<PhaseChangedEvent>(e => Log($"페이즈: {Describe(e.Current)}")));
            _subscriptions.Add(GameEventBus.Subscribe<EvidenceDrawnEvent>(e => Log($"{Name(e.PlayerId)} 증거물: {e.Card.Title}")));
            _subscriptions.Add(GameEventBus.Subscribe<CaseDefinedEvent>(e => Log($"사건: {e.Description}")));
            _subscriptions.Add(GameEventBus.Subscribe<MissionDealtEvent>(e => Log($"[비밀 미션] {e.Description}")));
            _subscriptions.Add(GameEventBus.Subscribe<VoteCastEvent>(e => Log($"투표 {e.VotesCastThisRound}/{e.VotesNeededThisRound}")));
            _subscriptions.Add(GameEventBus.Subscribe<VerdictEvent>(e =>
                Log(e.Result.HasConviction ? $"판결: {Name(e.Result.ConvictedId)} 유죄" : "판결: 동률, 무효")));
            _subscriptions.Add(GameEventBus.Subscribe<ScoreChangedEvent>(e => Log($"{Name(e.PlayerId)} {e.Delta:+#;-#} ({e.Reason}) → {e.NewScore}")));
            _subscriptions.Add(GameEventBus.Subscribe<ChatMessageEvent>(e =>
                Log($"[{(e.Message.Channel == ChatChannel.Jury ? "배심원" : "법정")}] {Name(e.Message.SenderId)}: {e.Message.Text}")));
            _subscriptions.Add(GameEventBus.Subscribe<GameOverEvent>(e => Log($"게임 종료! 우승: {string.Join(", ", e.WinnerIds.Select(Name))}")));
        }

        void OnDisable()
        {
            foreach (var s in _subscriptions) s.Dispose();
            _subscriptions.Clear();
        }

        void Update()
        {
            var game = CourtNetworkGame.Instance;
            if (!_profileSent && game != null && game.IsSpawned)
            {
                float.TryParse(_age, out var age);
                game.SetProfileRpc(_name, age);
                _profileSent = true;
            }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 420, Screen.height - 20), GUI.skin.box);
            var nm = NetworkManager.Singleton;
            if (nm == null) GUILayout.Label("씬에 NetworkManager가 없습니다.");
            else if (!nm.IsClient && !nm.IsServer) DrawConnect(nm);
            else DrawGame(CourtNetworkGame.Instance);
            GUILayout.EndArea();
        }

        void DrawConnect(NetworkManager nm)
        {
            GUILayout.Label("Court of Ni (개발용)");
            _name = Field("이름", _name);
            _age = Field("나이", _age);
            _address = Field("주소", _address);

            if (nm.NetworkConfig.NetworkTransport is UnityTransport utp)
                utp.SetConnectionData(_address, utp.ConnectionData.Port);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("호스트")) nm.StartHost();
            if (GUILayout.Button("접속")) nm.StartClient();
            GUILayout.EndHorizontal();
        }

        void DrawGame(CourtNetworkGame game)
        {
            if (game == null || !game.IsSpawned)
            {
                GUILayout.Label("접속 중...");
                return;
            }

            GUILayout.Label($"나: {game.NameOf(game.LocalPlayerId)} / 역할: {RoleName(game.LocalRole)}");
            foreach (var p in game.Players)
            {
                var role = game.Assignment?.RoleOf(p.Id) ?? Role.None;
                GUILayout.Label($"  {p.Name} ({RoleName(role)}) {p.Score}점");
            }

            if (!game.IsGameRunning)
            {
                if (game.IsServer && GUILayout.Button($"게임 시작 (최소 {RoleRotation.MinimumPlayers}명)")) game.StartGameRpc();
                return;
            }

            var phase = new PhaseState(game.Phase.Value, game.Round.Value, game.ActivePlayerId.Value, 0f);
            var timer = game.SecondsRemaining;
            GUILayout.Label($"페이즈: {Describe(phase)}{(timer > 0 ? $"  ({timer:0}초)" : "")}");
            if (!string.IsNullOrEmpty(game.CaseDescription)) GUILayout.Label($"사건: {game.CaseDescription}");

            if (game.LocalRole == Role.Judge && phase.Phase == MatchPhase.CaseBuilding)
            {
                GUILayout.Label($"뽑은 증거물: {string.Join(", ", game.CaseEvidence.Select(c => c.Title))}");
                _caseText = GUILayout.TextField(_caseText);
                if (GUILayout.Button("사건 확정")) game.DefineCaseRpc(_caseText);
            }

            if (game.LocalRole == Role.Juror)
            {
                foreach (var m in game.MyMissions) GUILayout.Label($"[비밀 미션] {m.Description}");
                if (phase.Phase == MatchPhase.Voting)
                {
                    GUILayout.BeginHorizontal();
                    foreach (var d in game.Assignment.DefendantIds)
                        if (GUILayout.Button($"{Name(d)} 범인")) game.CastVoteRpc(d);
                    GUILayout.EndHorizontal();
                }
            }

            if (game.LocalRole == Role.Judge && phase.Phase != MatchPhase.Finished && GUILayout.Button("다음 단계로")) game.RequestAdvanceRpc();
            if (game.CanHostStartNextMatch && GUILayout.Button("다음 판")) game.NextMatchRpc();

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_chatChannel == ChatChannel.Court, "법정", GUI.skin.button)) _chatChannel = ChatChannel.Court;
            if (game.LocalRole == Role.Juror && GUILayout.Toggle(_chatChannel == ChatChannel.Jury, "배심원", GUI.skin.button)) _chatChannel = ChatChannel.Jury;
            if (game.LocalRole != Role.Juror) _chatChannel = ChatChannel.Court;
            _chatText = GUILayout.TextField(_chatText, GUILayout.Width(200));
            if (GUILayout.Button("보내기") && !string.IsNullOrWhiteSpace(_chatText))
            {
                game.SendChatRpc(_chatChannel, _chatText);
                _chatText = "";
            }
            GUILayout.EndHorizontal();

            _logScroll = GUILayout.BeginScrollView(_logScroll);
            foreach (var line in _log) GUILayout.Label(line);
            GUILayout.EndScrollView();
        }

        void Log(string line)
        {
            _log.Add(line);
            if (_log.Count > 200) _log.RemoveAt(0);
            _logScroll.y = float.MaxValue;
        }

        static string Field(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(40));
            value = GUILayout.TextField(value);
            GUILayout.EndHorizontal();
            return value;
        }

        static string Name(int playerId) => CourtNetworkGame.Instance != null ? CourtNetworkGame.Instance.NameOf(playerId) : $"#{playerId}";

        static string RoleName(Role role)
        {
            switch (role)
            {
                case Role.Judge: return "판사";
                case Role.Defendant: return "피고인";
                case Role.Juror: return "배심원";
                default: return "-";
            }
        }

        static string Describe(PhaseState phase)
        {
            switch (phase.Phase)
            {
                case MatchPhase.RoleReveal: return "역할 공개";
                case MatchPhase.CaseBuilding: return "사건 만들기";
                case MatchPhase.Defense: return $"{phase.Round}라운드 변론: {Name(phase.ActivePlayerId)}";
                case MatchPhase.Questioning: return $"{phase.Round}라운드 질문";
                case MatchPhase.Deliberation: return $"{phase.Round}라운드 배심원 회의";
                case MatchPhase.Voting: return $"{phase.Round}라운드 투표";
                case MatchPhase.Verdict: return "판결";
                case MatchPhase.Finished: return "판 종료";
                default: return "대기";
            }
        }
    }
}
