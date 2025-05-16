using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Protobot.StateSystems;

namespace Protobot.Tools {
    public class RotateRing : RotationTool {
        public static bool snapping = true;
        
        [SerializeField] private Camera refCamera = null; //used for dot product angle comparison

        [SerializeField] private VectorLink rotVectorLink;
        
        private Vector3 initRotVector; //the initial Vector to rotate about

        private Vector3 initMouseVector; //the initial Vector between the mouse position and the selection position
        private Vector3 MouseVector => (MouseInput.Position - refCamera.WorldToScreenPoint(transform.position)).normalized;
        private Quaternion initRot; //the intiial rotation of the selection

        private Quaternion finalRotation = Quaternion.identity;
        public override Quaternion FinalRotation => finalRotation;

        private const float Increment = 5;
        
        private PositionManager positionManager;

        //Main functions
        public override void Rotate() {
            Vector3 curMouseVector = MouseVector;
            float angle = Vector2.SignedAngle(curMouseVector, initMouseVector);
            
            if (snapping)
                angle = Mathf.Round(angle * 1/Increment) * Increment; //force values to round at specific increment

            float camDot = Vector3.Dot(refCamera.transform.forward, initRotVector); //finds the dot product between the camera direction vs. the look direction of this rotate ring
            Quaternion newRot = Quaternion.AngleAxis(angle, -initRotVector * Mathf.Sign(camDot)) * initRot;
            Debug.Log($"[Rotate] Computed Angle: {angle}");
            Debug.Log($"[Rotate] Initial Rotation (Euler): {initRot.eulerAngles}");
            Debug.Log($"[Rotate] New Rotation (Euler): {newRot.eulerAngles}");

            movementManager.RotateTo(newRot);

            finalRotation = newRot;
        }

        public override void Initialize()
        {
            positionManager = FindObjectOfType<PositionManager>();
            initMouseVector = MouseVector;
            
            initRot = refObj.transform.rotation;
            initRotVector = rotVectorLink.Vector;
        }

        //Input Events
        public override void OnDrag()
        {
            if (MouseInput.LeftButton.isPressed)
            {
                Rotate();
                positionManager.UpdateValues();
            }
        }

        public override void OnEndDrag()
        {
            
        }
    }
}