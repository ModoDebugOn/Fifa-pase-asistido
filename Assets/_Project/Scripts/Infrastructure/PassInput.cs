using UnityEngine;
using UnityEngine.InputSystem;

namespace AssistedPass.Infrastructure
{
    /// <summary>
    /// Translates the input into "where the player wants to pass".
    /// - Gamepad: left stick, relative to the camera (as in football games).
    /// - Mouse: from the player towards the point on the grass under the cursor.
    /// Pass: Space or A button. Reset play: R or Y button.
    /// </summary>
    public class PassInput
    {
        readonly float deadZone;
        Vector3 lastDirection = Vector3.right;

        /// <summary>Camera the mouse and the stick are relative to. Can change (split screen).</summary>
        public Camera Camera { get; set; }

        /// <summary>Optional input that replaces the real devices while it is active.</summary>
        public static IInputOverride Override { get; set; }

        static bool Overridden => Override != null && Override.Active;

        public PassInput(Camera cam, float deadZone)
        {
            Camera = cam;
            this.deadZone = deadZone;
        }

        /// <summary>Input direction on the grass (y = 0, normalized).</summary>
        public Vector3 ReadDirection(Vector3 origin)
        {
            // 0) Another input source has taken over.
            if (Overridden)
                return lastDirection = Override.Direction.normalized;

            // 1) Gamepad: if the stick is moving, it takes priority.
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (stick.magnitude > deadZone)
                {
                    lastDirection = CameraRelative(stick);
                    return lastDirection;
                }
            }

            // 2) Mouse: ray from the camera to the ground plane.
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Ray ray = Camera.ScreenPointToRay(mouse.position.ReadValue());
                var ground = new Plane(Vector3.up, Vector3.zero);
                if (ground.Raycast(ray, out float distance))
                {
                    Vector3 toCursor = ray.GetPoint(distance) - origin;
                    toCursor.y = 0f;
                    if (toCursor.sqrMagnitude > 0.01f)
                        lastDirection = toCursor.normalized;
                }
            }

            return lastDirection;
        }

        public bool PassPressed => Overridden ? Override.ConsumePass() :
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        public bool ResetPressed => Overridden ? Override.ConsumeReset() :
            (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);

        Vector3 CameraRelative(Vector2 stick)
        {
            Vector3 forward = Vector3.ProjectOnPlane(Camera.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(Camera.transform.right, Vector3.up).normalized;
            return (forward * stick.y + right * stick.x).normalized;
        }
    }
}
