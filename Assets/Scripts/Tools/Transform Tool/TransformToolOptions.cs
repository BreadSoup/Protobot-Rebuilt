using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Protobot.Tools {
    public class TransformToolOptions : MonoBehaviour {
        [SerializeField] private MatchObjectLinkTransform linkMatch;
        [SerializeField] private Transform transformToolCanvas;

        public void OrientGlobal() {
            linkMatch.matchRotation = false;
            transformToolCanvas.eulerAngles = Vector3.zero;
        }

        public void OrientLocal() {
            linkMatch.matchRotation = true;
        }

        public void SetSnapping(bool value) {
            PositionTool.snapping = value;
            RotateRing.snapping = value;
        }
    }
}
