using AssistedPass.Domain;
using UnityEngine;

namespace AssistedPass.Infrastructure
{
    /// <summary>
    /// Assist profile editable in the Inspector (High, Medium, Low...).
    /// It is just the "Unity format" of AssistConfig: at startup it is converted to the domain object.
    /// </summary>
    [CreateAssetMenu(fileName = "Assist_", menuName = "Assisted Pass/Assist profile")]
    public class AssistProfile : ScriptableObject
    {
        [Tooltip("Name shown on the demo panel button.")]
        public string displayName;

        [Tooltip("0 = manual: the cone closes down to a line.\n1 = maximum help: the cone opens fully.")]
        [Range(0f, 1f)] public float level = 1f;

        [Tooltip("Half-angle of the cone (degrees) at maximum assistance.")]
        [Range(0f, 80f)] public float maxAngle = 40f;

        [Tooltip("How far receivers are searched for (metres).")]
        [Min(1f)] public float range = 40f;

        [Tooltip("Half-width of the 'line' at assistance 0 (metres): the receiver's margin to control the ball.")]
        [Min(0.1f)] public float controlRadius = 1f;

        [Tooltip("With no receiver, the ball travels in the input direction up to this distance (metres).")]
        [Min(1f)] public float lostPassDistance = 20f;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        public AssistConfig CreateConfig() => new AssistConfig
        {
            Level = level,
            MaxAngle = maxAngle,
            Range = range,
            ControlRadius = controlRadius,
            LostPassDistance = lostPassDistance
        };
    }
}
