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
        /// <summary>
        /// Returns the active snap increment based on held modifier keys and the
        /// snap toggle.  0 means no snapping.  Subclasses call this to snap only
        /// the component(s) they are responsible for moving, so perpendicular axes
        /// are never rounded and the object doesn't jump sideways.
        ///   Shift held       → 0.50-unit grid
        ///   Ctrl  held       → 0.25-unit grid
        ///   Snap toggle ON   → 0.25-unit grid
        /// </summary>
        public static float GetSnapIncrement()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)) return 0.5f;
            if (kb != null && (kb.leftCtrlKey.isPressed  || kb.rightCtrlKey.isPressed))  return 0.25f;
            if (snapping) return 0.25f;
            return 0f;
        }

        public Vector3 MoveToPos(Vector3 pos)
        {
            // Snapping is applied by each subclass before calling MoveToPos so that
            // only the moving axis/plane components are rounded.  Rounding the full
            // world position here would also round fixed perpendicular components
            // (e.g. the Y of an object not on a 0.5-unit grid) causing sideways jumps.
            movementManager.MoveTo(pos);
            return pos;
        }
    }
}