using System;
using System.Numerics;
using NUnit.Framework;
using AssistedPass.Application;
using AssistedPass.Domain;

namespace AssistedPass.Tests
{
    /// <summary>
    /// Full flow of a play: aim, pass, the ball arrives (or not) and reset.
    /// The passer is at (0,0); there is a teammate ahead, at (18,0).
    /// </summary>
    public class PlayTests
    {
        /// <summary>Fake ball: stores the pass and lets the test decide when it arrives.</summary>
        class FakeBall : IBall
        {
            public Player GivenTo;
            public Vector2? Target;
            Action onArrival;

            public void GiveTo(Player player) { GivenTo = player; Target = null; }
            public void Pass(Vector2 target, Action onArrival) { Target = target; this.onArrival = onArrival; }
            public void Arrive() => onArrival?.Invoke();
        }

        static readonly Vector2 TowardsTeammate = Vector2.UnitX;
        static readonly Vector2 TowardsNobody = -Vector2.UnitX;

        Player passer, teammate;
        Play play;
        FakeBall ball;
        EvaluatePass evaluate;
        ExecutePass pass;
        ResetPlay reset;

        [SetUp]
        public void SetUp()
        {
            passer = new Player(1, Team.Home, Vector2.Zero);
            teammate = new Player(2, Team.Home, new Vector2(18f, 0f));
            play = new Play(new[] { passer, teammate }, passer, new AssistConfig());

            ball = new FakeBall();
            evaluate = new EvaluatePass(play);
            pass = new ExecutePass(play, evaluate, ball);
            reset = new ResetPlay(play, ball);
        }

        [Test]
        public void OnPass_BallHeadsToReceiverAndNobodyHasIt()
        {
            pass.Execute(TowardsTeammate);

            Assert.That(ball.Target, Is.EqualTo(teammate.Position));
            Assert.That(play.Holder, Is.Null);
            Assert.That(play.Phase, Is.EqualTo(PlayPhase.BallInFlight));
        }

        [Test]
        public void OnArrival_ReceiverHasTheBall()
        {
            pass.Execute(TowardsTeammate);
            ball.Arrive();

            Assert.That(play.Holder, Is.EqualTo(teammate));
            Assert.That(play.Phase, Is.EqualTo(PlayPhase.InPossession));
            Assert.That(ball.GivenTo, Is.EqualTo(teammate));
        }

        [Test]
        public void WithNoReceiver_PassIsLost()
        {
            pass.Execute(TowardsNobody);
            ball.Arrive();

            Assert.That(play.Holder, Is.Null);
            Assert.That(play.Phase, Is.EqualTo(PlayPhase.PassLost));
        }

        [Test]
        public void WithBallInFlight_CannotPassAgain()
        {
            pass.Execute(TowardsTeammate);

            Assert.That(pass.Execute(TowardsTeammate), Is.False);
        }

        [Test]
        public void WithBallInFlight_DecisionStaysFrozen()
        {
            pass.Execute(TowardsTeammate);
            var passDecision = play.LastResult;

            // Even if the input points elsewhere, the pass decision is still shown.
            Assert.That(evaluate.Execute(TowardsNobody), Is.SameAs(passDecision));
            Assert.That(passDecision.Chosen.Player, Is.EqualTo(teammate));
        }

        [Test]
        public void ResetDuringPass_PassNoLongerCounts()
        {
            pass.Execute(TowardsTeammate);
            reset.Execute();
            ball.Arrive(); // arrives late: must be ignored

            Assert.That(play.Holder, Is.EqualTo(passer));
            Assert.That(play.Phase, Is.EqualTo(PlayPhase.InPossession));
        }

        [Test]
        public void InitialHolderOutsideThePlay_IsAnError()
        {
            var intruder = new Player(99, Team.Home, Vector2.Zero);

            Assert.Throws<ArgumentException>(() =>
                new Play(new[] { passer }, intruder, new AssistConfig()));
        }
    }
}
