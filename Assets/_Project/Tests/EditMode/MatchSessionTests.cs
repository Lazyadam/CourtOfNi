using System.Collections.Generic;
using System.Linq;
using CourtOfNi.Core.Cards;
using CourtOfNi.Core.Chat;
using CourtOfNi.Core.Events;
using CourtOfNi.Core.Match;
using CourtOfNi.Core.Missions;
using CourtOfNi.Core.Phases;
using CourtOfNi.Core.Roles;
using NUnit.Framework;

namespace CourtOfNi.Core.Tests
{
    public class MatchSessionTests
    {
        // 판사 0, 피고인 1·2, 배심원 3·4
        static RoleAssignment Assignment => new RoleAssignment(0, new[] { 1, 2 }, new[] { 3, 4 });

        static MatchSession NewMatch(out List<IGameEvent> events, IEnumerable<IJuryMission> missions = null, int rounds = 2)
        {
            var players = TestFixtures.Players(5).ToDictionary(p => p.Id);
            var rng = new System.Random(1);
            var match = new MatchSession(Assignment, TestFixtures.ManualRules(rounds), players,
                new Deck<EvidenceCard>(TestFixtures.Evidence(10), rng),
                new Deck<IJuryMission>(missions ?? Enumerable.Empty<IJuryMission>(), rng));
            var log = new List<IGameEvent>();
            match.EventRaised += log.Add;
            events = log;
            return match;
        }

        static void AdvanceTo(MatchSession match, MatchPhase phase)
        {
            while (match.Phase.Phase != phase) Assert.That(match.RequestAdvance(0), Is.True);
        }

        [Test]
        public void JudgeDrawsCaseEvidence_AndDefendantsDrawOnTheirTurn()
        {
            var match = NewMatch(out _);
            match.Start();
            AdvanceTo(match, MatchPhase.CaseBuilding);
            Assert.That(match.CaseEvidence.Count, Is.EqualTo(2));

            Assert.That(match.DefineCase(1, "피고인은 사건을 만들 수 없다"), Is.False);
            Assert.That(match.DefineCase(0, "새벽 3시, 사라진 고양이"), Is.True);
            Assert.That(match.Phase.Phase, Is.EqualTo(MatchPhase.Defense));
            Assert.That(match.EvidenceOf(1).Count, Is.EqualTo(1));
            Assert.That(match.EvidenceOf(2).Count, Is.EqualTo(0));

            match.RequestAdvance(0);
            Assert.That(match.EvidenceOf(2).Count, Is.EqualTo(1));
        }

        [Test]
        public void OnlyJudgeCanAdvance()
        {
            var match = NewMatch(out _);
            match.Start();
            Assert.That(match.RequestAdvance(3), Is.False);
            Assert.That(match.Phase.Phase, Is.EqualTo(MatchPhase.RoleReveal));
        }

        [Test]
        public void VotingClosesEarlyWhenAllJurorsVoted()
        {
            var match = NewMatch(out _);
            match.Start();
            AdvanceTo(match, MatchPhase.Voting);

            Assert.That(match.CastVote(1, 2), Is.False, "피고인은 투표할 수 없다");
            Assert.That(match.CastVote(3, 1), Is.True);
            Assert.That(match.Phase.Phase, Is.EqualTo(MatchPhase.Voting));
            Assert.That(match.CastVote(4, 1), Is.True);
            Assert.That(match.Phase.Phase, Is.EqualTo(MatchPhase.Defense));
            Assert.That(match.Phase.Round, Is.EqualTo(2));
        }

        [Test]
        public void Verdict_AppliesDefendantScoresAndMissions()
        {
            var missions = new IJuryMission[]
            {
                new VoteOnlyForSideMission("innocent-only", "무고한 피고인에게만 투표", VerdictSide.Innocent, reward: 1, penalty: 1),
                new VoteOnlyForSideMission("innocent-only-2", "무고한 피고인에게만 투표", VerdictSide.Innocent, reward: 1, penalty: 1),
            };
            var match = NewMatch(out var events, missions, rounds: 1);
            match.Start();
            AdvanceTo(match, MatchPhase.Voting);

            match.CastVote(3, 1);
            match.CastVote(4, 2);
            // 1:1 동률 → 마지막 라운드도 동률 → 무효. 다시 투표 대신 새 매치로 확인
            Assert.That(match.Verdict.HasConviction, Is.False);
            Assert.That(events.OfType<ScoreChangedEvent>().Where(e => e.Reason.Contains("판결")), Is.Empty);

            match = NewMatch(out events, missions, rounds: 1);
            match.Start();
            AdvanceTo(match, MatchPhase.Voting);
            match.CastVote(3, 1);
            match.CastVote(4, 1);

            Assert.That(match.Phase.Phase, Is.EqualTo(MatchPhase.Verdict));
            Assert.That(match.Verdict.ConvictedId, Is.EqualTo(1));

            var scores = events.OfType<ScoreChangedEvent>().ToList();
            Assert.That(scores.Single(e => e.PlayerId == 1).Delta, Is.EqualTo(-3));
            Assert.That(scores.Single(e => e.PlayerId == 2).Delta, Is.EqualTo(2));
            // 두 배심원 모두 유죄가 된 1번에게 투표했으므로 "무고한 피고인에게만 투표" 미션 실패
            Assert.That(scores.Where(e => e.PlayerId == 3 || e.PlayerId == 4).Select(e => e.Delta), Is.EqualTo(new[] { -1, -1 }));

            match.RequestAdvance(0);
            Assert.That(match.IsFinished, Is.True);
        }

        [Test]
        public void MissionsAreDealtPrivatelyToJurors()
        {
            var missions = new IJuryMission[]
            {
                new VoteOnlyForSideMission("a", "A", VerdictSide.Innocent, 1, 0),
                new VoteOnlyForSideMission("b", "B", VerdictSide.Convicted, 1, 0),
            };
            var match = NewMatch(out var events, missions);
            match.Start();

            var dealt = events.OfType<MissionDealtEvent>().ToList();
            Assert.That(dealt.Select(e => e.JurorId), Is.EquivalentTo(new[] { 3, 4 }));
            foreach (var e in dealt) Assert.That(e.Recipients, Is.EqualTo(new[] { e.JurorId }));
        }

        [Test]
        public void JuryChatOnlyReachesJurors()
        {
            var match = NewMatch(out var events);
            match.Start();

            Assert.That(match.SendChat(1, ChatChannel.Jury, "몰래"), Is.False);
            Assert.That(match.SendChat(3, ChatChannel.Jury, "1번이 수상해"), Is.True);
            Assert.That(match.SendChat(1, ChatChannel.Court, "저는 무죄입니다"), Is.True);

            var chats = events.OfType<ChatMessageEvent>().ToList();
            Assert.That(chats[0].Recipients, Is.EquivalentTo(new[] { 3, 4 }));
            Assert.That(chats[1].Recipients, Is.EquivalentTo(new[] { 0, 1, 2, 3, 4 }));
        }

        [Test]
        public void CardsReturnToDeckWhenMatchEnds()
        {
            var players = TestFixtures.Players(5).ToDictionary(p => p.Id);
            var deck = new Deck<EvidenceCard>(TestFixtures.Evidence(10), new System.Random(1));
            var match = new MatchSession(Assignment, TestFixtures.ManualRules(1), players, deck, null);
            match.Start();
            while (!match.IsFinished) match.RequestAdvance(0);

            Assert.That(deck.DrawCount + deck.DiscardCount, Is.EqualTo(10));
        }
    }
}
