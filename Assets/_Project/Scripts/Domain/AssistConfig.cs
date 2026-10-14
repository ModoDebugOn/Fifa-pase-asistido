using System;

namespace AssistedPass.Domain
{
    /// <summary>
    /// Everything that defines "how much the game helps you" when passing.
    /// Values are clamped on assignment so the configuration is never invalid.
    /// </summary>
    public sealed class AssistConfig
    {
        float level = 1f;
        float maxAngle = 40f;
        float range = 40f;
        float controlRadius = 1f;
        float lostPassDistance = 20f;

        /// <summary>0 = manual (the cone is a line), 1 = maximum help (cone fully open).</summary>
        public float Level { get => level; set => level = Math.Clamp(value, 0f, 1f); }

        /// <summary>Half-angle of the cone, in degrees, at maximum assistance.</summary>
        public float MaxAngle { get => maxAngle; set => maxAngle = Math.Clamp(value, 0f, 80f); }

        /// <summary>How far receivers are searched for (m).</summary>
        public float Range { get => range; set => range = Math.Max(1f, value); }

        /// <summary>Half-width of the "line" at assistance 0 (m): the receiver's margin to control the ball.</summary>
        public float ControlRadius { get => controlRadius; set => controlRadius = Math.Max(0.1f, value); }

        /// <summary>With no receiver, the ball travels in the input direction up to this distance (m).</summary>
        public float LostPassDistance { get => lostPassDistance; set => lostPassDistance = Math.Max(1f, value); }

        /// <summary>Actual half-angle of the cone for the current assistance level (degrees).</summary>
        public float HalfAngle => maxAngle * level;

        /// <summary>
        /// Half-width of the cone at a distance "forward" from the passer.
        /// It is a cone with thickness: at level 0 it becomes a strip of fixed width (the "line").
        /// </summary>
        public float HalfWidthAt(float forward) =>
            MathF.Tan(HalfAngle * Geometry.DegToRad) * MathF.Max(0f, forward) + controlRadius;

        public AssistConfig Clone() => (AssistConfig)MemberwiseClone();
    }
}
