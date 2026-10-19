using System.Collections.Generic;

namespace CourtOfNi.Core.Players
{
    /// <summary>
    /// 한 플레이어의 정보. Attributes에는 "나이"처럼 배심원 미션이 참조하는 값을 자유롭게 넣는다.
    /// </summary>
    public sealed class PlayerInfo
    {
        public int Id { get; }
        public string DisplayName { get; set; }
        public int Score { get; set; }
        public Dictionary<string, float> Attributes { get; } = new Dictionary<string, float>();

        public PlayerInfo(int id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public bool TryGetAttribute(string key, out float value) => Attributes.TryGetValue(key, out value);
    }
}
