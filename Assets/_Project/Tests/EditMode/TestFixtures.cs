using System.Collections.Generic;
using System.Linq;
using CourtOfNi.Core.Cards;
using CourtOfNi.Core.Match;
using CourtOfNi.Core.Players;

namespace CourtOfNi.Core.Tests
{
    static class TestFixtures
    {
        public static List<PlayerInfo> Players(int count) =>
            Enumerable.Range(0, count).Select(i =>
            {
                var p = new PlayerInfo(i, $"P{i}");
                p.Attributes["age"] = 20 + i;
                return p;
            }).ToList();

        public static List<EvidenceCard> Evidence(int count) =>
            Enumerable.Range(0, count).Select(i => new EvidenceCard($"ev{i}", $"증거{i}")).ToList();

        /// <summary>타이머 없는 규칙. 테스트에서 Advance로 직접 진행한다.</summary>
        public static MatchRules ManualRules(int rounds = 2) => new MatchRules
        {
            Rounds = rounds,
            RoleRevealSeconds = 0,
            CaseBuildingSeconds = 0,
            DefenseSeconds = 0,
            QuestioningSeconds = 0,
            DeliberationSeconds = 0,
            VotingSeconds = 0,
            VerdictSeconds = 0,
            ConvictedPenalty = 3,
            AcquittedReward = 2,
        };
    }
}
