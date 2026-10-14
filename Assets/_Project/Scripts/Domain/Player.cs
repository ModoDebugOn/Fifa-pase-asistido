using System.Numerics;

namespace AssistedPass.Domain
{
    public enum Team { Home, Away }

    /// <summary>
    /// A player as the domain sees it: a team and a point on the grass.
    /// The pitch is a 2D plane: X = pitch length, Y = pitch width.
    /// </summary>
    public sealed class Player
    {
        public int Id { get; }
        public Team Team { get; }

        /// <summary>Position of the player's feet on the grass (metres).</summary>
        public Vector2 Position { get; set; }

        public Player(int id, Team team, Vector2 position)
        {
            Id = id;
            Team = team;
            Position = position;
        }

        public bool IsTeammateOf(Player other) => other != this && other.Team == Team;

        public override string ToString() => $"{Team} #{Id}";
    }
}
