using System.Collections.Generic;
using CourtOfNi.Core.Match;
using CourtOfNi.Core.Phases;
using NUnit.Framework;

namespace CourtOfNi.Core.Tests
{
    public class PhaseMachineTests
    {
        [Test]
        public void Sequence_AlternatesFirstDefendantEachRound()
        {
            var machine = new PhaseMachine(TestFixtures.ManualRules(rounds: 2), new[] { 10, 20 });
            var seen = new List<string>();
            machine.PhaseChanged += (_, current) => seen.Add(current.ToString());

            machine.Start();
            while (!machine.IsFinished) machine.Advance();

            Assert.That(seen, Is.EqualTo(new[]
            {
                "RoleReveal R0", "CaseBuilding R0",
                "Defense R1 (player 10)", "Defense R1 (player 20)", "Questioning R1", "Deliberation R1", "Voting R1",
                "Defense R2 (player 20)", "Defense R2 (player 10)", "Questioning R2", "Deliberation R2", "Voting R2",
                "Verdict R0", "Finished R0",
            }));
        }

        [Test]
        public void Tick_AdvancesOnlyTimedPhases()
        {
            var rules = TestFixtures.ManualRules();
            rules.RoleRevealSeconds = 1f;
            var machine = new PhaseMachine(rules, new[] { 1, 2 });
            machine.Start();

            machine.Tick(0.5f);
            Assert.That(machine.Current.Phase, Is.EqualTo(MatchPhase.RoleReveal));
            machine.Tick(0.6f);
            Assert.That(machine.Current.Phase, Is.EqualTo(MatchPhase.CaseBuilding));

            // 사건 만들기는 시간 제한이 없으므로 아무리 기다려도 넘어가지 않는다.
            machine.Tick(1000f);
            Assert.That(machine.Current.Phase, Is.EqualTo(MatchPhase.CaseBuilding));
        }
    }
}
