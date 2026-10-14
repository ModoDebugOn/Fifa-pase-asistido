using System;
using System.Numerics;
using AssistedPass.Domain;

namespace AssistedPass.Application
{
    /// <summary>
    /// What the application needs from the "physical" ball. Moving it takes time and animation,
    /// so the engine (Infrastructure) implements it and reports back when it is done.
    /// </summary>
    public interface IBall
    {
        /// <summary>Puts the ball at the player's feet (cancels any pass in progress).</summary>
        void GiveTo(Player player);

        /// <summary>Sends the ball to a point on the grass and calls onArrival when it gets there.</summary>
        void Pass(Vector2 target, Action onArrival);
    }
}
