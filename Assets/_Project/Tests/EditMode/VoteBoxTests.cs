using CourtOfNi.Core.Match;
using NUnit.Framework;

namespace CourtOfNi.Core.Tests
{
    public class VoteBoxTests
    {
        const int A = 1, B = 2;

        [Test]
        public void MostVotesIsConvicted()
        {
            var box = new VoteBox(new[] { A, B }, new[] { 10, 11, 12 });
            box.Cast(1, 10, A);
            box.Cast(1, 11, A);
            box.Cast(1, 12, B);

            var result = box.Resolve(TieBreakRule.NoConviction);
            Assert.That(result.ConvictedId, Is.EqualTo(A));
            Assert.That(result.InnocentIds, Is.EqualTo(new[] { B }));
            Assert.That(result.TotalVotes[A], Is.EqualTo(2));
        }

        [Test]
        public void RevotingInSameRoundReplacesVote()
        {
            var box = new VoteBox(new[] { A, B }, new[] { 10 });
            box.Cast(1, 10, A);
            box.Cast(1, 10, B);
            Assert.That(box.VotesInRound(1), Is.EqualTo(1));
            Assert.That(box.Resolve(TieBreakRule.NoConviction).ConvictedId, Is.EqualTo(B));
        }

        [Test]
        public void TotalTie_UsesLastRound()
        {
            var box = new VoteBox(new[] { A, B }, new[] { 10, 11 });
            box.Cast(1, 10, A); box.Cast(1, 11, A);
            box.Cast(2, 10, B); box.Cast(2, 11, B);
            box.Cast(3, 10, A); box.Cast(3, 11, B);
            box.Cast(4, 10, B); box.Cast(4, 11, B);
            box.Cast(5, 10, A); box.Cast(5, 11, A);
            // 합계 A 5 : B 5, 마지막(5) 라운드는 A 2 : B 0
            Assert.That(box.Resolve(TieBreakRule.LastRoundThenNoConviction).ConvictedId, Is.EqualTo(A));
            Assert.That(box.Resolve(TieBreakRule.NoConviction).HasConviction, Is.False);
        }

        [Test]
        public void CompleteTie_NoConviction()
        {
            var box = new VoteBox(new[] { A, B }, new[] { 10, 11 });
            box.Cast(1, 10, A); box.Cast(1, 11, B);
            var result = box.Resolve(TieBreakRule.LastRoundThenNoConviction);
            Assert.That(result.HasConviction, Is.False);
            Assert.That(result.InnocentIds, Is.EquivalentTo(new[] { A, B }));
        }

        [Test]
        public void RejectsNonJurorAndNonDefendant()
        {
            var box = new VoteBox(new[] { A, B }, new[] { 10 });
            Assert.Throws<System.InvalidOperationException>(() => box.Cast(1, A, B));
            Assert.Throws<System.InvalidOperationException>(() => box.Cast(1, 10, 99));
        }
    }
}
