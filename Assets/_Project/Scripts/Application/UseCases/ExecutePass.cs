using System.Numerics;
using AssistedPass.Domain;

namespace AssistedPass.Application
{
    /// <summary>
    /// The player presses pass: the receiver is decided and the ball heads towards them.
    /// On arrival the receiver gets the ball; if there was no receiver, the pass is lost.
    /// </summary>
    public sealed class ExecutePass
    {
        readonly Play play;
        readonly EvaluatePass evaluate;
        readonly IBall ball;

        public ExecutePass(Play play, EvaluatePass evaluate, IBall ball)
        {
            this.play = play;
            this.evaluate = evaluate;
            this.ball = ball;
        }

        /// <summary>Returns false if passing is not possible right now (nobody has the ball).</summary>
        public bool Execute(Vector2 direction)
        {
            if (play.Phase != PlayPhase.InPossession) return false;

            PassResult result = evaluate.Execute(direction);

            play.Holder = null;
            play.Phase = PlayPhase.BallInFlight;
            ball.Pass(result.Target, () => OnArrival(result));
            return true;
        }

        void OnArrival(PassResult result)
        {
            // If the play was reset while the ball was travelling, this pass no longer counts.
            if (play.Phase != PlayPhase.BallInFlight || play.LastResult != result) return;

            if (result.IsLost)
            {
                play.Phase = PlayPhase.PassLost;
                return;
            }

            Player receiver = result.Chosen.Player;
            play.Holder = receiver;
            play.Phase = PlayPhase.InPossession;
            ball.GiveTo(receiver);
        }
    }
}
