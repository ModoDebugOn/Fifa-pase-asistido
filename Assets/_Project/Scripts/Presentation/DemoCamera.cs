using AssistedPass.Application;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AssistedPass.Presentation
{
    /// <summary>
    /// Camera that follows the ball like football games do: it does not stick to it, it chases it
    /// smoothly and looks a bit ahead of where it is going. It has several shots (C to switch)
    /// and blends between them smoothly, without cuts.
    /// Shots marked "alignWithAim" rotate with the direction the player is aiming,
    /// read (only read) from the Play.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class DemoCamera : MonoBehaviour
    {
        [System.Serializable]
        public class Shot
        {
            public string name = "TV";
            [Tooltip("Camera position relative to the point it follows (m).")]
            public Vector3 offset = new Vector3(0f, 22f, -34f);
            [Range(10f, 90f)] public float fieldOfView = 40f;
            [Tooltip("How much it follows the ball across the pitch width (0 = stays centered, 1 = fully).")]
            [Range(0f, 1f)] public float widthFollow = 0.4f;
            [Tooltip("Metres it looks ahead of the ball, along the shot's forward direction.")]
            public float lead = 6f;
            [Tooltip("Rotates with the player's aim: forward (+X of the offset) becomes the aim direction " +
                     "instead of the opponent's goal.")]
            public bool alignWithAim;
        }

        [Tooltip("What the camera follows: the ball.")]
        [SerializeField] Transform target;
        [Tooltip("Center of the pitch the camera films. Empty = world origin.")]
        [SerializeField] Transform pitchCenter;

        [SerializeField] Shot[] shots =
        {
            // Broadcast: side-on and elevated, like the main TV camera.
            new Shot { name = "TV", offset = new Vector3(0f, 22f, -34f), fieldOfView = 40f, widthFollow = 0.4f, lead = 6f },
            // Almost top-down: the best one to see the geometry of the cone.
            new Shot { name = "Tactical", offset = new Vector3(-4f, 42f, -14f), fieldOfView = 45f, widthFollow = 1f, lead = 12f },
            // Behind the passer, close to the grass, looking at the attack: the spectacular shot.
            new Shot { name = "Behind", offset = new Vector3(-22f, 7f, 0f), fieldOfView = 55f, widthFollow = 1f, lead = 10f, alignWithAim = true },
        };
        [SerializeField] int initialShot = 0;

        [Header("Movement")]
        [Tooltip("Time (s) to catch up with the ball. Higher = smoother and lazier.")]
        [SerializeField, Range(0.05f, 2f)] float smoothTime = 0.45f;
        [Tooltip("Time (s) of the blend between shots.")]
        [SerializeField, Range(0.1f, 3f)] float shotSmoothTime = 0.8f;
        [Tooltip("Seconds it looks ahead along the ball's path while it travels.")]
        [SerializeField, Range(0f, 1f)] float lookAhead = 0.35f;
        [Tooltip("The camera does not follow beyond this X, so it doesn't show too much outside the pitch.")]
        [SerializeField] float limitX = 40f;
        [Tooltip("Time (s) to turn towards the aim direction in the shots that align with it.")]
        [SerializeField, Range(0.05f, 2f)] float headingSmoothTime = 0.5f;

        [Header("Narrow image (reels 9:16, split screen)")]
        [Tooltip("How much it moves away when the image is narrower than 16:9. " +
                 "0 = same distance (closer look), 1 = shows as much pitch as in 16:9.")]
        [SerializeField, Range(0f, 1f)] float narrowFit = 0.6f;

        Camera cam;
        Play play;
        int shotIndex;
        Vector3 focus, focusVelocity;
        Vector3 offset, offsetVelocity;
        float fov, fovVelocity;
        // Heading of the shot around Y: 0 = looking at the opponent's goal (+X).
        float yaw, yawVelocity, aimYaw;
        Vector3 previousPosition;

        public Shot CurrentShot => shots[shotIndex];

        /// <summary>Optional: without it, every shot looks towards the opponent's goal.</summary>
        public void Initialize(Play play) => this.play = play;

        void Awake()
        {
            cam = GetComponent<Camera>();
            shotIndex = Mathf.Clamp(initialShot, 0, shots.Length - 1);
        }

        // First frame without blending: the camera starts already framed.
        void Start() => Snap();

        /// <summary>Switches to the shot with that name (as C does), blending or instantly.</summary>
        public void SetShot(string shotName, bool instant)
        {
            int index = System.Array.FindIndex(shots, s => s.name == shotName);
            if (index < 0) return;
            shotIndex = index;
            if (instant) Snap();
        }

        // Jumps to the current shot with no blending.
        void Snap()
        {
            if (target != null) previousPosition = target.position;
            ReadAim();
            yaw = TargetYaw();
            focus = DesiredFocus(Vector3.zero);
            offset = CurrentShot.offset;
            fov = CurrentShot.fieldOfView;
            focusVelocity = offsetVelocity = Vector3.zero;
            yawVelocity = fovVelocity = 0f;
            Apply();
        }

        void LateUpdate()
        {
            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
                shotIndex = (shotIndex + 1) % shots.Length;

            if (target == null) return;

            // Ball velocity: while it travels, the camera looks at where it is going to be.
            float dt = Mathf.Max(Time.deltaTime, 1e-5f);
            Vector3 ballVelocity = (target.position - previousPosition) / dt;
            previousPosition = target.position;

            ReadAim();
            yaw = Mathf.SmoothDampAngle(yaw, TargetYaw(), ref yawVelocity, headingSmoothTime);
            focus = Vector3.SmoothDamp(focus, DesiredFocus(ballVelocity), ref focusVelocity, smoothTime);
            offset = Vector3.SmoothDamp(offset, CurrentShot.offset, ref offsetVelocity, shotSmoothTime);
            fov = Mathf.SmoothDamp(fov, CurrentShot.fieldOfView, ref fovVelocity, shotSmoothTime);
            Apply();
        }

        // Aim direction of the latest decision. While the ball travels it is frozen,
        // so the camera keeps looking along the pass. After a reset (no decision) it keeps the last one.
        void ReadAim()
        {
            if (play?.LastResult == null) return;
            var d = play.LastResult.Direction; // pitch X = world X, pitch Y = world Z
            aimYaw = Mathf.Atan2(-d.Y, d.X) * Mathf.Rad2Deg;
        }

        float TargetYaw() => CurrentShot.alignWithAim ? aimYaw : 0f;

        // Rotation that turns the shot's "forward" (+X) into the current heading.
        Quaternion Heading => Quaternion.Euler(0f, yaw, 0f);

        // Point on the grass the camera wants at the center of the frame.
        Vector3 DesiredFocus(Vector3 ballVelocity)
        {
            if (target == null) return Vector3.zero;
            Shot s = CurrentShot;
            Vector3 center = pitchCenter != null ? pitchCenter.position : Vector3.zero;
            Vector3 b = target.position + ballVelocity * lookAhead - center; // relative to the pitch
            Vector3 lead = Heading * (Vector3.right * s.lead);
            float x = Mathf.Clamp(b.x + lead.x, -limitX, limitX);
            // Turned (vertical image) the pitch width is the narrow side: follow the ball fully.
            float z = b.z * (Turned ? 1f : s.widthFollow) + lead.z;
            return center + new Vector3(x, 0f, z);
        }

        // In a vertical image the shot is turned 90°: the camera goes behind the play and the
        // attack runs from the bottom to the top of the screen, along its long side.
        // Shots that align with the aim already look along the play, so they are never turned.
        bool Turned => cam.aspect < 1f && !CurrentShot.alignWithAim;

        // How much further the camera goes when the image is narrower than 16:9, so the play still fits.
        float NarrowDistance()
        {
            if (CurrentShot.alignWithAim) return 1f; // the play runs into the depth of the image
            const float reference = 16f / 9f;
            float alongPlay = Turned ? 1f : cam.aspect; // screen size along the attack, in heights
            return Mathf.Lerp(1f, Mathf.Max(1f, reference / alongPlay), narrowFit);
        }

        void Apply()
        {
            Quaternion turn = Turned ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            transform.position = focus + Heading * (turn * offset) * NarrowDistance();
            transform.rotation = Quaternion.LookRotation(focus - transform.position);
            cam.fieldOfView = fov;
        }
    }
}
