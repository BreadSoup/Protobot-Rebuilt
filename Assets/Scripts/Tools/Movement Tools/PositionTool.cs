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
            //   Shift → 0.50-unit increments
            //   Ctrl  → 0.25-unit increments
            //   Toggle ON (no modifier) → 0.25-unit increments
            var kb = Keyboard.current;
            float snapInc = 0f;
            if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed))
                snapInc = 0.5f;
            else if (kb != null && (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed))
                snapInc = 0.25f;
            else if (snapping)
                snapInc = 0.25f;

            if (snapInc > 0f)
            {
                // Snap the absolute world position, not a delta from the object's
                // current position.  The object is animated by DOTween (0.25 s tween),
                // so its transform.position changes every frame while it's in motion.
                // A delta-based round would compute a different grid offset each frame
                // and produce jitter.  Rounding the raw target position avoids this.
                pos = pos.Round(snapInc);
            }

            movementManager.MoveTo(pos);
            return pos;
        }
    }
}