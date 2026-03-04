using Parts_List;
using UnityEngine;

namespace Protobot {
    /// <summary>
    /// Static helper that resizes a shaft part in-place when its length changes.
    ///
    /// Called by PropertiesMenuUI (live editing) and ShaftResizeElement (undo/redo).
    ///
    /// Length math reference:
    ///   The shaft mesh is centered at the GameObject's local origin.
    ///   Its extent along local Z is:  ±(length / 2)
    ///   Changing the length by Δ shifts the centre by ±(Δ / 2) to keep
    ///   one end fixed in world space.
    /// </summary>
    public static class ShaftResizer {

        // ── Resize ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Scales the shaft to <paramref name="newLength"/> inches and updates all
        /// metadata (PartName, SavedObject).
        ///
        /// Move the part to <see cref="CalcNewPosition"/> BEFORE calling this
        /// if you want one end to remain fixed in world space.
        /// </summary>
        public static void ApplyResize(ShaftResizeData data, float newLength) {
            newLength = Mathf.Clamp(newLength, data.minLength, data.maxLength);
            if (Mathf.Approximately(newLength, data.length)) return;

            var part = data.gameObject;

            // ── Scale the shaft along its local Z axis ────────────────────────
            // The shaft mesh is centered at the origin; Unity propagates the
            // transform scale to both the MeshRenderer and any MeshColliders.
            Vector3 s = part.transform.localScale;
            s.z = newLength;
            part.transform.localScale = s;

            // ── Update metadata ───────────────────────────────────────────────
            data.length = newLength;

            if (part.TryGetComponent(out PartName pn)) {
                pn.name          = newLength + data.nameSuffix;
                pn.param2Display = newLength + "\"";
                pn.weightInGrams = data.weightPerInch * newLength;
            }

            // SavedObject.id format: "PartTypeId-param1value-param2value"
            // For shafts, param2 is the length — replace the last segment.
            if (part.TryGetComponent(out SavedObject so)) {
                int lastDash = so.id.LastIndexOf('-');
                if (lastDash >= 0)
                    so.id = so.id.Substring(0, lastDash + 1) + newLength;
            }
        }

        // ── Position calculation ─────────────────────────────────────────────────

        /// <summary>
        /// Returns the world position the shaft should be moved to so that
        /// one of its ends stays at a fixed world location when length changes.
        ///
        /// "Far end"  = the +local-Z end.
        /// "Near end" = the −local-Z end.
        /// </summary>
        /// <param name="part">Shaft transform.</param>
        /// <param name="currentLength">Current length in inches.</param>
        /// <param name="newLength">Target length in inches.</param>
        /// <param name="keepFarEnd">
        ///   true  = keep the +Z (far)  end fixed → grow/shrink from the near end.
        ///   false = keep the −Z (near) end fixed → grow/shrink from the far  end.
        /// </param>
        public static Vector3 CalcNewPosition(Transform part, float currentLength,
                                              float newLength, bool keepFarEnd) {
            float deltaHalf = (newLength - currentLength) * 0.5f;

            // keepFarEnd: far-end world pos must not change.
            //   newCenter + forward * newHalf = center + forward * currentHalf
            //   → shift = −forward * deltaHalf
            // keepNearEnd: near-end world pos must not change.
            //   newCenter − forward * newHalf = center − forward * currentHalf
            //   → shift = +forward * deltaHalf
            Vector3 shift = keepFarEnd
                ? -part.forward * deltaHalf
                :  part.forward * deltaHalf;

            return part.position + shift;
        }
    }
}
