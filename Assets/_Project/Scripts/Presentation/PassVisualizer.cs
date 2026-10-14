using System.Collections.Generic;
using AssistedPass.Application;
using AssistedPass.Domain;
using AssistedPass.Presentation.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using PitchVector = System.Numerics.Vector2;

namespace AssistedPass.Presentation
{
    /// <summary>
    /// Draws in the game view what the pass system is "thinking".
    /// It does not use Gizmos (editor-only, 1-pixel lines): every frame it builds
    /// a mesh with thick strokes and fills that read well on video.
    /// Each layer can be toggled separately (keys 1-5) to explain step by step.
    /// </summary>
    public class PassVisualizer : MonoBehaviour
    {
        [Tooltip("Material with vertex colors and transparency (Sprites/Default shader).")]
        [SerializeField] Material material;

        [Header("Layers (keys 1-5)")]
        public bool showCone = true;        // 1
        public bool showDirection = true;   // 2
        public bool showCandidates = true;  // 3
        public bool showScores = true;      // 4
        public bool showChosen = true;      // 5
        [Tooltip("While the ball travels, the cone and lines are hidden so they don't cover it; " +
                 "only the receiver and candidate rings remain.")]
        public bool hideLinesWhileBallInFlight = true;

        [Header("Cone")]
        [Tooltip("Cone color at maximum assistance (the game helps you).")]
        public Color coneColorAssisted = new Color(0.1f, 0.85f, 1f);
        [Tooltip("Cone color at assistance 0 (manual pass).")]
        public Color coneColorManual = new Color(1f, 0.15f, 0.35f);
        [SerializeField, Range(0f, 1f)] float coneAlphaNear = 0.5f;
        [SerializeField, Range(0f, 1f)] float coneAlphaFar = 0.08f;
        [Tooltip("Waves per second travelling outwards along the cone. 0 = no sweep.")]
        [SerializeField] float sweepSpeed = 0.8f;
        [SerializeField, Range(0.02f, 0.5f)] float waveWidth = 0.12f;
        [SerializeField, Range(0f, 1f)] float waveIntensity = 0.45f;
        [SerializeField] int coneSlices = 60;

        [Header("Colors")]
        public Color directionColor = Color.white;
        public Color candidateColor = Color.white;
        public Color outsideConeColor = new Color(0.75f, 0.75f, 0.75f, 0.45f);
        public Color chosenColor = new Color(1f, 0.85f, 0.1f);
        public Color lostColor = new Color(1f, 0.25f, 0.2f);

        [Header("Animation")]
        [Tooltip("Beats per second of the rings and of the lost-pass cross.")]
        [SerializeField] float pulseSpeed = 1.2f;
        [Tooltip("Speed (m/s) of the dashes travelling along the pass line.")]
        [SerializeField] float flowSpeed = 8f;
        [SerializeField] float dashSpacing = 2f;

        [Header("Sizes")]
        [SerializeField] float lineWidth = 0.25f;
        [SerializeField] float ringRadius = 1.1f;
        [SerializeField] float heightAboveGrass = 0.05f;
        [SerializeField] int fontSize = 28;
        [Tooltip("Camera used to place the score labels. Empty = main camera (set it in split screen).")]
        [SerializeField] Camera labelCamera;

        Play play;
        Mesh mesh;
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> triangles = new List<int>();
        Canvas labelCanvas;
        readonly List<Text> labels = new List<Text>();

        public void Initialize(Play play) => this.play = play;

        void Awake()
        {
            mesh = new Mesh { name = "PassVisualizer" };
            mesh.MarkDynamic();
            labelCanvas = Ui.CreateCanvas("Score Labels", sortingOrder: 10, LabelCamera);
            labelCanvas.transform.SetParent(transform, false);
        }

        Camera LabelCamera => labelCamera != null ? labelCamera : Camera.main;

        // LateUpdate: draws after the controller has evaluated the pass this frame.
        void LateUpdate()
        {
            ReadLayerKeys();
            if (play == null || material == null) return;

            BuildMesh(play.LastResult);
            Graphics.RenderMesh(new RenderParams(material), mesh, 0, Matrix4x4.identity);
            UpdateScoreLabels(play.LastResult);
        }

