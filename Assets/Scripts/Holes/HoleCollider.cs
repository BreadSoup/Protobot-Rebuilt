using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Protobot {
    public class HoleCollider : MonoBehaviour {
        public static int HOLE_COLLISIONS_LAYER => 11;
        public static int HOLE_COLLISIONS_MASK => LayerMask.GetMask("HoleCollisions");

        public HoleData holeData;

        public Action<HoleDetector> OnSetDetector;

        public enum HoleType { 
            Normal,
            Threaded,
            Clamp
        }
        
        public HoleType holeType;

        public List<HoleDetector> detectors; // the list of hole detectors attached to this collider

        public bool twoSided = false;
        private int MaxDetectorLimit => twoSided ? 2 : 1;
        
        public bool IsOccupied => detectors.Count == MaxDetectorLimit;
        public bool IsEmpty => detectors.Count == 0;

        private void Awake() {
            detectors = new List<HoleDetector>();
            MeshCollider meshCollider = GetComponent<MeshCollider>();

            holeData = new HoleData {
                position = transform.position,
                rotation = transform.rotation,
                forward = transform.forward,

                shape = meshCollider.sharedMesh,
                depth = transform.localScale.z,
                size = new Vector2(transform.localScale.x, transform.localScale.y),
                part = transform.parent.gameObject
            };
        }

        private void Update() {
            holeData.position = transform.position;
            holeData.rotation = transform.rotation;
            holeData.forward = transform.forward;

            RemoveDeletedDetectors();
        }

        public void RemoveDeletedDetectors() {
            // Guard against detectors that have been fully destroyed (Unity fake-null):
            // accessing .gameObject on a destroyed MonoBehaviour throws MissingReferenceException.
            var deleteList = detectors
                .Where(detector => detector == null || detector.gameObject.IsDeleted())
                .ToList();

            foreach (var detector in deleteList) {
                if (detector != null)
                    detector.RemoveHole(this);
                else
                    detectors.Remove(detector); // silently discard the dead reference
            }
        }

        public bool IsOccupiedBy(HoleDetector otherDetector) {
            return detectors.Contains(otherDetector);
        }
        
        public void AddDetector(HoleDetector newDetector) {
            if (IsOccupiedBy(newDetector) || IsOccupied) return;
            
            detectors.Add(newDetector);
            OnSetDetector?.Invoke(newDetector);
        }

        public void RemoveDetector(HoleDetector detector) {
            detectors.Remove(detector);
        }

        private void OnDisable() {
            if (!IsEmpty && gameObject.IsDeleted()) {
                foreach (var detector in detectors)
                    if (detector != null) // guard against destroyed detectors
                        detector.RemoveHole(this);
            }
        }
    }

    [Serializable]
    public class HoleData {

        public Mesh shape = null;
        public float depth;
        public Vector2 size;
        public GameObject part;

        public Vector3 position;
        public Quaternion rotation;
        public Vector3 forward;

        public bool IsHighStrength() => size == new Vector2(0.25f, 0.25f) && shape == HoleShapes.instance.GetShapeMesh("square");
    }
}