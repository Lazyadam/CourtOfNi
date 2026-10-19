using System;
using System.Collections.Generic;
using System.Linq;
using CourtOfNi.Core.Cards;
using CourtOfNi.Core.Chat;
using CourtOfNi.Core.Events;
using CourtOfNi.Core.Missions;
using CourtOfNi.Core.Phases;
using CourtOfNi.Core.Players;
using CourtOfNi.Core.Roles;

namespace CourtOfNi.Core.Match
{
    /// <summary>
    /// 한 판(역할 배정 한 번)의 진행을 맡는다. 서버(호스트)에서만 돌고,
    /// 결과는 EventRaised로 내보내서 네트워크 계층이 클라이언트에 전달한다.
    /// </summary>
    public sealed class MatchSession
    {
        readonly MatchRules _rules;
        readonly IReadOnlyDictionary<int, PlayerInfo> _players;
        readonly Deck<EvidenceCard> _evidenceDeck;
        readonly Deck<IJuryMission> _missionDeck;
        readonly List<EvidenceCard> _caseEvidence = new List<EvidenceCard>();
        readonly Dictionary<int, List<EvidenceCard>> _defenseEvidence = new Dictionary<int, List<EvidenceCard>>();
        readonly Dictionary<int, List<IJuryMission>> _missions = new Dictionary<int, List<IJuryMission>>();
        readonly VoteBox _votes;

        public MatchSession(RoleAssignment assignment, MatchRules rules, IReadOnlyDictionary<int, PlayerInfo> players,
            Deck<EvidenceCard> evidenceDeck, Deck<IJuryMission> missionDeck)
        {
            Assignment = assignment ?? throw new ArgumentNullException(nameof(assignment));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _evidenceDeck = evidenceDeck ?? throw new ArgumentNullException(nameof(evidenceDeck));
            _missionDeck = missionDeck;

            foreach (var d in assignment.DefendantIds) _defenseEvidence[d] = new List<EvidenceCard>();
            _votes = new VoteBox(assignment.DefendantIds, assignment.JurorIds);
            Phases = new PhaseMachine(rules, assignment.DefendantIds);
            Phases.PhaseChanged += OnPhaseChanged;
        }

        /// <summary>연출과 네트워크로 내보낼 이벤트.</summary>
        public event Action<IGameEvent> EventRaised;

        public RoleAssignment Assignment { get; }
        public PhaseMachine Phases { get; }
        public PhaseState Phase => Phases.Current;
        public bool IsFinished => Phases.IsFinished;
        public IReadOnlyList<EvidenceCard> CaseEvidence => _caseEvidence;
        public string CaseDescription { get; private set; }
        public VerdictResult Verdict { get; private set; }

        public IReadOnlyList<EvidenceCard> EvidenceOf(int defendantId) => _defenseEvidence[defendantId];
        public IReadOnlyList<IJuryMission> MissionsOf(int jurorId) =>
            _missions.TryGetValue(jurorId, out var list) ? list : (IReadOnlyList<IJuryMission>)Array.Empty<IJuryMission>();

        public void Start() => Phases.Start();

        public void Tick(float deltaSeconds) => Phases.Tick(deltaSeconds);

        /// <summary>판사만 페이즈를 넘길 수 있다.</summary>
        public bool RequestAdvance(int playerId)
        {
            if (playerId != Assignment.JudgeId || IsFinished) return false;
            Phases.Advance();
            return true;
        }

        /// <summary>판사가 뽑은 증거물을 엮어서 사건 설명을 확정한다.</summary>
        public bool DefineCase(int playerId, string description)
        {
            if (playerId != Assignment.JudgeId || Phase.Phase != MatchPhase.CaseBuilding) return false;
            CaseDescription = string.IsNullOrWhiteSpace(description) ? "" : description.Trim();
            Raise(new CaseDefinedEvent { Evidence = _caseEvidence.ToArray(), Description = CaseDescription });
            Phases.Advance();
            return true;
        }

