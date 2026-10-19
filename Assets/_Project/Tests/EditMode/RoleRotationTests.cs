using System.Collections.Generic;
using System.Linq;
using CourtOfNi.Core.Roles;
using NUnit.Framework;

namespace CourtOfNi.Core.Tests
{
    public class RoleRotationTests
    {
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(10)]
        public void OneLap_EveryoneJudgesOnce_AndRolesAreEven(int playerCount)
        {
            var ids = Enumerable.Range(0, playerCount).ToList();
            var rotation = new RoleRotation(ids);

            for (var i = 0; i < rotation.MatchesPerLap; i++)
            {
                var a = rotation.Next();
                Assert.That(a.AllPlayers.OrderBy(x => x), Is.EqualTo(ids));
                Assert.That(a.DefendantIds, Has.No.Member(a.JudgeId));
            }

            foreach (var id in ids) Assert.That(rotation.CountOf(id, Role.Judge), Is.EqualTo(1));
            AssertSpreadAtMostOne(ids.Select(id => rotation.CountOf(id, Role.Defendant)));
            AssertSpreadAtMostOne(ids.Select(id => rotation.CountOf(id, Role.Juror)));
        }

        [Test]
        public void RequiresMinimumPlayers()
        {
            Assert.Throws<System.ArgumentException>(() => new RoleRotation(new[] { 0, 1, 2 }));
        }

        static void AssertSpreadAtMostOne(IEnumerable<int> counts)
        {
            var list = counts.ToList();
            Assert.That(list.Max() - list.Min(), Is.LessThanOrEqualTo(1), string.Join(",", list));
        }
    }
}
