using System.Numerics;
using AssistedPass.Domain;

namespace AssistedPass.Application
{
    /// <summary>
    /// "If I passed now, who would get it?". Called every frame while the player aims,
    /// so the visualization shows the cone and the candidates in real time.
    /// </summary>
    public sealed class EvaluatePass
    {
        readonly Play play;

        public EvaluatePass(Play play) => this.play = play;

        /// <summary>
        /// Evaluates the pass in the given direction. If nobody has the ball, it does not
        /// recalculate and returns the latest decision (or null if there is none).
        /// </summary>
        public PassResult Execute(Vector2 direction)
        {
            if (play.Phase != PlayPhase.InPossession)
                return play.LastResult;

            play.LastResult = ReceiverSelector.Evaluate(
                play.Holder, direction, play.Players, play.Config);

            return play.LastResult;
        }
    }
}
