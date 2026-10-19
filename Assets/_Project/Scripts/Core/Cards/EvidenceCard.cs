namespace CourtOfNi.Core.Cards
{
    /// <summary>증거물 카드. 런타임에서는 id로 식별하고, 그림과 설명은 데이터 에셋이 가진다.</summary>
    public sealed class EvidenceCard
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }

        public EvidenceCard(string id, string title, string description = "")
        {
            Id = id;
            Title = title;
            Description = description ?? "";
        }

        public override string ToString() => $"{Title} ({Id})";
    }
}
