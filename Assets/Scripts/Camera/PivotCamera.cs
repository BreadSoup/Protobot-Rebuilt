using UnityEngine;
using DG.Tweening;

namespace Protobot {
    public class PivotCamera : MonoBehaviour {
        public static PivotCamera Main;
        [SerializeField] public new Camera camera;

        // Moving values
        public bool moving => DOTween.IsTweening(transform);

        private Tweener orbitTween;
        public bool orbiting => orbitTween != null ? orbitTween.active : false;

        private Tweener zoomTween;
        public bool zooming => zoomTween != null ? zoomTween.active : false;

        public Vector3 cameraPosition => camera.transform.localPosition;
        public Vector3 focusPosition => transform.position;
        public float focusDistance => cameraPosition.z;
        public Vector3 lookAngle => transform.eulerAngles;
        public Vector3 orbitRot;

        private Vector3 panPos = Vector3.zero;

        [CacheComponent] private ProjectionSwitcher projectionSwitcher;
        private IPivotCameraInput[] inputs;
        private float zoomDistance;
        public bool invertZoom = false;
        public float snapSensitivity;

        // New smoothing toggle
        public bool smoothing = false;

        void Start() {
            orbitRot = transform.eulerAngles;

            var mainCams = GameObject.FindGameObjectsWithTag("MainCamera");

            foreach (GameObject cam in mainCams) {
                if (cam.GetComponent<PivotCamera>() != null) {
                    Main = cam.GetComponent<PivotCamera>();
                    break;
                }
            }

            if (Main == null) Debug.LogError("MAIN PIVOT CAMERA REFERENCE MISSING");

            SetupInputs();

            zoomDistance = -cameraPosition.z;
        }

        void Update() {
            Debug.Log("Smoothing is now " + (smoothing ? "Enabled" : "Disabled"));
            if (projectionSwitcher != null && projectionSwitcher.isOrtho) {
                camera.orthographicSize = -cameraPosition.z / 2;
                OrbitSnap();
            }
        }

        public void SetupInputs() {
            inputs = GetComponents<IPivotCameraInput>();

            foreach (IPivotCameraInput input in inputs) {
                input.updateOrbit += OrbitControl;
                input.updatePan += PanControl;
                input.updateZoom += ZoomControl;
            }
        }

        public void SetTransform(Vector3 newPos, Vector3 newAngle, float newDistance) {
            transform.position = newPos;

            transform.eulerAngles = newAngle;
            orbitRot = transform.eulerAngles;

            Vector3 newCamPos = camera.transform.localPosition;
            newCamPos.z = newDistance;
            zoomDistance = -newDistance;

            camera.transform.localPosition = newCamPos;

            panPos = transform.position;
        }

        public void MoveFocusPosition(Vector3 newFocusPos) {
            panPos = newFocusPos;
            if (smoothing)
                transform.DOMove(newFocusPos, 0.4f); // Use tween if smoothing is enabled
            else
                transform.position = newFocusPos; // Instant move if smoothing is disabled
        }

        // ZOOM
        public void ZoomControl(float input) {
            input *= (zoomDistance * 0.3f);

            int inverted = 1;
            if (invertZoom) inverted = -1;

            zoomDistance += -input * inverted;
            zoomDistance = Mathf.Clamp(zoomDistance, 0.5f, 1000f);

            Vector3 newPos = cameraPosition;
            newPos.z = -zoomDistance;

            // Use tweening for smooth zoom if smoothing is enabled
            if (smoothing)
                zoomTween = camera.transform.DOLocalMove(newPos, 0.4f);
            else
                camera.transform.localPosition = newPos; // Instant zoom if smoothing is disabled
        }

        public void SetInvertZoom(bool value) {
            invertZoom = value;
        }

        public void SetSmoothing(bool value) {
            smoothing = value;
        }

        // ORBIT
        public void OrbitControl(Vector2 orbitValue) {
            orbitRot += new Vector3(orbitValue.x, orbitValue.y, 0);

            // Use tweening for smooth orbit if smoothing is enabled
            if (smoothing)
                orbitTween = transform.DORotateQuaternion(Quaternion.Euler(orbitRot), 0.4f);
            else
                transform.eulerAngles = orbitRot; // Instant orbit if smoothing is disabled
        }

        public void OrbitSnap() {
            Vector3 snapVector = lookAngle.Round(90);
            if (Vector3.Distance(snapVector, lookAngle) < snapSensitivity && !moving)
                transform.DOLocalRotate(snapVector, 0.25f);
        }

        // PAN
        public void PanControl(Vector2 panValue) {
            float zoomFactor = zoomDistance / 5; // makes panning adjust by zoom distance, closer zoom should be less pan distance covered
            panPos += ((transform.right * -panValue.x) + (transform.up * -panValue.y)) * zoomFactor;

            // Use tweening for smooth pan if smoothing is enabled
            if (smoothing)
                transform.DOMove(panPos, 0.25f);
            else
                transform.position = panPos; // Instant pan if smoothing is disabled
        }
    }
}
