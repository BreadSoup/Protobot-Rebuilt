using UnityEngine;
using UnityEngine.Rendering;

namespace Protobot {
    /// <summary>
    /// Creates translucent world-space quad planes that preview mirror axes.
    ///
    /// Two display modes:
    ///
    ///   ShowFlipPlane(bounds, axis)
    ///     One plane through the bounding-box CENTRE — previews where a flip-in-place
    ///     will reflect the selection.
    ///
    ///   ShowMirrorPlane(bounds, axis)
    ///     One plane at the bounding-box EDGE + padding — previews where a mirror-
    ///     duplicate will be placed so the copy sits just outside the original.
    ///
    ///   HideAll() — hides all planes.
    ///
    /// Bug fix: materials now use CullMode.Off so planes are visible from both sides.
    /// </summary>
    public class AxisPreviewPlanes : MonoBehaviour {

        // ── Config ────────────────────────────────────────────────────────────
        /// <summary>Extra world-units added to each edge of the bounding box.</summary>
        private const float EdgePadding   = 0.5f;   // gap between object and mirror plane (Unity units)
        private const float PlanePadding  = 2.0f;   // extra size added to the quad so it extends beyond the object

        private const float AlphaNormal   = 0.22f;
        private const float AlphaHighlight= 0.50f;

        // Axis colour convention (X=red, Y=green, Z=blue — matches Protobot's ColorX/Y/Z)
        private static readonly Color[] BaseCol = {
            new Color(0.75f, 0.22f, 0.22f, AlphaNormal),  // X — red
            new Color(0.20f, 0.65f, 0.20f, AlphaNormal),  // Y — green
            new Color(0.20f, 0.40f, 0.80f, AlphaNormal),  // Z — blue
        };
        private static readonly Color[] HighCol = {
            new Color(0.75f, 0.22f, 0.22f, AlphaHighlight),
            new Color(0.20f, 0.65f, 0.20f, AlphaHighlight),
            new Color(0.20f, 0.40f, 0.80f, AlphaHighlight),
        };

        // ── Internal ──────────────────────────────────────────────────────────
        private readonly GameObject[] _quads = new GameObject[3];
        private readonly Material[]   _mats  = new Material[3];

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake() {
            for (int i = 0; i < 3; i++) {
                _quads[i] = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _quads[i].name = i == 0 ? "MirrorPlane_X"
                               : i == 1 ? "MirrorPlane_Y" : "MirrorPlane_Z";
                _quads[i].transform.SetParent(transform);
                Destroy(_quads[i].GetComponent<MeshCollider>());

                _mats[i] = MakeTransparentMat(BaseCol[i]);
                _quads[i].GetComponent<Renderer>().material = _mats[i];
                _quads[i].SetActive(false);
            }
        }

        private void OnDestroy() {
            for (int i = 0; i < 3; i++)
                if (_mats[i] != null) Destroy(_mats[i]);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Shows one plane through the bounding-box CENTRE for the given axis.
        /// Used for "Flip" (mirror in-place) preview.
        /// </summary>
        public void ShowFlipPlane(Bounds bounds, int axis) {
            HideAll();
            PositionPlane(axis, bounds.center, bounds);
            _quads[axis].SetActive(true);
            _mats[axis].color = HighCol[axis];
        }

        /// <summary>
        /// Shows one plane at the bounding-box EDGE (+ EdgePadding) for the given axis.
        /// Used for "Mirror Duplicate" preview — the plane is where the copy will be
        /// placed, which is just outside the original object.
        /// </summary>
        public void ShowMirrorPlane(Bounds bounds, int axis) {
            HideAll();
            Vector3 axisDir = AxisDir(axis);
            float   halfExt = Mathf.Abs(Vector3.Dot(bounds.extents, axisDir));
            // Place the plane at the edge of the bounding box + half of EdgePadding
            // (the copy's inner edge ends up EdgePadding away from the original)
            Vector3 planePos = bounds.center + axisDir * (halfExt + EdgePadding * 0.5f);
            PositionPlane(axis, planePos, bounds);
            _quads[axis].SetActive(true);
            _mats[axis].color = HighCol[axis];
        }

        /// <summary>Hides all three planes.</summary>
        public void HideAll() {
            for (int i = 0; i < 3; i++)
                _quads[i].SetActive(false);
        }

        // ── Plane positioning ─────────────────────────────────────────────────
        /// <summary>
        /// Positions and sizes a single quad plane.
        ///
        /// Rotation notes (Unity Quad faces +Z by default):
        ///   X-plane (YZ plane, normal = right):   Euler(0, 90, 0)  → local X = world -Z, local Y = world Y
        ///   Y-plane (XZ plane, normal = up):      Euler(-90, 0, 0) → local X = world X,  local Y = world Z
        ///   Z-plane (XY plane, normal = forward): Euler(0, 0, 0)   → local X = world X,  local Y = world Y
        /// </summary>
        private void PositionPlane(int axis, Vector3 worldPos, Bounds bounds) {
            Vector3 s = bounds.size;
            float sx = s.x + PlanePadding;
            float sy = s.y + PlanePadding;
            float sz = s.z + PlanePadding;

            var t = _quads[axis].transform;
            t.position = worldPos;

            switch (axis) {
                case 0:  // X — normal = right, quad faces right
                    t.rotation   = Quaternion.Euler(0f, 90f, 0f);
                    t.localScale = new Vector3(sz, sy, 1f);
                    break;
                case 1:  // Y — normal = up, quad faces up
                    t.rotation   = Quaternion.Euler(-90f, 0f, 0f);
                    t.localScale = new Vector3(sx, sz, 1f);
                    break;
                default: // Z — normal = forward, default quad orientation
                    t.rotation   = Quaternion.identity;
                    t.localScale = new Vector3(sx, sy, 1f);
                    break;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static Vector3 AxisDir(int axis) =>
            axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;

        /// <summary>
        /// Creates a Standard-shader material in Transparent mode.
        /// CullMode.Off makes both faces visible (fixes the one-sided plane bug).
        /// </summary>
        private static Material MakeTransparentMat(Color color) {
            var mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend",  (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",  (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",    0);
            // ← Fix for single-sided bug: disable backface culling
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
