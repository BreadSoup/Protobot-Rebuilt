using UnityEngine;
using UnityEngine.Rendering;

namespace Protobot {
    /// <summary>
    /// Creates translucent world-space planes that preview mirror axes.
    ///
    ///   ShowFlipPlane(bounds, axis)
    ///     One plane through the bounding-box CENTRE (flip in-place preview).
    ///
    ///   ShowMirrorPlane(bounds, axis)
    ///     One plane at the bounding-box EDGE + gap (mirror-duplicate, +axis side).
    ///
    ///   ShowMirrorPlaneAt(worldPos, bounds, axis)
    ///     Same as ShowMirrorPlane but caller supplies the exact world position.
    ///     Used when the sign of the offset is chosen by the user (±axis).
    ///
    ///   HideAll() — hides all planes.
    ///
    /// Double-sided fix: each "plane" is actually two back-to-back quads so both
    /// faces are always visible, regardless of shader culling behaviour.
    /// </summary>
    public class AxisPreviewPlanes : MonoBehaviour {

        // ── Config ────────────────────────────────────────────────────────────
        private const float EdgePadding  = 0.5f;  // gap between object edge and mirror plane
        private const float PlanePadding = 2.0f;  // extra size around bounding box

        private const float AlphaNormal    = 0.22f;
        private const float AlphaHighlight = 0.50f;

        // Axis colours (X=red, Y=green, Z=blue — match Protobot's ColorX/Y/Z)
        private static readonly Color[] BaseCol = {
            new Color(0.75f, 0.22f, 0.22f, AlphaNormal),
            new Color(0.20f, 0.65f, 0.20f, AlphaNormal),
            new Color(0.20f, 0.40f, 0.80f, AlphaNormal),
        };
        private static readonly Color[] HighCol = {
            new Color(0.75f, 0.22f, 0.22f, AlphaHighlight),
            new Color(0.20f, 0.65f, 0.20f, AlphaHighlight),
            new Color(0.20f, 0.40f, 0.80f, AlphaHighlight),
        };

        // ── Internal ──────────────────────────────────────────────────────────
        // _planes[i] is a parent GO whose children are the front and back quads.
        private readonly GameObject[] _planes   = new GameObject[3];
        private readonly Material[]   _mats     = new Material[3];
        // Both front and back quads share the same material instance per axis.

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake() {
            string[] axisNames = { "X", "Y", "Z" };
            for (int i = 0; i < 3; i++) {
                _mats[i]   = MakeTransparentMat(BaseCol[i]);
                _planes[i] = CreatePlanePair($"MirrorPlane_{axisNames[i]}", _mats[i]);
                _planes[i].SetActive(false);
            }
        }

        private void OnDestroy() {
            for (int i = 0; i < 3; i++)
                if (_mats[i] != null) Destroy(_mats[i]);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Shows a plane at the bounding-box CENTRE for the given axis.
        /// Used for "Flip" (mirror in-place) preview.
        /// </summary>
        public void ShowFlipPlane(Bounds bounds, int axis) {
            HideAll();
            PositionPlane(axis, bounds.center, bounds);
            _planes[axis].SetActive(true);
            _mats[axis].color = HighCol[axis];
        }

        /// <summary>
        /// Shows a plane at the bounding-box EDGE (+ EdgePadding/2) in the +axis direction.
        /// Used for "Mirror Duplicate" (+side) preview.
        /// </summary>
        public void ShowMirrorPlane(Bounds bounds, int axis) {
            Vector3 n       = AxisDir(axis);
            float   halfExt = Mathf.Abs(Vector3.Dot(bounds.extents, n));
            ShowMirrorPlaneAt(bounds.center + n * (halfExt + EdgePadding * 0.5f), bounds, axis);
        }

        /// <summary>
        /// Shows a plane at <paramref name="worldPos"/> for the given axis.
        /// Use this when the caller controls which side the plane appears on.
        /// </summary>
        public void ShowMirrorPlaneAt(Vector3 worldPos, Bounds bounds, int axis) {
            HideAll();
            PositionPlane(axis, worldPos, bounds);
            _planes[axis].SetActive(true);
            _mats[axis].color = HighCol[axis];
        }

        /// <summary>Hides all three planes.</summary>
        public void HideAll() {
            for (int i = 0; i < 3; i++)
                _planes[i].SetActive(false);
        }

        // ── Plane creation ────────────────────────────────────────────────────

        /// <summary>
        /// Creates a parent GO with a front and a back quad child, sharing one material.
        /// The back quad is rotated 180° around Y so it covers the reverse face.
        /// This guarantees visibility from both sides without relying on shader culling.
        /// </summary>
        private GameObject CreatePlanePair(string name, Material mat) {
            var parent = new GameObject(name);
            parent.transform.SetParent(transform);

            CreateQuad("Front", parent.transform, Quaternion.identity,          mat);
            CreateQuad("Back",  parent.transform, Quaternion.Euler(0f, 180f, 0f), mat);
            return parent;
        }

        private static void CreateQuad(string name, Transform parent,
                                       Quaternion localRot, Material mat) {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localRotation = localRot;
            Destroy(go.GetComponent<MeshCollider>());
            go.GetComponent<Renderer>().material = mat;
        }

        // ── Plane positioning ─────────────────────────────────────────────────
        /// <summary>
        /// Positions and scales the plane parent GO for the given axis.
        /// Rotation notes (Unity Quad faces +Z by default):
        ///   X-plane (normal = right):   Euler(0, 90, 0)   — spans Y×Z
        ///   Y-plane (normal = up):      Euler(-90, 0, 0)  — spans X×Z
        ///   Z-plane (normal = forward): Euler(0, 0, 0)    — spans X×Y
        /// </summary>
        private void PositionPlane(int axis, Vector3 worldPos, Bounds bounds) {
            Vector3 s  = bounds.size;
            float   sx = s.x + PlanePadding;
            float   sy = s.y + PlanePadding;
            float   sz = s.z + PlanePadding;

            var t = _planes[axis].transform;
            t.position = worldPos;

            switch (axis) {
                case 0:
                    t.rotation   = Quaternion.Euler(0f, 90f, 0f);
                    t.localScale = new Vector3(sz, sy, 1f);
                    break;
                case 1:
                    t.rotation   = Quaternion.Euler(-90f, 0f, 0f);
                    t.localScale = new Vector3(sx, sz, 1f);
                    break;
                default:
                    t.rotation   = Quaternion.identity;
                    t.localScale = new Vector3(sx, sy, 1f);
                    break;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static Vector3 AxisDir(int axis) =>
            axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;

        /// <summary>
        /// Transparent Standard-shader material.
        /// Note: CullMode.Off is still set here as a secondary measure, but the
        /// two-quad approach above is the primary double-sided fix.
        /// </summary>
        private static Material MakeTransparentMat(Color color) {
            var mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend",  (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",  (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",    0);
            mat.SetInt("_Cull",      (int)CullMode.Off);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.color = color;
            return mat;
        }
    }
}
