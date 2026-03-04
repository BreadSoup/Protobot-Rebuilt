using UnityEngine;
using DG.Tweening;

namespace Protobot.Tools {
    public class PositionPlane : CastPositionTool {
        [SerializeField] private VectorLink normal;

        // In-plane position of the object at drag start — snap origin for the plane.
        private Vector3 initInPlane;

        public Vector3 finalPosition = Vector3.zero;
        public override Vector3 FinalPosition => finalPosition;

        public override void Move() {
            if (planeCast.hasHit) {
                float   snapInc = GetSnapIncrement();
                Vector3 point   = planeCast.point;

                if (snapInc > 0f) {
                    // Split into: normal-axis component (fixed — the plane height)
                    // and in-plane component (free — what the user is moving).
                    // Snap the delta from the drag-start in-plane position so that
                    // snap targets are relative to where the object started.
                    Vector3 normalComp = Vector3.Project(point, normal.Vector);
                    Vector3 inPlane    = point - normalComp;
                    Vector3 delta      = inPlane - initInPlane;
                    point = initInPlane + delta.Round(snapInc) + normalComp;
                }

                finalPosition = MoveToPos(point);
            }
        }

        public override void Initialize() {
            planeCast.transform.forward = normal.Vector;
            Vector3 normalComp = Vector3.Project(refObj.transform.position, normal.Vector);
            initInPlane = refObj.transform.position - normalComp;
        }
    }
}