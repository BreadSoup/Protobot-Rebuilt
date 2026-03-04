using UnityEngine;

namespace Protobot.StateSystems {
    /// <summary>
    /// Undo / redo element that restores a shaft part to a previously recorded
    /// length and world position.
    ///
    /// Usage — identical pattern to ResizeElement for aluminum:
    /// <code>
    ///   // 1. Capture the PRE-resize state.
    ///   StateSystem.AddElement(new ShaftResizeElement(shaftData));
    ///   // 2. Move the part and apply the new length.
    ///   shaftData.transform.position = newPos;
    ///   ShaftResizer.ApplyResize(shaftData, newLength);
    ///   // 3. Push POST-resize snapshot for redo.
    ///   StateSystem.AddState(new ShaftResizeElement(shaftData));
    /// </code>
    /// </summary>
    public class ShaftResizeElement : IElement {

        private readonly ShaftResizeData resizeData;
        private readonly float           length;
        private readonly Vector3         position;

        /// <summary>
        /// Snapshots <paramref name="data"/>'s current length and world position.
        /// Load() will restore exactly this state.
        /// </summary>
        public ShaftResizeElement(ShaftResizeData data) {
            resizeData = data;
            length     = data.length;
            position   = data.transform.position;
        }

        public void Load() {
            if (resizeData == null) return;
            resizeData.transform.position = position;
            ShaftResizer.ApplyResize(resizeData, length);
        }
    }
}
