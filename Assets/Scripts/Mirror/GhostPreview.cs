using System.Collections.Generic;
using UnityEngine;

namespace Protobot {
    /// <summary>
    /// Shows translucent blue "ghost" copies of selected objects at their would-be
    /// mirrored positions, so the user can preview the result before confirming.
    ///
    /// Usage:
    ///   ShowGhosts(sources, mirrorCenter, mirrorNormal) — create/update ghost copies
    ///   HideGhosts()                                    — destroy all ghosts
    /// </summary>
    public class GhostPreview : MonoBehaviour {

        // Blue translucent material, shared across all ghost instances
        private static Material _ghostMat;

        private readonly List<GameObject> _ghosts = new List<GameObject>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Destroys any existing ghosts, then creates new ones for each source object
        /// at the position/rotation it would have after being mirrored about the given plane.
        /// </summary>
        public void ShowGhosts(List<GameObject> sources, Vector3 mirrorCenter,
                               Vector3 mirrorNormal) {
            HideGhosts();

            if (sources == null) return;

            var mat = GetGhostMat();

            foreach (var src in sources) {
                if (src == null) continue;

                Vector3    ghostPos = MirrorSystem.MirrorPosition(
                                          src.transform.position, mirrorCenter, mirrorNormal);
                Quaternion ghostRot = MirrorSystem.MirrorRotation(
                                          src.transform.rotation, mirrorNormal);

                var ghost = Instantiate(src, ghostPos, ghostRot);
                ghost.name = $"[Ghost] {src.name}";

                // Remove all non-visual components so the ghost is purely decorative
                StripNonVisual(ghost);

                // Apply blue transparent material to every Renderer in the ghost
                foreach (var r in ghost.GetComponentsInChildren<Renderer>()) {
                    // Assign a new material instance per renderer so colour changes
                    // don't bleed across different ghosts
                    r.material = mat;
                }

                _ghosts.Add(ghost);
            }
        }

        /// <summary>Destroys all active ghost GameObjects.</summary>
        public void HideGhosts() {
            foreach (var g in _ghosts)
                if (g != null) Destroy(g);
            _ghosts.Clear();
        }

        private void OnDestroy() => HideGhosts();

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Removes Colliders, Rigidbodies, and other non-Renderer components from
        /// the ghost so it has no physics presence and causes no interference.
        /// </summary>
        private static void StripNonVisual(GameObject ghost) {
            // Process all children + root
            foreach (var col in ghost.GetComponentsInChildren<Collider>())
                Destroy(col);
            foreach (var rb in ghost.GetComponentsInChildren<Rigidbody>())
                Destroy(rb);

            // Remove MonoBehaviours that could fire logic (but keep Transform/Renderer)
            foreach (var mb in ghost.GetComponentsInChildren<MonoBehaviour>())
                Destroy(mb);
        }

        /// <summary>
        /// Returns the shared blue transparent ghost material, creating it on first call.
        /// </summary>
        private static Material GetGhostMat() {
            if (_ghostMat != null) return _ghostMat;

            _ghostMat = new Material(Shader.Find("Standard"));
            _ghostMat.SetFloat("_Mode",      3f);
            _ghostMat.SetInt("_SrcBlend",    (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _ghostMat.SetInt("_DstBlend",    (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _ghostMat.SetInt("_ZWrite",      0);
            _ghostMat.SetFloat("_Glossiness", 0f);
            _ghostMat.SetFloat("_Metallic",   0f);
            _ghostMat.DisableKeyword("_ALPHATEST_ON");
            _ghostMat.EnableKeyword("_ALPHABLEND_ON");
            _ghostMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            _ghostMat.renderQueue = 3000;
            _ghostMat.color = new Color(0.20f, 0.50f, 1.00f, 0.40f);
            return _ghostMat;
        }
    }
}
