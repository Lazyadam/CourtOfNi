namespace CourtOfNi.Core.Match
{
    public enum TieBreakRule
    {
        /// <summary>총 득표가 같으면 마지막 라운드 득표로 가리고, 그래도 같으면 아무도 유죄가 되지 않는다.</summary>
        LastRoundThenNoConviction = 0,
        /// <summary>총 득표가 같으면 아무도 유죄가 되지 않는다.</summary>
        NoConviction = 1,
    }

    /// <summary>
    /// 한 판의 규칙 값. 데이터 에셋(GameRulesetAsset)이 이 값을 만들어서 넘긴다.
    /// 시간 값이 0 이하면 타이머 없이 판사가 직접 다음 단계로 넘긴다.
    /// </summary>
    public sealed class MatchRules
    {
        /// <summary>변론 → 질문 → 배심원 회의 → 투표 사이클 수 (기획상 2~3회).</summary>
        public int Rounds = 3;

        /// <summary>판사가 사건을 만들 때 뽑는 증거물 카드 수.</summary>
        public int CaseEvidenceCount = 2;

        /// <summary>피고인이 자기 변론 차례마다 뽑는 증거물 카드 수.</summary>
        public int EvidencePerDefenseTurn = 1;

        /// <summary>배심원 한 명이 받는 비공개 미션 수.</summary>
        public int MissionsPerJuror = 1;

        /// <summary>유죄(범인)가 된 피고인이 잃는 점수.</summary>
        public int ConvictedPenalty = 2;

        /// <summary>무고한 피고인이 얻는 점수.</summary>
        public int AcquittedReward = 2;

        public TieBreakRule TieBreak = TieBreakRule.LastRoundThenNoConviction;

        public float RoleRevealSeconds = 5f;
        public float CaseBuildingSeconds = 0f;
        public float DefenseSeconds = 60f;
        public float QuestioningSeconds = 60f;
        public float DeliberationSeconds = 45f;
        public float VotingSeconds = 20f;
        public float VerdictSeconds = 8f;
    }
}
