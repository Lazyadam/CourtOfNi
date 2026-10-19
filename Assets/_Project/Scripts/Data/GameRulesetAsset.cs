using System.Collections.Generic;
using System.Linq;
using CourtOfNi.Core.Cards;
using CourtOfNi.Core.Match;
using CourtOfNi.Core.Missions;
using UnityEngine;

namespace CourtOfNi.Data
{
    /// <summary>
    /// 한 게임의 규칙과 카드 구성을 담는 에셋. 규칙을 바꾸고 싶으면 이 에셋을 복제해서 값만 바꾸면 된다.
    /// </summary>
    [CreateAssetMenu(menuName = "Court of Ni/Game Ruleset", fileName = "Ruleset_Default")]
    public sealed class GameRulesetAsset : ScriptableObject
    {
        [Header("진행")]
        [SerializeField, Range(1, 5)] int rounds = 3;
        [SerializeField, Min(1)] int caseEvidenceCount = 2;
        [SerializeField, Min(0)] int evidencePerDefenseTurn = 1;
        [SerializeField, Min(0)] int missionsPerJuror = 1;
        [Tooltip("모든 플레이어가 판사를 몇 바퀴 돌면 게임이 끝나는지")]
        [SerializeField, Min(1)] int laps = 1;

        [Header("점수")]
        [SerializeField, Min(0)] int convictedPenalty = 2;
        [SerializeField, Min(0)] int acquittedReward = 2;
        [SerializeField] TieBreakRule tieBreak = TieBreakRule.LastRoundThenNoConviction;

        [Header("제한 시간 (초, 0이면 판사가 직접 넘김)")]
        [SerializeField, Min(0)] float roleRevealSeconds = 5f;
        [SerializeField, Min(0)] float caseBuildingSeconds;
        [SerializeField, Min(0)] float defenseSeconds = 60f;
        [SerializeField, Min(0)] float questioningSeconds = 60f;
        [SerializeField, Min(0)] float deliberationSeconds = 45f;
        [SerializeField, Min(0)] float votingSeconds = 20f;
        [SerializeField, Min(0)] float verdictSeconds = 8f;

        [Header("카드")]
        [SerializeField] List<EvidenceCardAsset> evidenceCards = new List<EvidenceCardAsset>();
        [SerializeField] List<JuryMissionAsset> juryMissions = new List<JuryMissionAsset>();

        public int Laps => laps;
        public IReadOnlyList<EvidenceCardAsset> EvidenceCards => evidenceCards;
        public IReadOnlyList<JuryMissionAsset> JuryMissions => juryMissions;

        public MatchRules ToMatchRules() => new MatchRules
        {
            Rounds = rounds,
            CaseEvidenceCount = caseEvidenceCount,
            EvidencePerDefenseTurn = evidencePerDefenseTurn,
            MissionsPerJuror = missionsPerJuror,
            ConvictedPenalty = convictedPenalty,
            AcquittedReward = acquittedReward,
            TieBreak = tieBreak,
            RoleRevealSeconds = roleRevealSeconds,
            CaseBuildingSeconds = caseBuildingSeconds,
            DefenseSeconds = defenseSeconds,
            QuestioningSeconds = questioningSeconds,
            DeliberationSeconds = deliberationSeconds,
            VotingSeconds = votingSeconds,
            VerdictSeconds = verdictSeconds,
        };

        public IEnumerable<EvidenceCard> CreateEvidenceCards() =>
            evidenceCards.Where(c => c != null).Select(c => c.ToRuntime());

        public IEnumerable<IJuryMission> CreateMissions() =>
            juryMissions.Where(m => m != null).Select(m => m.CreateRuntime());

        public EvidenceCardAsset FindEvidence(string cardId) =>
            evidenceCards.FirstOrDefault(c => c != null && c.Id == cardId);
    }
}
