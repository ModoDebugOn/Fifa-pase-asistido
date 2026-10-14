using System.Collections.Generic;
using System.Numerics;

namespace AssistedPass.Domain
{
    /// <summary>
    /// The "brain" of the assisted pass:
    /// 1. Finds which teammates fall inside the cone cast in the input direction.
    /// 2. Scores each one by how well aligned it is with the input.
    /// 3. Picks the best (on a tie, the closest: the ball reaches them sooner).
    ///    If there is none, the ball goes where you aim.
    /// </summary>
    public static class ReceiverSelector
    {
        const float TieMargin = 0.001f;

        public static PassResult Evaluate(
            Player passer, Vector2 inputDirection, IEnumerable<Player> players, AssistConfig config)
        {
            Vector2 direction = Geometry.NormalizeOr(inputDirection, Vector2.UnitX);
            var result = new PassResult(passer, direction, config);
            Vector2 origin = passer.Position;

            foreach (Player player in players)
            {
                if (!passer.IsTeammateOf(player)) continue;

                var c = new Candidate(player);
                Vector2 toPlayer = player.Position - origin;

                // Teammate position relative to the input direction:
                // how far ahead it is (forward) and how far it is from the line (lateral).
                (c.Forward, c.Lateral) = Geometry.Decompose(toPlayer, direction);
                c.Distance = toPlayer.Length();

                float halfWidth = config.HalfWidthAt(c.Forward);
                c.InsideCone = c.Forward > 0f && c.Forward <= config.Range && c.Lateral <= halfWidth;

                if (c.InsideCone)
                {
                    // 1 if right on the input line, 0 if on the edge of the cone.
                    c.Score = 1f - c.Lateral / halfWidth;

                    if (IsBetter(c, result.Chosen))
                        result.Chosen = c;
                }

                result.candidates.Add(c);
            }

            result.Target = result.Chosen != null
                ? result.Chosen.Player.Position
                : origin + direction * config.LostPassDistance;

            return result;
        }

        static bool IsBetter(Candidate c, Candidate current)
        {
            if (current == null) return true;
            if (c.Score > current.Score + TieMargin) return true;
            if (c.Score < current.Score - TieMargin) return false;
            return c.Distance < current.Distance; // tie: the closest wins
        }
    }
}
