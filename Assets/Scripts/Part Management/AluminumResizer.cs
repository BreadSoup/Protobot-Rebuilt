using Parts_List;
using UnityEngine;

namespace Protobot {
    /// <summary>
    /// Static helper that rebuilds an aluminum part's mesh and hole colliders
    /// in-place when the hole count changes.
    ///
    /// Called by PropertiesMenuUI (live editing) and ResizeElement (undo/redo).
    ///
    /// Hole-count math reference:
    ///   Each hole occupies 0.5 VEX units of length, so:
    ///     half-length = holeCount * 0.25
    ///   The mesh is always centered at the GameObject's origin, so the left
    ///   and right edges (in local X) are at ±(holeCount * 0.25).
    /// </summary>
    public static class AluminumResizer {

        // ── Mesh + collider rebuild ───────────────────────────────────────────

        /// <summary>
        /// Rebuilds the mesh and hole colliders on <paramref name="data"/>'s
        /// GameObject to match <paramref name="newCount"/> holes.
        ///
        /// Move the part to <see cref="CalcNewPosition"/> BEFORE calling this
        /// if you want one edge to remain fixed in world space.
        /// </summary>
        public static void ApplyResize(AluminumResizeData data, int newCount) {
            newCount = Mathf.Clamp(newCount, data.minHoleCount, data.maxHoleCount);
            if (newCount == data.holeCount) return;

            var part = data.gameObject;

            // ── Replace mesh ──────────────────────────────────────────────────
            Mesh newMesh = data.subParts.GetMesh(newCount);
            var mf = part.GetComponent<MeshFilter>();
            var mc = part.GetComponent<MeshCollider>();
            if (mf != null) mf.sharedMesh = newMesh;
            if (mc != null) mc.sharedMesh = newMesh;

            // ── Replace hole colliders ────────────────────────────────────────
            // Disconnect snap connections first so no HoleDetectors are orphaned.
            foreach (var h in part.GetComponentsInChildren<HoleCollider>()) {
                for (int i = h.detectors.Count - 1; i >= 0; i--)
                    h.detectors[i].RemoveHole(h);
                Object.Destroy(h.gameObject);
            }
            data.subParts.GenerateHoles(part, newCount);

            // ── Update metadata ───────────────────────────────────────────────
            data.holeCount = newCount;

            if (part.TryGetComponent(out PartName pn)) {
                pn.name          = data.namePrefix + newCount + data.nameSuffix;
                pn.param2Label   = data.param2Label;
                pn.param2Display = string.Format(data.param2DisplayFormat, newCount);
                pn.weightInGrams = data.weightPerHole * newCount;
            }

            // SavedObject.id format: "PartTypeId-param1value-param2value"
            // For aluminum, param2 is the hole count — replace the last segment.
            if (part.TryGetComponent(out SavedObject so)) {
                int lastDash = so.id.LastIndexOf('-');
                if (lastDash >= 0)
                    so.id = so.id.Substring(0, lastDash + 1) + newCount;
            }
        }

        // ── Position calculation ──────────────────────────────────────────────

        /// <summary>
        /// Returns the world position the part should be moved to so that
        /// one of its ends stays at a fixed world location when hole count changes.
        ///
        /// "Right edge" = the +local-X end (higher section index).
        /// "Left edge"  = the −local-X end (lower section index / index 0).
        /// </summary>
        /// <param name="part">Part transform (position + right-axis).</param>
        /// <param name="currentCount">Current hole count.</param>
        /// <param name="newCount">Target hole count.</param>
        /// <param name="keepRightEdge">
        ///   true  = keep the +X (right) edge fixed → modify from the left side.
        ///   false = keep the −X (left)  edge fixed → modify from the right side.
        /// </param>
        public static Vector3 CalcNewPosition(Transform part, int currentCount,
                                              int newCount, bool keepRightEdge) {
            // half-length change:  deltaHalf = (newCount − currentCount) * 0.25
            float deltaHalf = (newCount - currentCount) * 0.25f;

            // keepRightEdge: right-edge world pos must not change.
            //   P_new + right * newHalf = P + right * currentHalf
            //   → shift = −right * deltaHalf
            // keepLeftEdge: left-edge world pos must not change.
            //   P_new − right * newHalf = P − right * currentHalf
            //   → shift = +right * deltaHalf
            Vector3 shift = keepRightEdge
                ? -part.right * deltaHalf
                :  part.right * deltaHalf;

            return part.position + shift;
        }

        // ── Preview position helpers ──────────────────────────────────────────

        /// <summary>
        /// World-space centre position for the delta-mesh preview (the ghost that
        /// shows which sections will be removed or added).
        ///
        /// A mesh of <paramref name="absDelta"/> holes (centered at its own origin)
        /// placed here will sit exactly at the end of the current part that will be
        /// modified — overlapping for removal, adjacent for addition.
        /// </summary>
        /// <param name="part">Part transform.</param>
        /// <param name="currentCount">Current hole count.</param>
        /// <param name="newCount">Target hole count.</param>
        /// <param name="modifyLeft">
        ///   true  = the left  (−X) end is being modified.
        ///   false = the right (+X) end is being modified.
        /// </param>
        public static Vector3 CalcPreviewCenter(Transform part, int currentCount,
                                                int newCount, bool modifyLeft) {
            // For both removal and addition:
            //   modifyLeft  → preview center = part.position − right * newCount * 0.25
            //   modifyRight → preview center = part.position + right * newCount * 0.25
            float sign = modifyLeft ? -1f : 1f;
            return part.position + sign * part.right * newCount * 0.25f;
        }

        /// <summary>
        /// World-space position for the directional indicator (blue sphere / arrow)
        /// placed at the edge of the current part that will be modified.
        /// </summary>
        public static Vector3 CalcArrowPos(Transform part, int currentCount, bool modifyLeft) {
            float sign = modifyLeft ? -1f : 1f;
            return part.position + sign * part.right * currentCount * 0.25f;
        }
    }
}
