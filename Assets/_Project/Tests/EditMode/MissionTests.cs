using System.Linq;
using CourtOfNi.Core.Match;
using CourtOfNi.Core.Missions;
using CourtOfNi.Core.Roles;
using NUnit.Framework;

namespace CourtOfNi.Core.Tests
{
    public class MissionTests
    {
        // 판사 0, 피고인 1(21살)·2(22살), 배심원 3·4
        static MissionContext Context(int convicted, params (int round, int juror, int defendant)[] votes)
        {
            var assignment = new RoleAssignment(0, new[] { 1, 2 }, new[] { 3, 4 });
            var box = new VoteBox(assignment.DefendantIds, assignment.JurorIds);
            foreach (var v in votes) box.Cast(v.round, v.juror, v.defendant);
            var verdict = box.Resolve(TieBreakRule.NoConviction);
            Assert.That(verdict.ConvictedId, Is.EqualTo(convicted), "테스트 전제 확인");
            return new MissionContext(assignment, verdict, TestFixtures.Players(5).ToDictionary(p => p.Id));
        }

        [Test]
        public void VoteOnlyForInnocent()
        {
            var mission = new VoteOnlyForSideMission("m", "", VerdictSide.Innocent, reward: 1, penalty: 2);
            // 1번이 유죄(2표), 2번이 무죄. 배심원 4는 두 라운드 모두 2번(무고)에게 투표
            var ctx = Context(1, (1, 3, 1), (1, 4, 2), (2, 3, 1), (2, 4, 2), (3, 3, 1), (3, 4, 1));

            Assert.That(mission.Evaluate(ctx, 3).ScoreDelta, Is.EqualTo(-2));
            var ctx2 = Context(1, (1, 3, 1), (1, 4, 2), (2, 3, 1), (2, 4, 2), (3, 3, 1));
            Assert.That(mission.Evaluate(ctx2, 4).ScoreDelta, Is.EqualTo(1));
        }

        [Test]
        public void OldestDefendantInnocent()
        {
            var mission = new DefendantAttributeMission("m", "", "age", highest: true, VerdictSide.Innocent, reward: 1, penalty: 0);
            // 2번(22살)이 가장 나이 많음
            Assert.That(mission.Evaluate(Context(1, (1, 3, 1)), 3).Success, Is.True);
            var fail = mission.Evaluate(Context(2, (1, 3, 2)), 3);
            Assert.That(fail.Success, Is.False);
            Assert.That(fail.ScoreDelta, Is.EqualTo(0));
        }
    }
}
