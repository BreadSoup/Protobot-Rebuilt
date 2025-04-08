using UnityEngine;
using DG.Tweening;

namespace Protobot.Tools {
    public class PositionPlane : CastPositionTool {
        [SerializeField] private VectorLink normal;

        public Vector3 finalPosition = Vector3.zero;
        public override Vector3 FinalPosition => finalPosition;

        public override void Move() {
            if (planeCast.hasHit) {
                PositionManager.UpdateValues();
                finalPosition = MoveToPos(planeCast.point);
            }
        }

        public override void Initialize() {
            planeCast.transform.forward = normal.Vector;
            PositionManager = FindObjectOfType<PositionManager>();
        }
    }
}