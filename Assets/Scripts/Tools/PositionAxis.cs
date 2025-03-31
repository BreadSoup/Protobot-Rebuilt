using UnityEngine;
using DG.Tweening;
using Protobot.StateSystems;

namespace Protobot.Tools {
    public class PositionAxis : CastPositionTool {
        public VectorLink normal;
        [HideInInspector] public Vector3 initObjPos;
    
        public Vector3 finalPosition = Vector3.zero;
        public override Vector3 FinalPosition => finalPosition;

        public override void Move() {
            if (planeCast.hasHit) {
                initObjPos -= Vector3.Project(initObjPos, normal.Vector);
                Vector3 point = initObjPos + Vector3.Project(planeCast.point, normal.Vector);

                finalPosition = MoveToPos(point);
            }
        }

        public override void Initialize() {
            planeCast.GetComponent<LookAt>().vectorLink = normal;
            initObjPos = refObj.transform.position;
            PositionManager = FindObjectOfType<PositionManager>();
        }
    }
}
