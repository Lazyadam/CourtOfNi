using System;
using System.Collections.Generic;
using System.Linq;

namespace CourtOfNi.Core.Roles
{
    /// <summary>한 판(매치)의 역할 배정: 판사 1명, 피고인 2명, 나머지 배심원.</summary>
    public sealed class RoleAssignment
    {
        public const int DefendantCount = 2;

        public int JudgeId { get; }
        public IReadOnlyList<int> DefendantIds { get; }
        public IReadOnlyList<int> JurorIds { get; }

        public RoleAssignment(int judgeId, IReadOnlyList<int> defendantIds, IReadOnlyList<int> jurorIds)
        {
            if (defendantIds == null || defendantIds.Count != DefendantCount)
                throw new ArgumentException($"피고인은 정확히 {DefendantCount}명이어야 합니다.", nameof(defendantIds));
            if (jurorIds == null || jurorIds.Count == 0)
                throw new ArgumentException("배심원은 최소 1명이어야 합니다.", nameof(jurorIds));

            JudgeId = judgeId;
            DefendantIds = defendantIds.ToArray();
            JurorIds = jurorIds.ToArray();
        }

        public Role RoleOf(int playerId)
        {
            if (playerId == JudgeId) return Role.Judge;
            if (DefendantIds.Contains(playerId)) return Role.Defendant;
            if (JurorIds.Contains(playerId)) return Role.Juror;
            return Role.None;
        }

        public int OtherDefendant(int defendantId)
        {
            if (DefendantIds[0] == defendantId) return DefendantIds[1];
            if (DefendantIds[1] == defendantId) return DefendantIds[0];
            throw new ArgumentException($"{defendantId}는 피고인이 아닙니다.", nameof(defendantId));
        }

        public IEnumerable<int> AllPlayers => new[] { JudgeId }.Concat(DefendantIds).Concat(JurorIds);
    }
}
