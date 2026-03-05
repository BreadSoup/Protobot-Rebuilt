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
    /// Snapping (snaps the drag angle, not the final Euler angles, to avoid
    /// gimbal-lock artifacts near 270°):
    ///   Hold Shift → 15° increments
    ///   Hold Ctrl  → 5° increments
    ///   Snap toggle on (no modifier) → 5° increments
    ///   No modifier + snap off → free rotation
    ///
    /// Local / Global space (IsLocalSpace):
    ///   false (default) — rings rotate around world X / Y / Z axes.
    ///   true            — rings rotate around the selected object's own
    ///                     local X / Y / Z axes, so the gizmo follows the
    ///                     object as you rotate it.
    ///
    /// How local space works:
    ///   SyncRingLocalSpace (in PropertiesMenuUI) sets matchRotation = true on
    ///   each ring container's MatchObjectLinkTransform. This means the container's
    ///   world rotation always matches the selected object. Because rotVectorLink is
    ///   a TransformVectorLink pointing to the ring's own transform, its Vector is
    ///   already the object's local axis in world space — no manual remapping needed.
    /// </summary>
    public class RotateRing : RotationTool {
        /// <summary>Global snap toggle — toggled by TransformToolOptions.</summary>
        public static bool snapping = false;

        /// <summary>
        /// When true, rotation axes are taken from the selected object's local
        /// transform (local space). When false, world axes are used (global space).
        /// Toggled by the Local/Global checkbox in the Properties Menu.
        /// </summary>
        public static bool IsLocalSpace = false;

        [SerializeField] private Camera refCamera = null;
        [SerializeField] private VectorLink rotVectorLink;

        private Vector3 initRotVector;   // rotation axis captured at drag start
        private Vector3 initMouseVector; // mouse-to-ring-center at drag start
        private Vector3 MouseVector => (MouseInput.Position - refCamera.WorldToScreenPoint(transform.position)).normalized;
        private Quaternion initRot;      // object rotation at drag start

        private Quaternion finalRotation = Quaternion.identity;
        public override Quaternion FinalRotation => finalRotation;

        // -----------------------------------------------------------------------
        // Snapping
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns the snap increment in degrees based on modifier keys / toggle.
        /// Returns 0 for free (unsnapped) rotation.
        /// Uses the new Input System — NOT the old Input.GetKey.
        /// </summary>
        private float GetSnapIncrement() {
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
            float angle = Vector2.SignedAngle(MouseVector, initMouseVector);

            // Snap the raw drag angle BEFORE computing the quaternion.
            // This avoids converting through Euler angles, which causes gimbal-lock
            // glitches (e.g. the 270° X-axis flip bug).
            float increment = GetSnapIncrement();
            if (increment > 0f)
                angle = Mathf.Round(angle / increment) * increment;

            float camDot = Vector3.Dot(refCamera.transform.forward, initRotVector);
            Quaternion newRot = Quaternion.AngleAxis(angle, -initRotVector * Mathf.Sign(camDot)) * initRot;

            movementManager.RotateTo(newRot);
            finalRotation = newRot;
        }

        public override void Initialize() {
            initMouseVector = MouseVector;
            initRot = refObj.transform.rotation;

            // rotVectorLink is a TransformVectorLink pointing to this ring's own
            // transform. Its Vector is therefore always the correct world-space
            // rotation axis — no remapping needed for either mode:
            //   • Global: ring container is world-aligned → Vector = world X/Y/Z
            //   • Local:  ring container matches the object's rotation
            //             (matchRotation = true) → Vector = object's local axis
            //             already expressed in world space.
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
