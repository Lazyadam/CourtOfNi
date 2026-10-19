using System.Linq;
using CourtOfNi.Core.Cards;
using CourtOfNi.Core.Phases;
using CourtOfNi.Core.Roles;
using CourtOfNi.Core.Voice;
using NUnit.Framework;

namespace CourtOfNi.Core.Tests
{
    public class PolicyAndDeckTests
    {
        [Test]
        public void Voice_DuringDefense_OnlyJudgeAndActiveDefendantSpeak()
        {
            var phase = new PhaseState(MatchPhase.Defense, 1, activePlayerId: 1, duration: 60);
            Assert.That(VoicePolicy.RoutesFor(1, Role.Defendant, phase)[0].CanSpeak, Is.True);
            Assert.That(VoicePolicy.RoutesFor(2, Role.Defendant, phase)[0].CanSpeak, Is.False);
            Assert.That(VoicePolicy.RoutesFor(0, Role.Judge, phase)[0].CanSpeak, Is.True);
            Assert.That(VoicePolicy.RoutesFor(3, Role.Juror, phase)[0].CanSpeak, Is.False);
        }

        [Test]
        public void Voice_DuringDeliberation_JurorsMoveToJuryChannel()
        {
            var phase = new PhaseState(MatchPhase.Deliberation, 1, -1, 45);
            Assert.That(VoicePolicy.RoutesFor(3, Role.Juror, phase).Single().Channel, Is.EqualTo(VoicePolicy.JuryChannel));
            Assert.That(VoicePolicy.RoutesFor(1, Role.Defendant, phase).Single().Channel, Is.EqualTo(VoicePolicy.CourtChannel));
        }

        [Test]
        public void Deck_ReshufflesDiscardsWhenEmpty()
        {
            var deck = new Deck<int>(new[] { 1, 2, 3 }, new System.Random(0));
            var drawn = new[] { deck.Draw(), deck.Draw(), deck.Draw() };
            Assert.That(drawn, Is.EquivalentTo(new[] { 1, 2, 3 }));

            foreach (var c in drawn) deck.Discard(c);
            Assert.That(deck.DrawCount, Is.EqualTo(0));
            deck.Draw();
            Assert.That(deck.DrawCount, Is.EqualTo(2));
            Assert.That(deck.DiscardCount, Is.EqualTo(0));
        }
    }
}
