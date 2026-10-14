using UnityEngine;
using PitchVector = System.Numerics.Vector2;

namespace AssistedPass.Infrastructure
{
    /// <summary>
    /// The domain works in 2D (the grass plane) and Unity in 3D.
    /// Domain X = Unity X (pitch length); domain Y = Unity Z (pitch width).
    /// </summary>
    public static class PitchConversion
    {
        public static PitchVector ToPitch(Vector3 world) => new PitchVector(world.x, world.z);

        public static Vector3 ToWorld(PitchVector pitch, float height = 0f) => new Vector3(pitch.X, height, pitch.Y);
    }
}
