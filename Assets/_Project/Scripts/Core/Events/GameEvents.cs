using System.Collections.Generic;
using CourtOfNi.Core.Cards;
using CourtOfNi.Core.Chat;
using CourtOfNi.Core.Match;
using CourtOfNi.Core.Phases;
using CourtOfNi.Core.Roles;

namespace CourtOfNi.Core.Events
{
    /// <summary>연출 훅이 붙는 모든 게임 이벤트의 표식.</summary>
    public interface IGameEvent { }

    /// <summary>이벤트가 누구에게 보여야 하는지. 네트워크 계층이 이 값으로 전송 대상을 고른다.</summary>
    public interface IPrivateGameEvent : IGameEvent
    {
        /// <summary>이 이벤트를 받을 플레이어 id 목록.</summary>
        IReadOnlyList<int> Recipients { get; }
    }

    public sealed class MatchStartedEvent : IGameEvent
    {
        public int MatchIndex;
        public int TotalMatches;
        public RoleAssignment Assignment;
    }

    public sealed class PhaseChangedEvent : IGameEvent
    {
        public PhaseState Previous;
        public PhaseState Current;
    }

    public sealed class EvidenceDrawnEvent : IGameEvent
    {
        public int PlayerId;
        public EvidenceCard Card;
        /// <summary>true면 판사가 사건을 만들 때 뽑은 카드.</summary>
        public bool ForCase;
    }

    public sealed class CaseDefinedEvent : IGameEvent
    {
        public IReadOnlyList<EvidenceCard> Evidence;
        public string Description;
    }

    public sealed class MissionDealtEvent : IPrivateGameEvent
    {
        public int JurorId;
        public string MissionId;
        public string Description;
        public IReadOnlyList<int> Recipients => new[] { JurorId };
    }

    public sealed class VoteCastEvent : IGameEvent
    {
        public int Round;
        public int JurorId;
        /// <summary>투표 내용은 비공개로 두고, 누가 투표를 마쳤는지만 알린다.</summary>
        public int VotesCastThisRound;
        public int VotesNeededThisRound;
    }

    public sealed class VerdictEvent : IGameEvent
    {
        public VerdictResult Result;
    }

    public sealed class ScoreChangedEvent : IGameEvent
    {
        public int PlayerId;
        public int Delta;
        public int NewScore;
        public string Reason;
    }

    public sealed class ChatMessageEvent : IPrivateGameEvent
    {
        public ChatMessage Message;
        public IReadOnlyList<int> Recipients { get; set; }
    }

    public sealed class GameOverEvent : IGameEvent
    {
        public IReadOnlyList<int> WinnerIds;
        public IReadOnlyDictionary<int, int> FinalScores;
    }
}
