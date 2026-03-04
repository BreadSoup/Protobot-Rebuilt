using UnityEngine;

namespace Protobot.StateSystems {
    /// <summary>
    /// Undo / redo element that restores an aluminum part to a previously recorded
    /// hole count and world position.
    ///
    /// Usage — wrap a resize with undo the same way ObjectElement does for moves:
    /// <code>
    ///   // 1. Capture the PRE-resize state and add it to the current undo slot.
    ///   StateSystem.AddElement(new ResizeElement(resizeData));
    ///   // 2. Move the part and apply the new hole count.
    ///   resizeData.transform.position = newPos;
    ///   AluminumResizer.ApplyResize(resizeData, newCount);
    ///   // 3. Push a new state containing the POST-resize snapshot for redo.
    ///   StateSystem.AddState(new ResizeElement(resizeData));
    /// </code>
    /// When the user presses Ctrl+Z, the PRE-resize snapshot's Load() fires,
    /// restoring the old hole count and position.  Ctrl+Y re-applies the POST
    /// snapshot.
    /// </summary>
    public class ResizeElement : IElement {

        private readonly AluminumResizeData resizeData;
        private readonly int               holeCount;
        private readonly Vector3           position;

        /// <summary>
        /// Snapshots <paramref name="data"/>'s current hole count and world position.
        /// Load() will restore exactly this state.
        /// </summary>
        public ResizeElement(AluminumResizeData data) {
            resizeData = data;
            holeCount  = data.holeCount;
            position   = data.transform.position;
        }

        public void Load() {
            if (resizeData == null) return;
            // Restore position first so CalcNewPosition side-effects don't interfere.
            resizeData.transform.position = position;
            AluminumResizer.ApplyResize(resizeData, holeCount);
        }
    }
}
