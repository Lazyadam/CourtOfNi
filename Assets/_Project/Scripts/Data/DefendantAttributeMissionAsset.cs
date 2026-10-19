using CourtOfNi.Core.Missions;
using UnityEngine;

namespace CourtOfNi.Data
{
    /// <summary>예: "가장 나이 많은 피고인이 무고했다면 +1점" → attributeKey = "age", highest = true, side = Innocent.</summary>
    [CreateAssetMenu(menuName = "Court of Ni/Missions/Defendant Attribute", fileName = "Mission_Attribute")]
    public sealed class DefendantAttributeMissionAsset : JuryMissionAsset
    {
        [Tooltip("플레이어 속성 키 (예: age)")]
        [SerializeField] string attributeKey = "age";
        [Tooltip("true면 가장 큰 값, false면 가장 작은 값의 피고인을 본다")]
        [SerializeField] bool highest = true;
        [SerializeField] VerdictSide side = VerdictSide.Innocent;

        public override IJuryMission CreateRuntime() =>
            new DefendantAttributeMission(Id, Description, attributeKey, highest, side, Reward, Penalty);
    }
}
