using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AssistedPass.Presentation.Ambience
{
    /// <summary>
    /// Stands and crowd around the pitch, generated in code.
    /// Pure decoration: it reads nothing from the pass system and can be removed without breaking anything.
    ///
    /// It does not create a GameObject per spectator: it stores one matrix per instance and draws
    /// everything in batches with GPU instancing (Graphics.RenderMeshInstanced). That way thousands
    /// of people cost almost the same as one.
    /// </summary>
    [ExecuteAlways] // also visible in the editor, to frame the camera
    public class Stadium : MonoBehaviour
    {
        [Header("Pitch (m)")]
        [SerializeField] float pitchLength = 105f;
        [SerializeField] float pitchWidth = 68f;
        [Tooltip("Distance from the touchline/goal line to the first row.")]
        [SerializeField] float margin = 8f;

        [Header("Stands")]
        [SerializeField, Range(1, 40)] int rows = 14;
        [SerializeField] float rowDepth = 0.9f;
        [SerializeField] float rowHeight = 0.45f;
        [SerializeField] Color standColor = new Color(0.32f, 0.33f, 0.36f);
        [SerializeField] bool standsCastShadows = false;

        [Header("Crowd")]
        [Tooltip("Spacing between seats (m). Bigger = fewer people and better performance.")]
        [SerializeField, Range(0.5f, 3f)] float seatSpacing = 0.75f;
        [SerializeField, Range(0f, 1f)] float occupancy = 0.85f;
        [Tooltip("Scale of each spectator (a capsule 1 m wide and 2 m tall).")]
        [SerializeField] Vector3 personSize = new Vector3(0.45f, 0.55f, 0.45f);
        [Tooltip("Shirt colors. Muted, so they don't steal the spotlight from the cone.")]
        [SerializeField] Color[] palette =
        {
            new Color(0.55f, 0.18f, 0.20f), new Color(0.20f, 0.30f, 0.55f), new Color(0.75f, 0.72f, 0.65f),
            new Color(0.25f, 0.25f, 0.27f), new Color(0.60f, 0.50f, 0.25f), new Color(0.30f, 0.45f, 0.35f),
        };
        [SerializeField] int seed = 7;

        [Header("Animation")]
        [Tooltip("Height of the crowd's sway (m). 0 = still.")]
        [SerializeField, Range(0f, 0.5f)] float swayAmplitude = 0.06f;
        [SerializeField] float swaySpeed = 2f;

        [Header("Optional")]
        [Tooltip("URP Lit material with GPU instancing. If empty, one is created.")]
        [SerializeField] Material baseMaterial;

        const int MaxPerCall = 1023; // safe instance limit per call

        static Mesh cubeMesh, personMesh;
        Material standMaterial;
        Material[] crowdMaterials;
        bool dirty = true;

        readonly List<Matrix4x4> steps = new List<Matrix4x4>();
        // Per color: base position of each spectator and its animation phase.
        List<Vector3>[] positions;
        List<float>[] phases;
        readonly Matrix4x4[] batch = new Matrix4x4[MaxPerCall];

        void OnEnable()
        {
            dirty = true;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            Release();
        }

        // When a value changes in the Inspector, it is regenerated on the next frame.
        void OnValidate() => dirty = true;

        // ------------------------------------------------------------------ generation

        void Generate()
        {
            Release();
            dirty = false;
            if (palette == null || palette.Length == 0) return;

            if (cubeMesh == null) cubeMesh = CreateCube();
            if (personMesh == null) personMesh = CreateCapsule(segments: 8, rings: 3);

            standMaterial = CreateMaterial(standColor);
            crowdMaterials = new Material[palette.Length];
            positions = new List<Vector3>[palette.Length];
            phases = new List<float>[palette.Length];
            for (int i = 0; i < palette.Length; i++)
            {
                crowdMaterials[i] = CreateMaterial(palette[i]);
                positions[i] = new List<Vector3>();
                phases[i] = new List<float>();
            }

            // The side stands are longer and cover the corners;
            // the end stands fit between them.
            float standDepth = rows * rowDepth;
            float halfLength = pitchLength * 0.5f + margin, halfWidth = pitchWidth * 0.5f + margin;
            float sideLength = 2f * (halfLength + standDepth);
            float endLength = 2f * halfWidth;

            // The stadium is centered on this object (place it at the center of the pitch).
            Vector3 center = transform.position;
            var random = new System.Random(seed);
            Stand(center + new Vector3(0f, 0f,  halfWidth), Vector3.forward, sideLength, random);
            Stand(center + new Vector3(0f, 0f, -halfWidth), Vector3.back,    sideLength, random);
            Stand(center + new Vector3( halfLength, 0f, 0f), Vector3.right,  endLength,  random);
            Stand(center + new Vector3(-halfLength, 0f, 0f), Vector3.left,   endLength,  random);
        }

        /// <summary>One stand: steps rising away from the pitch, with seated people.</summary>
        void Stand(Vector3 edge, Vector3 outwards, float length, System.Random random)
        {
            Quaternion rotation = Quaternion.LookRotation(outwards); // local z = outwards, local x = along
            Vector3 along = rotation * Vector3.right;
            int seats = Mathf.FloorToInt(length / seatSpacing);

            for (int r = 0; r < rows; r++)
            {
                float height = (r + 1) * rowHeight;
                Vector3 rowCenter = edge + outwards * ((r + 0.5f) * rowDepth);

                // Solid step from the ground up.
                steps.Add(Matrix4x4.TRS(rowCenter + Vector3.up * (height * 0.5f), rotation,
                                        new Vector3(length, height, rowDepth)));

                // People sitting on top, with gaps and a bit of disorder.
                for (int s = 0; s < seats; s++)
                {
                    if (random.NextDouble() > occupancy) continue;
                    float x = -length * 0.5f + (s + 0.5f) * seatSpacing;
                    x += ((float)random.NextDouble() - 0.5f) * seatSpacing * 0.3f;
                    Vector3 p = rowCenter + along * x + Vector3.up * (height + personSize.y);

                    int color = random.Next(palette.Length);
                    positions[color].Add(p);
                    phases[color].Add((float)random.NextDouble() * Mathf.PI * 2f);
                }
            }
        }

        // ------------------------------------------------------------------ drawing

        // Drawn right before each camera renders: works the same in Play mode
        // and in the editor's Scene view.
        void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView) return;
            if (dirty) Generate();
            if (standMaterial == null) return;

            var standParams = Params(standMaterial, cam, standsCastShadows);
            for (int start = 0; start < steps.Count; start += MaxPerCall)
                Graphics.RenderMeshInstanced(standParams, cubeMesh, 0, steps,
                                             Mathf.Min(MaxPerCall, steps.Count - start), start);

            float t = UnityEngine.Application.isPlaying ? Time.time : 0f;
            for (int c = 0; c < crowdMaterials.Length; c++)
            {
                var rp = Params(crowdMaterials[c], cam, false);
                List<Vector3> pos = positions[c];
                for (int start = 0; start < pos.Count; start += MaxPerCall)
                {
                    int n = Mathf.Min(MaxPerCall, pos.Count - start);
                    for (int i = 0; i < n; i++)
                    {
                        // Gentle sway: each one at its own pace so the stand "breathes".
                        float bounce = swayAmplitude * Mathf.Abs(Mathf.Sin(t * swaySpeed + phases[c][start + i]));
                        batch[i] = Matrix4x4.TRS(pos[start + i] + Vector3.up * bounce, Quaternion.identity, personSize);
                    }
                    Graphics.RenderMeshInstanced(rp, personMesh, 0, batch, n);
                }
            }
        }

        static RenderParams Params(Material material, Camera cam, bool shadows) => new RenderParams(material)
        {
            camera = cam,
            shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
            receiveShadows = true,
        };

        Material CreateMaterial(Color color)
        {
            Material m = baseMaterial != null
                ? new Material(baseMaterial)
                : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.hideFlags = HideFlags.HideAndDontSave;
            m.enableInstancing = true;
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.1f);
            return m;
        }

        void Release()
        {
            steps.Clear();
            if (standMaterial != null) DestroyImmediate(standMaterial);
            if (crowdMaterials != null)
                foreach (Material m in crowdMaterials)
                    if (m != null) DestroyImmediate(m);
            standMaterial = null;
            crowdMaterials = null;
        }

        // ------------------------------------------------------------------ meshes

        // 1 m cube centered at the origin. Normal = Cross(v, u) so every face points outwards.
        static Mesh CreateCube()
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            Vector3[] faces = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (Vector3 normal in faces)
            {
                Vector3 axis2 = Mathf.Abs(normal.y) > 0.5f ? Vector3.forward : Vector3.up;
                Vector3 axis1 = Vector3.Cross(normal, axis2);
                Vector3 c = normal * 0.5f;
                int i = v.Count;
                v.Add(c - (axis1 + axis2) * 0.5f); v.Add(c + (-axis1 + axis2) * 0.5f);
                v.Add(c + (axis1 + axis2) * 0.5f); v.Add(c + (axis1 - axis2) * 0.5f);
                for (int k = 0; k < 4; k++) n.Add(normal);
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
                t.Add(i); t.Add(i + 2); t.Add(i + 3);
            }
            var mesh = new Mesh { name = "Stadium_Cube", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetTriangles(t, 0);
            return mesh;
        }

        // Low-poly capsule (1 m wide, 2 m tall, like Unity's):
        // a profile of two hemispheres and a cylinder, revolved around the Y axis.
        static Mesh CreateCapsule(int segments, int rings)
        {
            var profile = new List<(float phi, float centerY)>();
            for (int k = 0; k <= rings; k++) profile.Add((-90f + 90f * k / rings, -0.5f));
            for (int k = 0; k <= rings; k++) profile.Add((90f * k / rings, 0.5f));

            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            foreach ((float phi, float centerY) in profile)
            {
                float rad = phi * Mathf.Deg2Rad;
                for (int s = 0; s < segments; s++)
                {
                    float theta = s * Mathf.PI * 2f / segments;
                    var normal = new Vector3(Mathf.Cos(rad) * Mathf.Cos(theta), Mathf.Sin(rad), Mathf.Cos(rad) * Mathf.Sin(theta));
                    v.Add(normal * 0.5f + Vector3.up * centerY);
                    n.Add(normal);
                }
            }
            for (int a = 0; a < profile.Count - 1; a++)
                for (int s = 0; s < segments; s++)
                {
                    int below = a * segments, above = (a + 1) * segments, s1 = (s + 1) % segments;
                    t.Add(below + s); t.Add(above + s); t.Add(above + s1);
                    t.Add(below + s); t.Add(above + s1); t.Add(below + s1);
                }
            var mesh = new Mesh { name = "Stadium_Person", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetTriangles(t, 0);
            return mesh;
        }
    }
}
