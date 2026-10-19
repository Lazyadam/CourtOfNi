namespace CourtOfNi.Core.Phases
{
    public enum MatchPhase
    {
        /// <summary>매치 시작 전.</summary>
        NotStarted = 0,
        /// <summary>역할 공개.</summary>
        RoleReveal = 1,
        /// <summary>판사가 증거물 카드를 뽑아 사건을 만든다.</summary>
        CaseBuilding = 2,
        /// <summary>피고인 한 명의 변론 차례. ActivePlayerId가 변론 중인 피고인.</summary>
        Defense = 3,
        /// <summary>배심원이 피고인에게 질문한다.</summary>
        Questioning = 4,
        /// <summary>배심원끼리만 회의한다.</summary>
        Deliberation = 5,
        /// <summary>배심원 투표.</summary>
        Voting = 6,
        /// <summary>판결과 점수 정산.</summary>
        Verdict = 7,
        /// <summary>매치 종료.</summary>
        Finished = 8,
    }

    /// <summary>현재 페이즈의 스냅샷. 네트워크로 그대로 동기화하기 쉬운 값만 담는다.</summary>
    public readonly struct PhaseState
    {
        public readonly MatchPhase Phase;
        /// <summary>1부터 시작하는 라운드 번호. 라운드가 없는 페이즈에서는 0.</summary>
        public readonly int Round;
        /// <summary>변론 중인 피고인 id. 없으면 -1.</summary>
        public readonly int ActivePlayerId;
        /// <summary>제한 시간. 0 이하면 타이머 없음.</summary>
        public readonly float Duration;

        public PhaseState(MatchPhase phase, int round, int activePlayerId, float duration)
        {
            Phase = phase;
            Round = round;
            ActivePlayerId = activePlayerId;
            Duration = duration;
        }

        public bool HasTimer => Duration > 0f;

        public override string ToString() =>
            ActivePlayerId >= 0 ? $"{Phase} R{Round} (player {ActivePlayerId})" : $"{Phase} R{Round}";
    }
}
