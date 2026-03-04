using UnityEngine;
using UnityEngine.Events;
using System;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Protobot.StateSystems;

namespace Protobot.Tools {
    public abstract class PositionTool : ClickDragTool {
        public static bool snapping = false;
        public abstract Vector3 FinalPosition {get;}

        /// <summary> Executes right when mouse is down before dragging </summary>
        abstract public void Initialize();
        abstract public void Move();

        //Input events
        public override void OnPointerDown() {
            Initialize();
        }

        public override void OnPointerUp() {
        }
        public Vector3 MoveToPos(Vector3 pos)
        {
            // Modifier keys override the snap toggle, mirroring rotation behaviour:
            //   Shift → 0.25-unit increments
            //   Ctrl  → 0.10-unit increments
            //   Toggle ON (no modifier) → 0.125-unit increments
            var kb = Keyboard.current;
            float snapInc = 0f;
            if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed))
                snapInc = 0.25f;
            else if (kb != null && (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed))
                snapInc = 0.1f;
            else if (snapping)
                snapInc = 0.125f;

            if (snapInc > 0f)
            {
                var refObj = movementManager.MovingObj;
                if (refObj != null)
                {
                    var relativePos = pos - refObj.transform.position;
                    relativePos = relativePos.Round(snapInc);
                    pos = refObj.transform.position + relativePos;
                }
                else
                {
                    pos = pos.Round(snapInc);
                }
            }

            movementManager.MoveTo(pos);
            return pos;
        }
    }
}