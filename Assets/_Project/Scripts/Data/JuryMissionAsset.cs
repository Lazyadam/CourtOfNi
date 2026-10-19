using CourtOfNi.Core.Missions;
using UnityEngine;

namespace CourtOfNi.Data
{
    /// <summary>
    /// 배심원 미션 데이터의 기반 클래스. 새 미션 종류는 이 클래스를 상속해서 CreateRuntime만 구현하면
    /// 에디터의 Create 메뉴에서 바로 만들 수 있다.
    /// </summary>
    public abstract class JuryMissionAsset : ScriptableObject
    {
        [Tooltip("네트워크로 주고받는 고유 id. 비워두면 에셋 이름을 쓴다.")]
        [SerializeField] string id;
        [SerializeField, TextArea(2, 4)] string description;
        [Tooltip("성공 시 얻는 점수")]
        [SerializeField] int reward = 1;
        [Tooltip("실패 시 잃는 점수 (0이면 감점 없음)")]
        [SerializeField, Min(0)] int penalty;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string Description => description;
        public int Reward => reward;
        public int Penalty => penalty;

        public abstract IJuryMission CreateRuntime();
    }
}
