using CourtOfNi.Core.Missions;
using UnityEngine;

namespace CourtOfNi.Data
{
    /// <summary>예: "무고한 피고인에게만 투표했다면 +1점".</summary>
    [CreateAssetMenu(menuName = "Court of Ni/Missions/Vote Only For Side", fileName = "Mission_VoteOnly")]
    public sealed class VoteOnlyForSideMissionAsset : JuryMissionAsset
    {
        [SerializeField] VerdictSide side = VerdictSide.Innocent;

        public override IJuryMission CreateRuntime() =>
            new VoteOnlyForSideMission(Id, Description, side, Reward, Penalty);
    }
}
