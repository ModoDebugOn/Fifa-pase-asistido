using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;

namespace AssistedPass.Domain.Tests
{
    /// <summary>
    /// Cases the video shows: what the assisted pass does compared with the manual one.
    /// The passer is always at (0,0) and aims towards +X.
    /// </summary>
    public class ReceiverSelectorTests
    {
        static readonly Vector2 Ahead = Vector2.UnitX;

        Player passer;
        List<Player> players;

        [SetUp]
        public void SetUp()
        {
            passer = new Player(0, Team.Home, Vector2.Zero);
            players = new List<Player> { passer };
        }

        Player Teammate(float x, float y) => Add(Team.Home, x, y);
        Player Opponent(float x, float y) => Add(Team.Away, x, y);

        Player Add(Team team, float x, float y)
        {
            var p = new Player(players.Count, team, new Vector2(x, y));
            players.Add(p);
            return p;
        }

        static AssistConfig Assist(float level) => new AssistConfig { Level = level };

        PassResult Evaluate(AssistConfig config) =>
            ReceiverSelector.Evaluate(passer, Ahead, players, config);

        [Test]
        public void WithHighAssist_AimingBadlyStillReachesTeammate()
        {
            var teammate = Teammate(18f, 6f); // about 18° off the input line

            var r = Evaluate(Assist(1f));

            Assert.That(r.Chosen?.Player, Is.EqualTo(teammate));
        }

        [Test]
        public void WithManualAssist_AimingBadlyLosesThePass()
        {
            Teammate(18f, 6f);

            var r = Evaluate(Assist(0f));

            Assert.That(r.IsLost, Is.True);
            Assert.That(r.Target, Is.EqualTo(new Vector2(r.Config.LostPassDistance, 0f)));
        }

        [Test]
        public void WithManualAssist_AimingWellReachesTeammate()
        {
            var teammate = Teammate(18f, 0.5f); // within the control radius

            var r = Evaluate(Assist(0f));

            Assert.That(r.Chosen?.Player, Is.EqualTo(teammate));
        }

        [Test]
        public void ChosenIsTheBestAlignedWithInput()
        {
            Teammate(18f, 8f);
            var aligned = Teammate(18f, 1f);

            var r = Evaluate(Assist(1f));

            Assert.That(r.Chosen.Player, Is.EqualTo(aligned));
        }

        [Test]
        public void WithTwoOnTheSameLine_ClosestReceives()
        {
            Teammate(30f, 0f);
            var closest = Teammate(10f, 0f);

            var r = Evaluate(Assist(1f));

            Assert.That(r.Chosen.Player, Is.EqualTo(closest));
        }

        [Test]
        public void TeammateBehindPasserIsNotACandidate()
        {
            Teammate(-15f, 0f);

            var r = Evaluate(Assist(1f));

            Assert.That(r.Candidates[0].InsideCone, Is.False);
            Assert.That(r.IsLost, Is.True);
        }

        [Test]
        public void OpponentOnTheLineDoesNotAffectThePass()
        {
            var teammate = Teammate(18f, 0f);
            float withoutOpponent = Evaluate(Assist(1f)).Chosen.Score;

            Opponent(9f, 0f);
            var r = Evaluate(Assist(1f));

            Assert.That(r.Chosen.Player, Is.EqualTo(teammate));
            Assert.That(r.Chosen.Score, Is.EqualTo(withoutOpponent));
        }

        [Test]
        public void OpponentsAreNeverCandidates()
        {
            Opponent(18f, 0f);

            var r = Evaluate(Assist(1f));

            Assert.That(r.Candidates, Is.Empty);
        }
    }
}
