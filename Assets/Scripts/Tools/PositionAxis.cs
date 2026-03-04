using UnityEngine;
using DG.Tweening;
using Protobot.StateSystems;

namespace Protobot.Tools {
    public class PositionAxis : CastPositionTool {
        public VectorLink normal;
        [HideInInspector] public Vector3 initObjPos;
        // Signed distance along the axis at drag start — used as the snap origin
        // so snapped positions are initAxisDist ± N×snapInc, not world multiples.
        private float initAxisDist;

        public Vector3 finalPosition = Vector3.zero;
        public override Vector3 FinalPosition => finalPosition;

        public override void Move() {
            if (planeCast.hasHit) {
                initObjPos -= Vector3.Project(initObjPos, normal.Vector);

                Vector3 axisDir  = normal.Vector.normalized;
                float   axisDist = Vector3.Dot(planeCast.point, axisDir);
                float   snapInc  = GetSnapIncrement();
                if (snapInc > 0f) {
                    // Snap the delta from the drag-start position, not the absolute
                    // world distance.  This keeps snap targets relative to where the
                    // object was (e.g. 1.12 → 1.37 → 1.62 with 0.25 snap).
                    float delta = axisDist - initAxisDist;
                    axisDist = initAxisDist + Mathf.Round(delta / snapInc) * snapInc;
                }

                Vector3 point = initObjPos + axisDir * axisDist;
                finalPosition = MoveToPos(point);
            }
        }

        public override void Initialize() {
            planeCast.GetComponent<LookAt>().vectorLink = normal;
            initObjPos    = refObj.transform.position;
            initAxisDist  = Vector3.Dot(initObjPos, normal.Vector.normalized);
        }
    }
}
