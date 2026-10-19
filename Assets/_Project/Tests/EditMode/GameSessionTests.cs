using System.Collections.Generic;
using System.Linq;
using CourtOfNi.Core.Events;
using CourtOfNi.Core.Match;
using CourtOfNi.Core.Phases;
using CourtOfNi.Core.Roles;
using NUnit.Framework;

namespace CourtOfNi.Core.Tests
{
    public class GameSessionTests
    {
        [Test]
        public void FullGame_PlaysOneLapAndPicksWinner()
        {
            var players = TestFixtures.Players(5);
            var game = new GameSession(players, TestFixtures.ManualRules(rounds: 2), TestFixtures.Evidence(6), null, seed: 42);
            var events = new List<IGameEvent>();
            game.EventRaised += events.Add;

            while (game.CanStartNextMatch)
            {
                var match = game.StartNextMatch();
                var judge = match.Assignment.JudgeId;
                while (!match.IsFinished)
                {
                    if (match.Phase.Phase == MatchPhase.CaseBuilding) match.DefineCase(judge, "사건");
                    else if (match.Phase.Phase == MatchPhase.Voting)
                        foreach (var juror in match.Assignment.JurorIds) match.CastVote(juror, match.Assignment.DefendantIds[0]);
                    else match.RequestAdvance(judge);
                }
            }

            Assert.That(game.MatchesPlayed, Is.EqualTo(5));
            Assert.That(game.IsGameOver, Is.True);
            foreach (var p in players) Assert.That(game.Rotation.CountOf(p.Id, Role.Judge), Is.EqualTo(1));

            var over = events.OfType<GameOverEvent>().Single();
            var best = players.Max(p => p.Score);
            Assert.That(over.WinnerIds, Is.EquivalentTo(players.Where(p => p.Score == best).Select(p => p.Id)));
            Assert.That(events.OfType<MatchStartedEvent>().Count(), Is.EqualTo(5));
        }
    }
}
