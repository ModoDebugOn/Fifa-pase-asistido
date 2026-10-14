using UnityEngine;

namespace AssistedPass.Infrastructure
{
    /// <summary>
    /// Another source of input that can take over from the real devices: an automated demo,
    /// a test... While it is Active, PassInput ignores the mouse, keyboard and gamepad.
    /// Nothing in the demo uses it; it is there so other tools can drive the play.
    /// </summary>
    public interface IInputOverride
    {
        bool Active { get; }

        /// <summary>Aim direction on the grass (y = 0).</summary>
        Vector3 Direction { get; }

        /// <summary>True once per press.</summary>
        bool ConsumePass();

        /// <summary>True once per press.</summary>
        bool ConsumeReset();
    }
}
