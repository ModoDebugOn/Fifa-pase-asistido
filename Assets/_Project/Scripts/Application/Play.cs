using System;
using System.Collections.Generic;
using System.Linq;
using AssistedPass.Domain;

namespace AssistedPass.Application
{
    public enum PlayPhase
    {
        InPossession,  // someone has the ball and is aiming
        BallInFlight,  // the ball is travelling to its target
        PassLost       // the ball reached a spot with nobody there
    }

    /// <summary>
    /// State of the demo: who is on the pitch, who has the ball and what was decided.
    /// Only the use cases modify it; everything else (visualization, panel) only reads it.
    /// </summary>
    public sealed class Play
    {
        AssistConfig config;

        public IReadOnlyList<Player> Players { get; }
        public Player InitialHolder { get; }

        /// <summary>Who has the ball. Null while it travels or after a lost pass.</summary>
        public Player Holder { get; internal set; }

        public PlayPhase Phase { get; internal set; }

        /// <summary>
        /// Latest decision of the pass system. While the ball travels it stays frozen,
        /// so we can explain why it went where it went.
        /// </summary>
        public PassResult LastResult { get; internal set; }

        /// <summary>Configuration in use. Only changed through the ChangeAssist use case.</summary>
        public AssistConfig Config
        {
            get => config;
            internal set => config = value ?? throw new ArgumentNullException(nameof(Config));
        }

        public Play(IEnumerable<Player> players, Player initialHolder, AssistConfig config)
        {
            Players = players.ToList();
            if (!Players.Contains(initialHolder))
                throw new ArgumentException("The initial holder must be part of the play.", nameof(initialHolder));

            InitialHolder = initialHolder;
            Holder = initialHolder;
            Phase = PlayPhase.InPossession;
            Config = config;
        }
    }
}
