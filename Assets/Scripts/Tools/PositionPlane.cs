using UnityEngine;
using DG.Tweening;

namespace Protobot.Tools {
    public class PositionPlane : CastPositionTool {
        [SerializeField] private VectorLink normal;

        public Vector3 finalPosition = Vector3.zero;
        public override Vector3 FinalPosition => finalPosition;

        public override void Move() {
            if (planeCast.hasHit) {
                float   snapInc = GetSnapIncrement();
                Vector3 point   = planeCast.point;

                if (snapInc > 0f) {
                    // Split into: normal-axis component (fixed — the plane height)
                    // and in-plane component (free — what the user is moving).
                    // Only snap the in-plane part so the object stays on its plane
                    // and the fixed axis doesn't jump to an unexpected grid value.
                    Vector3 normalComp = Vector3.Project(point, normal.Vector);
                    Vector3 inPlane    = point - normalComp;
                    point = inPlane.Round(snapInc) + normalComp;
                }

                finalPosition = MoveToPos(point);
            }
        }

        public override void Initialize() {
            planeCast.transform.forward = normal.Vector;
        }
    }
}