using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Protobot.StateSystems;

namespace Protobot.Tools {
    /// <summary>
    /// Handles rotation of a selected object by dragging a ring gizmo.
    ///
    /// Snapping (applied to global euler angles, not the relative drag delta):
    ///   Hold Shift → snaps to 15° increments
    ///   Hold Ctrl  → snaps to 5° increments
    ///   Snap toggle on (no modifier) → snaps to 5° increments
    ///   No modifier + snap off → free rotation
    ///
    /// "Global" snapping means the final rotation is rounded to absolute values from 0
    /// (e.g. 0°, 5°, 10°, 15°…) rather than relative to where the drag started.
    /// So if a part is at 2.3° and you snap at 5°, it jumps to 5°, then 10° — not 7.3°.
    /// </summary>
    public class RotateRing : RotationTool {
        /// <summary>Global snap toggle — controlled by TransformToolOptions.SetSnapping().</summary>
        public static bool snapping = false;

        [SerializeField] private Camera refCamera = null; // used for dot product angle comparison
        [SerializeField] private VectorLink rotVectorLink;

        private Vector3 initRotVector;   // axis to rotate about, captured at drag start
        private Vector3 initMouseVector; // mouse-to-ring-center vector at drag start
        private Vector3 MouseVector => (MouseInput.Position - refCamera.WorldToScreenPoint(transform.position)).normalized;
        private Quaternion initRot;      // object rotation at drag start

        private Quaternion finalRotation = Quaternion.identity;
        public override Quaternion FinalRotation => finalRotation;

        // -----------------------------------------------------------------------
        // Snapping
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns the snap increment in degrees based on currently held modifier keys
        /// or the global snap toggle. Returns 0 for free (unsnapped) rotation.
        /// </summary>
        private float GetSnapIncrement() {
            // Uses the new Input System (Keyboard.current) — NOT the old Input.GetKey.
            var kb = Keyboard.current;
            if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)) return 15f;
            if (kb != null && (kb.leftCtrlKey.isPressed  || kb.rightCtrlKey.isPressed))  return 5f;
            if (snapping) return 5f;
            return 0f;
        }

        // -----------------------------------------------------------------------
        // Main functions
        // -----------------------------------------------------------------------

        public override void Rotate() {
            Vector3 curMouseVector = MouseVector;
            float angle = Vector2.SignedAngle(curMouseVector, initMouseVector);

            float camDot = Vector3.Dot(refCamera.transform.forward, initRotVector);
            Quaternion newRot = Quaternion.AngleAxis(angle, -initRotVector * Mathf.Sign(camDot)) * initRot;

            float increment = GetSnapIncrement();
            if (increment > 0f) {
                // Snap the final euler angles to the nearest global increment.
                // Rounding the OUTPUT (not the input angle) means snapping is always
                // relative to 0°, so parts land on clean absolute values.
                Vector3 euler = newRot.eulerAngles;
                euler.x = Mathf.Round(euler.x / increment) * increment;
                euler.y = Mathf.Round(euler.y / increment) * increment;
                euler.z = Mathf.Round(euler.z / increment) * increment;
                newRot = Quaternion.Euler(euler);
            }

            movementManager.RotateTo(newRot);
            finalRotation = newRot;
        }

        public override void Initialize() {
            initMouseVector = MouseVector;
            initRot = refObj.transform.rotation;
            initRotVector = rotVectorLink.Vector;
        }

        // -----------------------------------------------------------------------
        // Input Events
        // -----------------------------------------------------------------------

        public override void OnDrag() {
            if (MouseInput.LeftButton.isPressed)
                Rotate();
        }

        public override void OnEndDrag() { }
    }
}