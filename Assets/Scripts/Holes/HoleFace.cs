using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Protobot {
    /// <summary>
    /// World-space overlay that visually marks a selected hole on a part.
    ///
    /// The HoleCollider that owns the HoleData runs its own Update() every frame
    /// and keeps holeData.position / holeData.rotation / holeData.forward in sync
    /// with the part's current transform.  This Update() mirrors those live values
    /// so the overlay automatically follows the hole when the part moves or rotates
    /// (e.g. via the ring gizmo or the Properties Menu rotation fields).
    /// </summary>
    public class HoleFace : MonoBehaviour {
        public Vector3 direction;
        public Vector3 position => transform.position;
        public Quaternion Rotation => transform.rotation;
        public Quaternion LookRotation => Quaternion.LookRotation(direction, transform.up);
        public HoleData hole;

        [CacheComponent] private MeshFilter meshFilter;

        // +1 if direction was along hole.forward when Set() was called, -1 if opposite.
        // Stored so Update() can recalculate the correct face direction as the part rotates.
        private float dirSign = 1f;

        public void Set(HoleData newHole, Vector3 newDir) {
            transform.rotation = Quaternion.LookRotation(-newDir, newHole.rotation * Vector3.up);

            Vector3 newHolePos = newHole.position;
            Vector3 newPos = newHolePos + (newDir * (newHole.depth / 2));
            transform.position = newPos;

            direction = newDir;

            // Record which side of the hole we're on (along or against hole.forward)
            // so Update() can keep direction correct as hole.forward rotates with the part.
            dirSign = Vector3.Dot(newDir, newHole.forward) >= 0f ? 1f : -1f;

            meshFilter.mesh = newHole.shape;
            transform.localScale = new Vector3(newHole.size.x, newHole.size.y, 0.001f);

            hole = newHole;
        }

        public void Set(HoleFace newHoleFace) => Set(newHoleFace.hole, newHoleFace.direction);

        private void Update() {
            if (hole == null) return;

            // hole.position, hole.rotation, and hole.forward are kept current every frame
            // by HoleCollider.Update(), so using them here tracks the hole as the part moves.
            direction = hole.forward * dirSign;

            // Guard: LookRotation requires a non-zero forward vector.
            // If hole.forward is zero (e.g. during initialisation or a degenerate state)
            // skip this frame rather than spamming "Look rotation viewing vector is zero".
            if (direction == Vector3.zero) return;

            transform.rotation = Quaternion.LookRotation(-direction, hole.rotation * Vector3.up);
            transform.position  = hole.position + (direction * (hole.depth / 2));
        }
    }
}
