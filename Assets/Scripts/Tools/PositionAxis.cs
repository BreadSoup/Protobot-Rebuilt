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

                // Snap only the distance along the movement axis.
                // Rounding the full world position would also round the fixed
                // perpendicular components stored in initObjPos, causing the
                // object to jump sideways if those aren't on the snap grid.
                Vector3 axisDir  = normal.Vector.normalized;
                float   axisDist = Vector3.Dot(planeCast.point, axisDir);
                float   snapInc  = GetSnapIncrement();
                if (snapInc > 0f)
                    axisDist = Mathf.Round(axisDist / snapInc) * snapInc;

                Vector3 point = initObjPos + axisDir * axisDist;
                finalPosition = MoveToPos(point);
            }
        }

        public override void Initialize() {
            planeCast.GetComponent<LookAt>().vectorLink = normal;
            initObjPos = refObj.transform.position;
        }
    }
}
