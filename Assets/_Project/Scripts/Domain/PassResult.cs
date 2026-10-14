using System.Collections.Generic;
using System.Numerics;

namespace AssistedPass.Domain
{
    /// <summary>A teammate evaluated as a possible receiver, with all its measurements.</summary>
    public sealed class Candidate
    {
        public Player Player { get; }

        /// <summary>Metres "ahead" along the input direction.</summary>
        public float Forward { get; internal set; }
        /// <summary>Metres away from the input line.</summary>
        public float Lateral { get; internal set; }
        /// <summary>Metres from the passer.</summary>
        public float Distance { get; internal set; }

        public bool InsideCone { get; internal set; }

        /// <summary>
        /// How well aligned it is with the input, from 0 to 1:
        /// 1 = right on the input line, 0 = on the edge of the cone.
        /// </summary>
        public float Score { get; internal set; }

        internal Candidate(Player player) => Player = player;
    }

    /// <summary>What the system decided for a given input.</summary>
    public sealed class PassResult
    {
        public Player Passer { get; }
        public Vector2 Origin { get; }
        public Vector2 Direction { get; }
        public AssistConfig Config { get; }

        /// <summary>Every teammate evaluated, inside the cone or not.</summary>
        public IReadOnlyList<Candidate> Candidates => candidates;

        /// <summary>Best candidate inside the cone. Null = nobody: the pass is lost.</summary>
        public Candidate Chosen { get; internal set; }

        /// <summary>Point the ball is sent to.</summary>
        public Vector2 Target { get; internal set; }

        public bool IsLost => Chosen == null;

        internal readonly List<Candidate> candidates = new List<Candidate>();

        internal PassResult(Player passer, Vector2 direction, AssistConfig config)
        {
            Passer = passer;
            Origin = passer.Position;
            Direction = direction;
            Config = config;
        }
    }
}