        public bool CastVote(int jurorId, int defendantId)
        {
            if (Phase.Phase != MatchPhase.Voting) return false;
            if (Assignment.RoleOf(jurorId) != Role.Juror || Assignment.RoleOf(defendantId) != Role.Defendant) return false;

            var round = Phase.Round;
            _votes.Cast(round, jurorId, defendantId);
            Raise(new VoteCastEvent
            {
                Round = round,
                JurorId = jurorId,
                VotesCastThisRound = _votes.VotesInRound(round),
                VotesNeededThisRound = Assignment.JurorIds.Count,
            });

            if (_votes.RoundComplete(round)) Phases.Advance();
            return true;
        }

        public bool SendChat(int senderId, ChatChannel channel, string text)
        {
            text = ChatPolicy.Sanitize(text);
            if (text == null || !ChatPolicy.CanSend(Assignment.RoleOf(senderId), channel, Phase)) return false;

            Raise(new ChatMessageEvent
            {
                Message = new ChatMessage { SenderId = senderId, Channel = channel, Text = text },
                Recipients = ChatPolicy.Recipients(Assignment, channel),
            });
            return true;
        }

        void OnPhaseChanged(PhaseState previous, PhaseState current)
        {
            Raise(new PhaseChangedEvent { Previous = previous, Current = current });

            switch (current.Phase)
            {
                case MatchPhase.RoleReveal:
                    DealMissions();
                    break;
                case MatchPhase.CaseBuilding:
                    for (var i = 0; i < _rules.CaseEvidenceCount; i++)
                        DrawEvidence(Assignment.JudgeId, _caseEvidence, forCase: true);
                    break;
                case MatchPhase.Defense:
                    for (var i = 0; i < _rules.EvidencePerDefenseTurn; i++)
                        DrawEvidence(current.ActivePlayerId, _defenseEvidence[current.ActivePlayerId], forCase: false);
                    break;
                case MatchPhase.Verdict:
                    ResolveVerdict();
                    break;
                case MatchPhase.Finished:
                    ReturnCards();
                    break;
            }
        }

        void DealMissions()
        {
            if (_missionDeck == null || _missionDeck.IsEmpty) return;
            foreach (var juror in Assignment.JurorIds)
            {
                var list = new List<IJuryMission>();
                for (var i = 0; i < _rules.MissionsPerJuror && !_missionDeck.IsEmpty; i++)
                {
                    var mission = _missionDeck.Draw();
                    list.Add(mission);
                    Raise(new MissionDealtEvent { JurorId = juror, MissionId = mission.Id, Description = mission.Description });
                }
                _missions[juror] = list;
            }
        }

        void DrawEvidence(int playerId, List<EvidenceCard> into, bool forCase)
        {
            if (_evidenceDeck.IsEmpty) return;
            var card = _evidenceDeck.Draw();
            into.Add(card);
            Raise(new EvidenceDrawnEvent { PlayerId = playerId, Card = card, ForCase = forCase });
        }

        void ResolveVerdict()
        {
            Verdict = _votes.Resolve(_rules.TieBreak);
            Raise(new VerdictEvent { Result = Verdict });

            if (Verdict.HasConviction)
            {
                AddScore(Verdict.ConvictedId, -_rules.ConvictedPenalty, "유죄 판결");
                foreach (var innocent in Verdict.InnocentIds)
                    AddScore(innocent, _rules.AcquittedReward, "무죄 판결");
            }

            var context = new MissionContext(Assignment, Verdict, _players);
            foreach (var pair in _missions)
            foreach (var mission in pair.Value)
            {
                var outcome = mission.Evaluate(context, pair.Key);
                if (outcome.ScoreDelta != 0)
                    AddScore(pair.Key, outcome.ScoreDelta, (outcome.Success ? "미션 성공: " : "미션 실패: ") + mission.Description);
            }
        }

        void AddScore(int playerId, int delta, string reason)
        {
            if (delta == 0) return;
            var player = _players[playerId];
            player.Score += delta;
            Raise(new ScoreChangedEvent { PlayerId = playerId, Delta = delta, NewScore = player.Score, Reason = reason });
        }

        void ReturnCards()
        {
            foreach (var card in _caseEvidence.Concat(_defenseEvidence.Values.SelectMany(l => l)))
                _evidenceDeck.Discard(card);
            if (_missionDeck != null)
                foreach (var mission in _missions.Values.SelectMany(l => l))
                    _missionDeck.Discard(mission);
        }

        void Raise(IGameEvent gameEvent) => EventRaised?.Invoke(gameEvent);
    }
}