        void ReadLayerKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.digit1Key.wasPressedThisFrame) showCone = !showCone;
            if (keyboard.digit2Key.wasPressedThisFrame) showDirection = !showDirection;
            if (keyboard.digit3Key.wasPressedThisFrame) showCandidates = !showCandidates;
            if (keyboard.digit4Key.wasPressedThisFrame) showScores = !showScores;
            if (keyboard.digit5Key.wasPressedThisFrame) showChosen = !showChosen;
        }

        // ------------------------------------------------------------------ layers

        void BuildMesh(PassResult r)
        {
            vertices.Clear(); colors.Clear(); triangles.Clear();

            if (r != null)
            {
                // With the ball in the air the lines would cover it: only the rings are kept.
                bool lines = !(hideLinesWhileBallInFlight && play.Phase == PlayPhase.BallInFlight);

                if (showCone && lines) DrawCone(r);
                if (showDirection && lines) DrawDirection(r);
                if (showCandidates) DrawCandidates(r);
                if (showChosen) DrawChosen(r, lines);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        void DrawCone(PassResult r)
        {
            // The cone is a trapezoid: "ControlRadius" wide next to the passer,
            // opening up by the half-angle until the range.
            AssistConfig config = r.Config;
            Vector3 dir = Direction(r);
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            Vector3 o = World(r.Origin);

            // The color tells how much the game helps you: cool = assisted, warm = manual.
            Color tint = ConeColor(config.Level);
            float phase = Mathf.Repeat(Time.time * sweepSpeed, 1f);

            // Fill in slices: strong next to the passer, fading further away,
            // with a light wave sweeping outwards ("the game is searching").
            for (int s = 0; s < coneSlices; s++)
            {
                float t0 = s / (float)coneSlices, t1 = (s + 1) / (float)coneSlices;
                float f0 = t0 * config.Range, f1 = t1 * config.Range;
                float w0 = config.HalfWidthAt(f0), w1 = config.HalfWidthAt(f1);
                Vector3 p0 = o + dir * f0, p1 = o + dir * f1;
                Color c0 = FillColor(tint, t0, phase), c1 = FillColor(tint, t1, phase);
                Quad(p0 - side * w0, p0 + side * w0, p1 + side * w1, p1 - side * w1, c0, c0, c1, c1);
            }

            // Glowing edges: a wide soft halo with a brighter line on top.
            Vector3 far = o + dir * config.Range;
            float nearWidth = config.HalfWidthAt(0f);
            float farWidth = config.HalfWidthAt(config.Range);
            Vector3 a = o - side * nearWidth, b = o + side * nearWidth;
            Vector3 c = far + side * farWidth, d = far - side * farWidth;

            Color halo = Faded(tint, 0.3f), haloFar = Faded(tint, 0.15f);
            Color core = Color.Lerp(tint, Color.white, 0.35f), coreFar = Faded(core, 0.5f);

            GradientLine(a, d, lineWidth * 3f, halo, haloFar);
            GradientLine(b, c, lineWidth * 3f, halo, haloFar);
            Line(d, c, lineWidth * 3f, haloFar);
            GradientLine(a, d, lineWidth * 0.8f, core, coreFar);
            GradientLine(b, c, lineWidth * 0.8f, core, coreFar);
            Line(d, c, lineWidth * 0.8f, coreFar);
        }

        /// <summary>
        /// Cone color for an assistance level: cool = assisted, warm = manual.
        /// Blended by hue, so the middle goes through violet and stays saturated
        /// (an RGB blend between cyan and red passes through a dull grey).
        /// </summary>
        public Color ConeColor(float level)
        {
            Color.RGBToHSV(coneColorManual, out float h0, out float s0, out float v0);
            Color.RGBToHSV(coneColorAssisted, out float h1, out float s1, out float v1);
            return Color.HSVToRGB(Mathf.Lerp(h0, h1, level), Mathf.Lerp(s0, s1, level), Mathf.Lerp(v0, v1, level));
        }

        // Fill alpha at position t (0 = passer, 1 = range), plus the wave peak.
        Color FillColor(Color tint, float t, float phase)
        {
            float alpha = Mathf.Lerp(coneAlphaNear, coneAlphaFar, t);
            float wave = Mathf.Clamp01(1f - Mathf.Abs(t - phase) / waveWidth);
            alpha += wave * wave * waveIntensity;
            return new Color(tint.r, tint.g, tint.b, Mathf.Clamp01(alpha));
        }

        void DrawDirection(PassResult r)
        {
            Vector3 o = World(r.Origin) + Vector3.up * 0.01f;
            Arrow(o, o + Direction(r) * Mathf.Min(r.Config.Range, 12f), lineWidth, directionColor);
        }

        void DrawCandidates(PassResult r)
        {
            for (int i = 0; i < r.Candidates.Count; i++)
            {
                Candidate c = r.Candidates[i];
                Vector3 p = World(c.Player.Position);
                if (!c.InsideCone)
                {
                    Ring(p, ringRadius, lineWidth * 0.8f, outsideConeColor);
                    continue;
                }

                // Inside the cone: the ring "beats" and emits waves, so you can tell at a glance
                // who is being evaluated. Each one with its own phase offset.
                Ring(p, ringRadius * Beat(0.08f), lineWidth, candidateColor);
                ExpandingWave(p, ringRadius, candidateColor, i * 0.37f);
            }
        }

        void DrawChosen(PassResult r, bool lines)
        {
            Vector3 o = World(r.Origin) + Vector3.up * 0.02f;
            Vector3 target = World(r.Target) + Vector3.up * 0.02f;

            if (!r.IsLost)
            {
                // Receiver: thick ring with halo, beat and wave.
                float radius = ringRadius * 1.4f;
                Ring(target, radius * Beat(0.06f), lineWidth * 3f, Faded(chosenColor, 0.3f));
                Ring(target, radius * Beat(0.06f), lineWidth * 1.2f, chosenColor);
                ExpandingWave(target, radius, chosenColor, 0f);
                if (!lines) return;

                // Pass line: halo + arrow + dashes travelling towards the receiver.
                Vector3 dir = (target - o).normalized;
                Line(o, target - dir * lineWidth * 6f, lineWidth * 3f, Faded(chosenColor, 0.3f));
                Arrow(o, target, lineWidth, chosenColor);
                MovingDashes(o, target - dir * lineWidth * 6f, lineWidth * 0.5f,
                             Color.Lerp(chosenColor, Color.white, 0.7f));
            }
            else
            {
                // Nobody in the cone: the ball goes where you aim and is lost.
                // Moving dashed line and a beating red cross.
                if (lines) MovingDashes(o, target, lineWidth, lostColor);
                float size = 0.9f * Beat(0.15f);
                Cross(target, size, lineWidth * 3f, Faded(lostColor, 0.3f));
                Cross(target, size, lineWidth * 1.2f, lostColor);
            }
        }

        // ------------------------------------------------------------------ animation

        // Scale factor oscillating around 1 (e.g. 0.08 → between 0.92 and 1.08).
        float Beat(float amplitude) => 1f + amplitude * Mathf.Sin(Time.time * pulseSpeed * 2f * Mathf.PI);

        // A ring that starts at "radius", grows and fades out, in a loop.
        void ExpandingWave(Vector3 center, float radius, Color color, float phaseOffset)
        {
            float t = Mathf.Repeat(Time.time * pulseSpeed * 0.5f + phaseOffset, 1f);
            Ring(center, radius * (1f + t * 0.9f), lineWidth * 0.6f, Faded(color, 1f - t));
        }

        // Dashes moving from "from" to "to": they give a sense of flow, of a ball on its way.
        void MovingDashes(Vector3 from, Vector3 to, float width, Color color)
        {
            Vector3 dir = to - from;
            float length = dir.magnitude;
            if (length < 0.01f) return;
            dir /= length;

            float dashLength = dashSpacing * 0.5f;
            float start = Mathf.Repeat(Time.time * flowSpeed, dashSpacing) - dashSpacing;
            for (float d = start; d < length; d += dashSpacing)
            {
                float a = Mathf.Max(d, 0f), b = Mathf.Min(d + dashLength, length);
                if (b > a) Line(from + dir * a, from + dir * b, width, color);
            }
        }

        // ------------------------------------------------------------------ text

        // Score above every candidate inside the cone. The labels are reused frame after frame.
        void UpdateScoreLabels(PassResult r)
        {
            int used = 0;
            Camera cam = LabelCamera;

            if (showScores && r != null && cam != null)
            {
                foreach (Candidate c in r.Candidates)
                {
                    if (!c.InsideCone) continue;

                    Vector3 screen = cam.WorldToScreenPoint(World(c.Player.Position) + Vector3.up * 2.6f);
                    if (screen.z < 0f || !cam.pixelRect.Contains(screen)) continue; // behind or in the other half

                    bool isChosen = r.Chosen == c && showChosen;
                    Text label = ScoreLabel(used++);
                    label.text = c.Score.ToString("0.00");
                    label.color = isChosen ? chosenColor : candidateColor;
                    label.fontSize = fontSize;
                    // The canvas covers only this camera's viewport: positions relative to its corner.
                    label.rectTransform.anchoredPosition = ((Vector2)screen - cam.pixelRect.min) / labelCanvas.scaleFactor;
                }
            }

            for (int i = used; i < labels.Count; i++) labels[i].enabled = false;
        }

        Text ScoreLabel(int index)
        {
            if (index == labels.Count)
            {
                Text label = Ui.Label("Score", labelCanvas.transform, fontSize, Color.white, TextAnchor.MiddleCenter);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = Vector2.zero; // screen pixels
                label.rectTransform.sizeDelta = new Vector2(120f, 40f);
                labels.Add(label);
            }
            labels[index].enabled = true;
            return labels[index];
        }

        // ------------------------------------------------------------------ 2D pitch → 3D world

        // Same convention as the domain: pitch X = Unity X, pitch Y = Unity Z.
        Vector3 World(PitchVector p) => new Vector3(p.X, heightAboveGrass, p.Y);
        static Vector3 Direction(PassResult r) => new Vector3(r.Direction.X, 0f, r.Direction.Y);

        // ------------------------------------------------------------------ primitives

        static Color Faded(Color c, float alpha) => new Color(c.r, c.g, c.b, c.a * alpha);

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color) =>
            Quad(a, b, c, d, color, color, color, color);

        // One color per vertex, for gradients.
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color ca, Color cb, Color cc, Color cd)
        {
            int i = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            colors.Add(ca); colors.Add(cb); colors.Add(cc); colors.Add(cd);
            triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
            triangles.Add(i); triangles.Add(i + 2); triangles.Add(i + 3);
        }

        void Line(Vector3 from, Vector3 to, float width, Color color) =>
            GradientLine(from, to, width, color, color);

        void GradientLine(Vector3 from, Vector3 to, float width, Color fromColor, Color toColor)
        {
            Vector3 dir = to - from;
            if (dir.sqrMagnitude < 0.0001f) return;
            Vector3 side = Vector3.Cross(Vector3.up, dir.normalized) * (width * 0.5f);
            Quad(from - side, from + side, to + side, to - side, fromColor, fromColor, toColor, toColor);
        }

        void Arrow(Vector3 from, Vector3 to, float width, Color color)
        {
            Vector3 dir = to - from;
            float length = dir.magnitude;
            if (length < 0.01f) return;
            dir /= length;

            float headLength = Mathf.Min(width * 6f, length * 0.5f);
            Vector3 headBase = to - dir * headLength;
            Line(from, headBase, width, color);

            Vector3 side = Vector3.Cross(Vector3.up, dir) * (width * 2.5f);
            int i = vertices.Count;
            vertices.Add(headBase - side); vertices.Add(headBase + side); vertices.Add(to);
            colors.Add(color); colors.Add(color); colors.Add(color);
            triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
        }

        void Ring(Vector3 center, float radius, float width, Color color, int segments = 40)
        {
            float rIn = radius - width * 0.5f, rOut = radius + width * 0.5f;
            for (int s = 0; s < segments; s++)
            {
                float a0 = s * Mathf.PI * 2f / segments, a1 = (s + 1) * Mathf.PI * 2f / segments;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Quad(center + d0 * rIn, center + d0 * rOut, center + d1 * rOut, center + d1 * rIn, color);
            }
        }

        void Cross(Vector3 center, float size, float width, Color color)
        {
            Vector3 a = new Vector3(size, 0f, size), b = new Vector3(size, 0f, -size);
            Line(center - a, center + a, width, color);
            Line(center - b, center + b, width, color);
        }

        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }
    }
}
