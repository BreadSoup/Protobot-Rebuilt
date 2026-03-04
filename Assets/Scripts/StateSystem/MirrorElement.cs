using DG.Tweening;
using UnityEngine;

namespace Protobot.StateSystems {
    /// <summary>
    /// Undo / redo element that restores a GameObject's world position and rotation
    /// to values captured at construction time.
    ///
    /// MirrorSystem creates two of these per object when mirroring in-place:
    ///   • A PRE element (original transform)  → added to the current state → UNDO
    ///   • A POST element (mirrored transform) → pushed as a new state     → REDO
    ///
    /// The 0.25-second DOTween animation matches the move/rotate style used
    /// throughout the rest of the state system (see ObjectElement).
    /// </summary>
    public class MirrorElement : IElement {

        private readonly GameObject obj;
        private readonly Vector3    position;
        private readonly Quaternion rotation;

        /// <summary>
        /// Snapshots the object's current world position and rotation.
        /// <c>Load()</c> will restore exactly this state.
        /// </summary>
        public MirrorElement(GameObject go) {
            obj      = go;
            position = go.transform.position;
            rotation = go.transform.rotation;
        }

        public void Load() {
            if (obj == null) return;

            if (obj.transform.position != position)
                obj.transform.DOMove(position, 0.25f);

            if (obj.transform.rotation != rotation)
                obj.transform.DORotateQuaternion(rotation, 0.25f);
        }
    }
}
