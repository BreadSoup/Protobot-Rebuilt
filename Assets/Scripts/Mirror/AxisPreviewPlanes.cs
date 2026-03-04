using UnityEngine;
using UnityEngine.Rendering;

namespace Protobot {
    /// <summary>
    /// Creates three world-space translucent quad planes that visualise the
    /// X, Y, and Z mirror axes around the current selection's bounding box.
    ///
    /// Colour convention (matching Unity's gizmo colours):
    ///   X plane  →  red    (the YZ plane, normal = Vector3.right)
    ///   Y plane  →  green  (the XZ plane, normal = Vector3.up)
    ///   Z plane  →  blue   (the XY plane, normal = Vector3.forward)
    ///
    /// Usage:
    ///   ShowAll(bounds)       — show all three planes sized to the given bounds
    ///   HighlightAxis(0..2)   — brighten one plane, dim the others
    ///   HideAll()             — hide all three planes
    /// </summary>
    public class AxisPreviewPlanes : MonoBehaviour {

        // ── Configuration ──────────────────────────────────────────────────────

        /// <summary>Extra world-units added to each edge of the bounding box.</summary>
        private const float Padding = 2f;

        // Dim alpha when all three are shown together
        private const float AlphaNormal = 0.18f;
        // Bright alpha when an axis is focused in the sub-menu
        private const float AlphaHighlight = 0.45f;

        private static readonly Color[] BaseColors = {
            new Color(1.0f, 0.20f, 0.20f, AlphaNormal), // X — red
            new Color(0.2f, 1.00f, 0.30f, AlphaNormal), // Y — green
            new Color(0.2f, 0.45f, 1.00f, AlphaNormal), // Z — blue
        };

        private static readonly Color[] HighlightColors = {
            new Color(1.0f, 0.20f, 0.20f, AlphaHighlight),
            new Color(0.2f, 1.00f, 0.30f, AlphaHighlight),
            new Color(0.2f, 0.45f, 1.00f, AlphaHighlight),
        };

        // ── Internal state ────────────────────────────────────────────────────

        private readonly GameObject[] _quads = new GameObject[3];
        private readonly Material[]   _mats  = new Material[3];

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake() {
            for (int i = 0; i < 3; i++) {
                _quads[i] = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _quads[i].name = i == 0 ? "MirrorPlane_X"
                               : i == 1 ? "MirrorPlane_Y"
                                        : "MirrorPlane_Z";

                // Parent to this GameObject so it moves with it (though we keep it
                // at origin — planes are repositioned each ShowAll call)
                _quads[i].transform.SetParent(transform);

                // Remove the auto-created collider so it doesn't intercept clicks
                Destroy(_quads[i].GetComponent<MeshCollider>());

                _mats[i] = MakeTransparentMaterial(BaseColors[i]);
                _quads[i].GetComponent<Renderer>().material = _mats[i];
                _quads[i].SetActive(false);
            }
        }

        private void OnDestroy() {
            // Clean up runtime materials to avoid memory leaks
            for (int i = 0; i < 3; i++)
                if (_mats[i] != null) Destroy(_mats[i]);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Shows all three planes, sized and centred on <paramref name="bounds"/>.
        /// All planes use their normal (dim) alpha.
        /// </summary>
        public void ShowAll(Bounds bounds) {
            UpdatePlanes(bounds);
            for (int i = 0; i < 3; i++) {
                _quads[i].SetActive(true);
                _mats[i].color = BaseColors[i];
            }
        }

        /// <summary>
        /// Highlights the plane for <paramref name="axis"/> (0=X, 1=Y, 2=Z)
        /// while dimming the others.  Call <see cref="ShowAll"/> first to
        /// ensure the planes are positioned correctly.
        /// </summary>
        public void HighlightAxis(int axis) {
            for (int i = 0; i < 3; i++) {
                _quads[i].SetActive(true);
                _mats[i].color = (i == axis) ? HighlightColors[i] : BaseColors[i];
            }
        }

        /// <summary>Hides all three planes.</summary>
        public void HideAll() {
            for (int i = 0; i < 3; i++)
                _quads[i].SetActive(false);
        }

        // ── Plane Positioning ──────────────────────────────────────────────────

        /// <summary>
        /// Repositions and rescales each quad to cover the given bounds + padding.
        ///
        /// Unity Quad primitives are 1×1 in local XY and face their local +Z.
        ///
        ///   Plane 0 (X, normal = right):
        ///     Rotate 90° around Y so local +Z → world +X.
        ///     After this rotation: local X = world -Z, local Y = world Y.
        ///     Scale: x = sz, y = sy.
        ///
        ///   Plane 1 (Y, normal = up):
        ///     Rotate 90° around X so local +Z → world -Y, but we want +Y,
        ///     so rotate –90° (or use Euler(-90,0,0) → local +Z → world +Y).
        ///     After this rotation: local X = world X, local Y = world Z.
        ///     Scale: x = sx, y = sz.
        ///
        ///   Plane 2 (Z, normal = forward):
        ///     Default orientation, local +Z = world +Z.
        ///     Scale: x = sx, y = sy.
        /// </summary>
        private void UpdatePlanes(Bounds bounds) {
            Vector3 c  = bounds.center;
            float   sx = bounds.size.x + Padding;
            float   sy = bounds.size.y + Padding;
            float   sz = bounds.size.z + Padding;

            // X mirror plane (the YZ plane)
            _quads[0].transform.position   = c;
            _quads[0].transform.rotation   = Quaternion.Euler(0f, 90f, 0f);
            _quads[0].transform.localScale = new Vector3(sz, sy, 1f);

            // Y mirror plane (the XZ plane)
            _quads[1].transform.position   = c;
            _quads[1].transform.rotation   = Quaternion.Euler(-90f, 0f, 0f);
            _quads[1].transform.localScale = new Vector3(sx, sz, 1f);

            // Z mirror plane (the XY plane)
            _quads[2].transform.position   = c;
            _quads[2].transform.rotation   = Quaternion.identity;
            _quads[2].transform.localScale = new Vector3(sx, sy, 1f);
        }

        // ── Material Factory ───────────────────────────────────────────────────

        /// <summary>
        /// Creates a Standard-shader material configured for alpha-blended
        /// transparency.  This is the correct approach for runtime material
        /// creation in Unity's built-in render pipeline.
        /// </summary>
        private Material MakeTransparentMaterial(Color color) {
            var mat = new Material(Shader.Find("Standard"));

            // Set the Standard shader to Transparent rendering mode
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend",  (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",  (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",    0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.color = color;

            return mat;
        }
    }
}
