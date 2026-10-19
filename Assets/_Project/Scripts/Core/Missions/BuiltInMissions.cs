using System.Linq;

namespace CourtOfNi.Core.Missions
{
    public enum VerdictSide
    {
        Innocent = 0,
        Convicted = 1,
    }

    /// <summary>
    /// "모든 라운드에서 무고한(또는 유죄가 된) 피고인에게만 투표했다면 성공".
    /// 예: 무고한 피고인에게만 투표했다면 +1점.
    /// </summary>
    public sealed class VoteOnlyForSideMission : IJuryMission
    {
        public string Id { get; }
        public string Description { get; }
        readonly VerdictSide _side;
        readonly int _reward;
        readonly int _penalty;

        public VoteOnlyForSideMission(string id, string description, VerdictSide side, int reward, int penalty)
        {
            Id = id;
            Description = description;
            _side = side;
            _reward = reward;
            _penalty = penalty;
        }

        public MissionOutcome Evaluate(MissionContext context, int jurorId)
        {
            var verdict = context.Verdict;
            var votes = verdict.VotesBy(jurorId).ToList();
            var success = verdict.HasConviction && votes.Count > 0 && votes.All(v =>
                _side == VerdictSide.Innocent ? verdict.IsInnocent(v.DefendantId) : v.DefendantId == verdict.ConvictedId);
            return success ? new MissionOutcome(true, _reward) : new MissionOutcome(false, -_penalty);
        }
    }

    /// <summary>
    /// "피고인 중 속성값이 가장 큰(또는 작은) 사람이 무고했다면(또는 유죄라면) 성공".
    /// 예: 가장 나이 많은 피고인이 무고했다면 +1점 → attributeKey="age", highest=true, side=Innocent.
    /// 두 피고인의 값이 같거나 값이 없으면 실패로 처리한다.
    /// </summary>
    public sealed class DefendantAttributeMission : IJuryMission
    {
        public string Id { get; }
        public string Description { get; }
        readonly string _attributeKey;
        readonly bool _highest;
        readonly VerdictSide _side;
        readonly int _reward;
        readonly int _penalty;

        public DefendantAttributeMission(string id, string description, string attributeKey, bool highest,
            VerdictSide side, int reward, int penalty)
        {
            Id = id;
            Description = description;
            _attributeKey = attributeKey;
            _highest = highest;
            _side = side;
            _reward = reward;
            _penalty = penalty;
        }

        public MissionOutcome Evaluate(MissionContext context, int jurorId)
        {
            var verdict = context.Verdict;
            var valued = context.Assignment.DefendantIds
                .Select(id => (id, ok: context.Players[id].TryGetAttribute(_attributeKey, out var value), value))
                .ToList();

            var success = false;
            if (verdict.HasConviction && valued.All(x => x.ok) && valued.Select(x => x.value).Distinct().Count() == valued.Count)
            {
                var target = (_highest ? valued.OrderByDescending(x => x.value) : valued.OrderBy(x => x.value)).First().id;
                success = _side == VerdictSide.Innocent ? verdict.IsInnocent(target) : target == verdict.ConvictedId;
            }

            return success ? new MissionOutcome(true, _reward) : new MissionOutcome(false, -_penalty);
        }
    }
}
