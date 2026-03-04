using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Protobot.StateSystems;
using Protobot.Outlining;

namespace Protobot {
    /// <summary>
    /// Static helper class for mirror and mirror-duplicate operations.
    ///
    /// All mirror planes pass through the combined bounding-box centre of the
    /// selected objects.  Three axes are supported:
    ///   mirrorNormal = Vector3.right    → reflects X  (flips left ↔ right)
    ///   mirrorNormal = Vector3.up       → reflects Y  (flips top  ↔ bottom)
    ///   mirrorNormal = Vector3.forward  → reflects Z  (flips front ↔ back)
    /// </summary>
    public static class MirrorSystem {

        // ── Bounding Box ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns the combined world-space bounds of all Renderers found on (or
        /// inside) each object in <paramref name="objs"/>.
        /// </summary>
        public static Bounds GetBounds(List<GameObject> objs) {
            var  bounds = new Bounds();
            bool first  = true;

            foreach (var obj in objs) {
                if (obj == null) continue;
                foreach (var r in obj.GetComponentsInChildren<Renderer>()) {
                    if (first) { bounds = r.bounds; first = false; }
                    else         bounds.Encapsulate(r.bounds);
                }
            }
            return bounds;
        }

        // ── Mirror Math ───────────────────────────────────────────────────────

        /// <summary>
        /// Reflects <paramref name="worldPos"/> about the plane that passes through
        /// <paramref name="center"/> and has the given <paramref name="mirrorNormal"/>.
        /// </summary>
        public static Vector3 MirrorPosition(Vector3 worldPos, Vector3 center, Vector3 mirrorNormal) {
            Vector3 relative = worldPos - center;
            // Vector3.Reflect(v, n) = v - 2 * Dot(v, n) * n
            return center + Vector3.Reflect(relative, mirrorNormal.normalized);
        }

        /// <summary>
        /// Returns a rotation that is the mirror of <paramref name="rot"/> about a
        /// plane whose normal is <paramref name="mirrorNormal"/>.
        ///
        /// The forward and up vectors of the rotation are each reflected about the
        /// plane, then combined back into a quaternion via LookRotation.
        /// </summary>
        public static Quaternion MirrorRotation(Quaternion rot, Vector3 mirrorNormal) {
            Vector3 n   = mirrorNormal.normalized;
            Vector3 fwd = Vector3.Reflect(rot * Vector3.forward, n);
            Vector3 up  = Vector3.Reflect(rot * Vector3.up,      n);

            // Guard against degenerate vectors
            if (fwd.sqrMagnitude < 0.001f || up.sqrMagnitude < 0.001f) return rot;
            return Quaternion.LookRotation(fwd, up);
        }

        // ── Selection Helper ──────────────────────────────────────────────────

        /// <summary>
        /// Returns the list of part GameObjects to operate on, derived from the
        /// MovementManager's current selection — the same source ObjectDuplicator uses.
        /// Only objects with a Renderer are included (excludes pivots, etc.).
        /// Returns <c>null</c> if nothing is selected.
        /// </summary>
        public static List<GameObject> GetSelectedObjects(MovementManager mm) {
            if (mm == null || mm.MovingObj == null) return null;
            var objs = mm.MovingObj.GetConnectedObjects(true, true);
            return objs?.Where(o => o != null && o.GetComponent<Renderer>() != null).ToList();
        }

        // ── Mirror In-Place ───────────────────────────────────────────────────

        /// <summary>
        /// Mirrors every object in <paramref name="objs"/> in place (no cloning).
        /// Each object is reflected about the plane through <paramref name="center"/>
        /// with normal <paramref name="mirrorNormal"/>.
        ///
        /// Registers the action with the StateSystem so Ctrl+Z restores the
        /// original positions/rotations.
        /// </summary>
        public static void MirrorObjects(List<GameObject> objs, Vector3 center, Vector3 mirrorNormal) {
            if (objs == null || objs.Count == 0) return;

            // Pre-state — capture current transforms for UNDO
            var preElements = new List<IElement>();
            foreach (var obj in objs)
                preElements.Add(new MirrorElement(obj));
            StateSystem.AddElements(preElements);

            // Apply the mirror
            foreach (var obj in objs) {
                obj.transform.position = MirrorPosition(obj.transform.position, center, mirrorNormal);
                obj.transform.rotation = MirrorRotation(obj.transform.rotation, mirrorNormal);
            }

            // Post-state — capture mirrored transforms for REDO
            var postElements = new List<IElement>();
            foreach (var obj in objs)
                postElements.Add(new MirrorElement(obj));
            StateSystem.AddState(new State(postElements));
        }

        // ── Mirror Duplicate ──────────────────────────────────────────────────

        /// <summary>
        /// Instantiates mirrored copies of every object in <paramref name="objs"/>.
        /// The originals are left untouched.  Clones are placed at the mirrored
        /// positions/rotations immediately.
        ///
        /// Registers with StateSystem so Ctrl+Z removes the clones and Ctrl+Y
        /// restores them.
        /// </summary>
        public static void MirrorDuplicate(List<GameObject> objs, Vector3 center, Vector3 mirrorNormal) {
            if (objs == null || objs.Count == 0) return;

            var clones = new List<GameObject>();

            foreach (var obj in objs) {
                Vector3    mirPos = MirrorPosition(obj.transform.position, center, mirrorNormal);
                Quaternion mirRot = MirrorRotation(obj.transform.rotation, mirrorNormal);

                var clone = Object.Instantiate(obj, mirPos, mirRot);
                clone.DisableOutline();

                // Give each clone its own material instance so colour changes don't bleed
                var rend     = clone.GetComponent<Renderer>();
                var origRend = obj.GetComponent<Renderer>();
                if (rend != null && origRend != null)
                    rend.material = new Material(origRend.material);

                clones.Add(clone);
            }

            // Pre-state: record clones as "not yet existing" so UNDO hides them
            var preElements = new List<IElement>();
            foreach (var clone in clones) {
                var pre = new ObjectElement(clone);
                pre.existing = false;          // override: treat clone as non-existent in undo
                preElements.Add(pre);
            }
            StateSystem.AddElements(preElements);

            // Post-state: clones exist at their mirrored positions — REDO restores them
            var postElements = new List<IElement>();
            foreach (var clone in clones)
                postElements.Add(new ObjectElement(clone));
            StateSystem.AddState(new State(postElements));
        }
    }
}
