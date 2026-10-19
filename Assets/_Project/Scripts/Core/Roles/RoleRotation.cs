using System;
using System.Collections.Generic;
using System.Linq;

namespace CourtOfNi.Core.Roles
{
    /// <summary>
    /// 판사·피고인·배심원을 최대한 균일하게 돌린다.
    /// 각 역할마다 "그 역할을 맡은 횟수가 적은 사람 → 그 역할을 맡은 지 오래된 사람 → 좌석 순" 으로 고른다.
    /// 한 바퀴(모든 플레이어가 판사를 한 번씩)는 플레이어 수만큼의 매치다.
    /// </summary>
    public sealed class RoleRotation
    {
        readonly List<int> _seats;
        readonly Dictionary<(int player, Role role), int> _counts = new Dictionary<(int, Role), int>();
        readonly Dictionary<(int player, Role role), int> _lastMatch = new Dictionary<(int, Role), int>();
        int _matchIndex;

        public RoleRotation(IEnumerable<int> seatOrder)
        {
            _seats = seatOrder.ToList();
            if (_seats.Count < MinimumPlayers)
                throw new ArgumentException($"최소 {MinimumPlayers}명이 필요합니다.", nameof(seatOrder));
            if (_seats.Distinct().Count() != _seats.Count)
                throw new ArgumentException("플레이어 id가 중복되었습니다.", nameof(seatOrder));
        }

        /// <summary>판사 1 + 피고인 2 + 배심원 1.</summary>
        public const int MinimumPlayers = 1 + RoleAssignment.DefendantCount + 1;

        public int MatchesPerLap => _seats.Count;
        public int MatchesAssigned => _matchIndex;

        public int CountOf(int playerId, Role role) => _counts.TryGetValue((playerId, role), out var c) ? c : 0;

        public RoleAssignment Next()
        {
            var judge = PickFewest(_seats, Role.Judge, 1)[0];
            var defendants = PickFewest(_seats.Where(p => p != judge), Role.Defendant, RoleAssignment.DefendantCount);
            var jurors = _seats.Where(p => p != judge && !defendants.Contains(p)).ToList();

            Record(judge, Role.Judge);
            foreach (var d in defendants) Record(d, Role.Defendant);
            foreach (var j in jurors) Record(j, Role.Juror);
            _matchIndex++;

            return new RoleAssignment(judge, defendants, jurors);
        }

        List<int> PickFewest(IEnumerable<int> candidates, Role role, int count)
        {
            return candidates
                .OrderBy(p => CountOf(p, role))
                .ThenBy(p => _lastMatch.TryGetValue((p, role), out var last) ? last : -1)
                .ThenBy(p => SeatDistanceFromRotation(p))
                .Take(count)
                .ToList();
        }

        // 동률일 때 매번 같은 사람이 먼저 뽑히지 않도록 매치마다 시작 좌석을 한 칸씩 민다.
        int SeatDistanceFromRotation(int playerId)
        {
            var seat = _seats.IndexOf(playerId);
            var start = _matchIndex % _seats.Count;
            return (seat - start + _seats.Count) % _seats.Count;
        }

        void Record(int playerId, Role role)
        {
            _counts[(playerId, role)] = CountOf(playerId, role) + 1;
            _lastMatch[(playerId, role)] = _matchIndex;
        }
    }
}
