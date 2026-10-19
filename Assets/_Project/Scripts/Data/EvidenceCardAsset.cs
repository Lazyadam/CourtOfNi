using CourtOfNi.Core.Cards;
using UnityEngine;

namespace CourtOfNi.Data
{
    [CreateAssetMenu(menuName = "Court of Ni/Evidence Card", fileName = "Evidence_")]
    public sealed class EvidenceCardAsset : ScriptableObject
    {
        [Tooltip("네트워크로 주고받는 고유 id. 비워두면 에셋 이름을 쓴다.")]
        [SerializeField] string id;
        [SerializeField] string title;
        [SerializeField, TextArea(2, 5)] string description;
        [SerializeField] Sprite artwork;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string Title => string.IsNullOrEmpty(title) ? name : title;
        public string Description => description;
        public Sprite Artwork => artwork;

        public EvidenceCard ToRuntime() => new EvidenceCard(Id, Title, Description);
    }
}
