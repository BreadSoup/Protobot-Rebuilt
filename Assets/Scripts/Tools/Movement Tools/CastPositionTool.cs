using UnityEngine;
using UnityEngine.Events;
using System;
using UnityEngine.EventSystems;
using Protobot.StateSystems;
using Protobot.InputEvents;

namespace Protobot.Tools {
    public abstract class CastPositionTool : PositionTool {
        public PlaneCast planeCast;
        public InputEvent dragInput;
        
        internal PositionManager PositionManager;
        public void DisablePlaneObj() {
            planeCast.gameObject.SetActive(false);
        }

        public void EnablePlaneObj() {
            planeCast.gameObject.SetActive(true);
            planeCast.transform.position = refObj.transform.position;
        }

        public override void OnPointerDown() {
            base.OnPointerDown();
            EnablePlaneObj();            
        }

        public override void OnDrag() {
            if (dragInput.IsPressed) {
                Move();
                PositionManager.UpdateValues();
            }
        }

        public override void OnEndDrag() {
            DisablePlaneObj();
        }
    }
}