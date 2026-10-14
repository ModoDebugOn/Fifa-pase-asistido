using System;
using System.Numerics;

namespace AssistedPass.Domain
{
    /// <summary>Geometry helpers on the pitch plane.</summary>
    public static class Geometry
    {
        public const float DegToRad = MathF.PI / 180f;

        /// <summary>
        /// Splits vector "v" relative to a (normalized) direction:
        /// forward = how far ahead it is, lateral = how far it is from the line (always ≥ 0).
        /// </summary>
        public static (float forward, float lateral) Decompose(Vector2 v, Vector2 direction)
        {
            float forward = Vector2.Dot(v, direction);
            float lateral = MathF.Abs(direction.X * v.Y - direction.Y * v.X);
            return (forward, lateral);
        }

        /// <summary>Normalizes the vector; if it is (almost) zero, returns the fallback.</summary>
        public static Vector2 NormalizeOr(Vector2 v, Vector2 fallback)
        {
            float length = v.Length();
            return length > 1e-5f ? v / length : fallback;
        }
    }
}
