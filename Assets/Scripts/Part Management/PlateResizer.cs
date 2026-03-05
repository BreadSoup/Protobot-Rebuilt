using Parts_List;
using UnityEngine;

namespace Protobot {
    /// <summary>
    /// Static helper that rebuilds a plate part's mesh and hole colliders
    /// in-place when the length or width changes.
    ///
    /// Called by PropertiesMenuUI (live editing) and PlateResizeElement (undo/redo).
    ///
    /// Hole-count math reference:
    ///   Each hole occupies 0.5 VEX units in both X and Y.
    ///   Half-extent (edge to centre) = holeCount * 0.25
    ///   Centre-shift when count changes by Δ = Δ * 0.25
    /// </summary>
    public static class PlateResizer {

        // ── Mesh + collider rebuild ───────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the mesh and hole colliders on <paramref name="data"/>'s
        /// GameObject to match the given dimensions.
        ///
        /// Move the part to <see cref="CalcNewPosition"/> BEFORE calling this
        /// if you want an edge to remain fixed in world space.
        /// </summary>
        public static void ApplyResize(PlateResizeData data, int newLength, int newWidth) {
            newLength = Mathf.Clamp(newLength, data.minLength, data.maxLength);
            newWidth  = Mathf.Clamp(newWidth,  data.minWidth,  data.maxWidth);
            if (newLength == data.length && newWidth == data.width) return;

            var part = data.gameObject;

            // ── Replace mesh ──────────────────────────────────────────────────
            Mesh newMesh = data.generator.GetMesh(newLength, newWidth);
            var mf = part.GetComponent<MeshFilter>();
            var mc = part.GetComponent<MeshCollider>();
            if (mf != null) mf.sharedMesh = newMesh;
            if (mc != null) mc.sharedMesh = newMesh;

            // ── Break group connections for screws in holes being removed ─────
            // (Same logic as AluminumResizer — OnRemoveTargetHole only fires for
            //  threaded holes, so we break edges explicitly here.)
            foreach (var h in part.GetComponentsInChildren<HoleCollider>()) {
                foreach (var detector in h.detectors) {
                    var cp = detector.GetComponentInParent<ConnectingPart>();
                    if (cp != null)
                        GroupManager.BreakConnection(cp, part);
                }
            }

            // ── Replace hole colliders ────────────────────────────────────────
            foreach (var h in part.GetComponentsInChildren<HoleCollider>()) {
                for (int i = h.detectors.Count - 1; i >= 0; i--)
                    h.detectors[i].RemoveHole(h);
                Object.Destroy(h.gameObject);
            }

            // GenerateHoles uses Instantiate(obj, worldPos, worldRot, parent).
            // Temporarily reset the transform so local coords == world coords.
            Vector3    savedPos = part.transform.position;
            Quaternion savedRot = part.transform.rotation;
            part.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            data.generator.GenerateHoles(part, newLength, newWidth);

            part.transform.SetPositionAndRotation(savedPos, savedRot);

            // ── Update metadata ───────────────────────────────────────────────
            data.length = newLength;
            data.width  = newWidth;

            if (part.TryGetComponent(out PartName pn)) {
                pn.name          = "Plate (" + newLength + "x" + newWidth + ")";
                pn.param1Display = newLength + " holes";
                pn.param2Display = newWidth  + " holes";
                pn.weightInGrams = newLength * newWidth * data.weightPerHole;
            }

            // SavedObject.id format: "PartTypeId-param1value-param2value"
            // For plates, param1=length, param2=width — replace the last two segments.
            if (part.TryGetComponent(out SavedObject so)) {
                int lastDash = so.id.LastIndexOf('-');
                if (lastDash > 0) {
                    int secondLastDash = so.id.LastIndexOf('-', lastDash - 1);
                    if (secondLastDash >= 0)
                        so.id = so.id.Substring(0, secondLastDash + 1) +
                                newLength + "-" + newWidth;
                }
            }
        }

        // ── Position calculation ──────────────────────────────────────────────────

        /// <summary>
        /// Returns the world position the plate should be moved to so that
        /// a chosen pair of edges stays at a fixed world location when dimensions change.
        /// </summary>
        /// <param name="part">Plate transform.</param>
        /// <param name="curLen">Current length (holes).</param>
        /// <param name="newLen">Target length (holes).</param>
        /// <param name="keepRightEdge">
        ///   true  = keep +X edge fixed (grow/shrink from the left).
        ///   false = keep −X edge fixed (grow/shrink from the right).
        /// </param>
        /// <param name="curWid">Current width (holes).</param>
        /// <param name="newWid">Target width (holes).</param>
        /// <param name="keepTopEdge">
        ///   true  = keep +Y edge fixed (grow/shrink from the bottom).
        ///   false = keep −Y edge fixed (grow/shrink from the top).
        /// </param>
        public static Vector3 CalcNewPosition(Transform part,
                                              int curLen, int newLen, bool keepRightEdge,
                                              int curWid, int newWid, bool keepTopEdge) {
            float deltaHalfX = (newLen - curLen) * 0.25f;
            float deltaHalfY = (newWid - curWid) * 0.25f;

            // keepRightEdge → right (+X) stays fixed → shift left (negate)
            // keepLeftEdge  → left  (−X) stays fixed → shift right
            Vector3 xShift = keepRightEdge
                ? -part.right * deltaHalfX
                :  part.right * deltaHalfX;

            // keepTopEdge   → top (+Y) stays fixed → shift down (negate)
            // keepBottomEdge → bottom (−Y) stays fixed → shift up
            Vector3 yShift = keepTopEdge
                ? -part.up * deltaHalfY
                :  part.up * deltaHalfY;

            return part.position + xShift + yShift;
        }
    }
}
