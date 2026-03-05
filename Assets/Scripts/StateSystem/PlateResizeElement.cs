using UnityEngine;

namespace Protobot.StateSystems {
    /// <summary>
    /// Undo / redo element that restores a plate part to a previously recorded
    /// length, width, and world position.
    ///
    /// Usage — identical pattern to ResizeElement for aluminum:
    /// <code>
    ///   // 1. Capture the PRE-resize state.
    ///   StateSystem.AddElement(new PlateResizeElement(plateData));
    ///   // 2. Move the plate and apply the new dimensions.
    ///   plateData.transform.position = newPos;
    ///   PlateResizer.ApplyResize(plateData, newLength, newWidth);
    ///   // 3. Push POST-resize snapshot for redo.
    ///   StateSystem.AddState(new PlateResizeElement(plateData));
    /// </code>
    /// </summary>
    public class PlateResizeElement : IElement {

        private readonly PlateResizeData resizeData;
        private readonly int             length;
        private readonly int             width;
        private readonly Vector3         position;

        /// <summary>
        /// Snapshots <paramref name="data"/>'s current length, width, and world
        /// position. Load() will restore exactly this state.
        /// </summary>
        public PlateResizeElement(PlateResizeData data) {
            resizeData = data;
            length     = data.length;
            width      = data.width;
            position   = data.transform.position;
        }

        public void Load() {
            if (resizeData == null) return;
            resizeData.transform.position = position;
            PlateResizer.ApplyResize(resizeData, length, width);
        }
    }
}
