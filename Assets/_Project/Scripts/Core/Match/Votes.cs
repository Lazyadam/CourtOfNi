using System;
using System.Collections.Generic;
using System.Linq;

namespace CourtOfNi.Core.Match
{
    /// <summary>배심원 한 명이 한 라운드에 "이 피고인이 범인이다"라고 던진 표.</summary>
    public readonly struct VoteRecord
    {
        public readonly int Round;
        public readonly int JurorId;
        public readonly int DefendantId;

        public VoteRecord(int round, int jurorId, int defendantId)
        {
            Round = round;
            JurorId = jurorId;
            DefendantId = defendantId;
        }
    }

    /// <summary>판결 결과. ConvictedId가 -1이면 동률로 아무도 유죄가 되지 않은 것이다.</summary>
    public sealed class VerdictResult
    {
        public int ConvictedId { get; }
        public IReadOnlyList<int> InnocentIds { get; }
        public IReadOnlyDictionary<int, int> TotalVotes { get; }
        public IReadOnlyList<VoteRecord> Votes { get; }

        public VerdictResult(int convictedId, IReadOnlyList<int> innocentIds,
            IReadOnlyDictionary<int, int> totalVotes, IReadOnlyList<VoteRecord> votes)
        {
            ConvictedId = convictedId;
            InnocentIds = innocentIds;
            TotalVotes = totalVotes;
            Votes = votes;
        }

        public bool HasConviction => ConvictedId >= 0;
        public bool IsInnocent(int defendantId) => InnocentIds.Contains(defendantId);
        public IEnumerable<VoteRecord> VotesBy(int jurorId) => Votes.Where(v => v.JurorId == jurorId);
    }

    /// <summary>라운드별 투표를 모으고 판결을 계산한다.</summary>
    public sealed class VoteBox
    {
        readonly IReadOnlyList<int> _defendants;
        readonly IReadOnlyList<int> _jurors;
        readonly List<VoteRecord> _votes = new List<VoteRecord>();

        public VoteBox(IReadOnlyList<int> defendantIds, IReadOnlyList<int> jurorIds)
        {
            _defendants = defendantIds;
            _jurors = jurorIds;
        }

        public IReadOnlyList<VoteRecord> Votes => _votes;

        public int VotesInRound(int round) => _votes.Count(v => v.Round == round);
        public bool RoundComplete(int round) => VotesInRound(round) >= _jurors.Count;
        public bool HasVoted(int round, int jurorId) => _votes.Any(v => v.Round == round && v.JurorId == jurorId);

        /// <summary>표를 던진다. 같은 라운드에 다시 던지면 마지막 표로 바뀐다.</summary>
        public void Cast(int round, int jurorId, int defendantId)
        {
            if (!_jurors.Contains(jurorId)) throw new InvalidOperationException($"{jurorId}는 배심원이 아닙니다.");
            if (!_defendants.Contains(defendantId)) throw new InvalidOperationException($"{defendantId}는 피고인이 아닙니다.");

            _votes.RemoveAll(v => v.Round == round && v.JurorId == jurorId);
            _votes.Add(new VoteRecord(round, jurorId, defendantId));
        }

        public VerdictResult Resolve(TieBreakRule tieBreak)
        {
            var totals = _defendants.ToDictionary(d => d, d => _votes.Count(v => v.DefendantId == d));
            var convicted = PickMost(totals);

            if (convicted < 0 && tieBreak == TieBreakRule.LastRoundThenNoConviction && _votes.Count > 0)
            {
                var lastRound = _votes.Max(v => v.Round);
                var lastRoundTotals = _defendants.ToDictionary(
                    d => d, d => _votes.Count(v => v.Round == lastRound && v.DefendantId == d));
                convicted = PickMost(lastRoundTotals);
            }

            var innocent = _defendants.Where(d => d != convicted).ToArray();
            return new VerdictResult(convicted, innocent, totals, _votes.ToArray());
        }

        static int PickMost(Dictionary<int, int> totals)
        {
            var max = totals.Values.Max();
            var leaders = totals.Where(kv => kv.Value == max).Select(kv => kv.Key).ToList();
            return leaders.Count == 1 ? leaders[0] : -1;
        }
    }
}
