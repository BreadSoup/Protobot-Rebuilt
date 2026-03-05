using UnityEngine;
using System.Collections;
using DG.Tweening;

namespace Protobot.Transformations {
    /// <summary>
    /// Defines an linear displacement
    /// </summary>
    public class Translation {
        public Vector3 Position { get; }

        public Translation(Vector3 position) {
            Position = position;
        }

        public Tween Translate(GameObject gameObject, float duration = 0.25f) {
            return gameObject.transform.DOMove(Position, duration);
        }
    }

    /// <summary>
    /// Defines an angular displacement.
    /// Orientation is always in world space.
    /// </summary>
    public class Rotation {
        public Quaternion Orientation { get; }

        public Vector3 EulerAngles => Orientation.eulerAngles;
        public Vector3 Forward => Orientation * Vector3.forward;

        public Rotation(Quaternion orientation) {
            Orientation = orientation;
        }
        public Rotation(Vector3 forward) {
            Orientation = Quaternion.LookRotation(forward);
        }

        public Tween Rotate(GameObject gameObject, float duration = 0.25f) {
            // DORotateQuaternion targets a world-space orientation, which is correct
            // because Orientation is always computed in world space (e.g. RotateRing
            // builds it from initRot which is transform.rotation, not localRotation).
            // The old DOLocalRotateQuaternion only worked because the movement pivot
            // is always a root-level object (local == world for root objects).
            return gameObject.transform.DORotateQuaternion(Orientation, duration);
        }
    }

    public class Displacement {
        public Translation translation { get; }
        public Rotation rotation { get; }

        public Displacement(Vector3 position, Quaternion orientation) {
            translation = new Translation(position);
            rotation = new Rotation(orientation);
        }

        public Displacement(Vector3 position, Vector3 forward) {
            translation = new Translation(position);
            rotation = new Rotation(forward);
        }

        public Displacement(Vector3 position) {
            translation = new Translation(position);
            rotation = null;
        }

        public Displacement(Transform transform) {
            translation = new Translation(transform.position);
            rotation = new Rotation(transform.rotation);
        }

        public Tween Displace(GameObject gameObject, float duration = 0.25f) {
            rotation?.Rotate(gameObject, duration);
            return translation.Translate(gameObject, duration);
        }
    }
}