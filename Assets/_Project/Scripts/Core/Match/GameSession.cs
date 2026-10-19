using System;
using System.Collections.Generic;
using System.Linq;
using CourtOfNi.Core.Cards;
using CourtOfNi.Core.Events;
using CourtOfNi.Core.Missions;
using CourtOfNi.Core.Phases;
using CourtOfNi.Core.Players;
using CourtOfNi.Core.Roles;

namespace CourtOfNi.Core.Match
{
    /// <summary>
    /// 게임 전체(여러 판)를 관리한다. 모든 플레이어가 판사를 한 번씩 맡으면(한 바퀴) 게임이 끝나고,
    /// 점수가 가장 높은 사람이 이긴다.
    /// </summary>
    public sealed class GameSession
    {
        readonly List<PlayerInfo> _players;
        readonly Dictionary<int, PlayerInfo> _playersById;
        readonly RoleRotation _rotation;
        readonly Deck<EvidenceCard> _evidenceDeck;
        readonly Deck<IJuryMission> _missionDeck;
        readonly int _laps;

        public GameSession(IEnumerable<PlayerInfo> seatOrder, MatchRules rules,
            IEnumerable<EvidenceCard> evidenceCards, IEnumerable<IJuryMission> missions, int seed, int laps = 1)
        {
            _players = seatOrder.ToList();
            _playersById = _players.ToDictionary(p => p.Id);
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _laps = Math.Max(1, laps);

            var rng = new Random(seed);
            _rotation = new RoleRotation(_players.Select(p => p.Id));
            _evidenceDeck = new Deck<EvidenceCard>(evidenceCards, rng);
            if (_evidenceDeck.IsEmpty) throw new ArgumentException("증거물 카드가 최소 1장 필요합니다.", nameof(evidenceCards));
            _missionDeck = new Deck<IJuryMission>(missions ?? Enumerable.Empty<IJuryMission>(), rng);
        }

        public event Action<IGameEvent> EventRaised;

        public MatchRules Rules { get; }
        public IReadOnlyList<PlayerInfo> Players => _players;
        public IReadOnlyDictionary<int, PlayerInfo> PlayersById => _playersById;
        public RoleRotation Rotation => _rotation;
        public MatchSession CurrentMatch { get; private set; }
        public int TotalMatches => _rotation.MatchesPerLap * _laps;
        public int MatchesPlayed => _rotation.MatchesAssigned;
        public bool IsGameOver { get; private set; }

        public bool CanStartNextMatch => !IsGameOver && (CurrentMatch == null || CurrentMatch.IsFinished) && MatchesPlayed < TotalMatches;

        public MatchSession StartNextMatch()
        {
            if (!CanStartNextMatch) throw new InvalidOperationException("다음 판을 시작할 수 없습니다.");

            if (CurrentMatch != null) CurrentMatch.EventRaised -= Forward;

            var assignment = _rotation.Next();
            CurrentMatch = new MatchSession(assignment, Rules, _playersById, _evidenceDeck, _missionDeck);
            CurrentMatch.EventRaised += Forward;
            Forward(new MatchStartedEvent { MatchIndex = MatchesPlayed - 1, TotalMatches = TotalMatches, Assignment = assignment });
            CurrentMatch.Start();
            return CurrentMatch;
        }

        public void Tick(float deltaSeconds)
        {
            if (CurrentMatch == null || IsGameOver) return;
            CurrentMatch.Tick(deltaSeconds);
        }

        void Forward(IGameEvent gameEvent)
        {
            EventRaised?.Invoke(gameEvent);
            if (gameEvent is PhaseChangedEvent changed && changed.Current.Phase == MatchPhase.Finished
                && MatchesPlayed >= TotalMatches)
                EndGame();
        }

        void EndGame()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            var best = _players.Max(p => p.Score);
            EventRaised?.Invoke(new GameOverEvent
            {
                WinnerIds = _players.Where(p => p.Score == best).Select(p => p.Id).ToArray(),
                FinalScores = _players.ToDictionary(p => p.Id, p => p.Score),
            });
        }
    }
}
