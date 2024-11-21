using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

using Protobot.StateSystems;
using Protobot.InputEvents;
using Protobot.Transformations;
using UnityEngine.InputSystem;

namespace Protobot {
    public class Placement : MonoBehaviour { 
        public static int PLACEMENT_LAYER => 14;

        [SerializeField] private MovementManager movementManager;

        public bool placing;
        public bool rotating;

        private PlacementData currentPlacementData;

        [CacheComponent] private MeshRenderer meshRenderer;

        [CacheComponent] private MeshFilter meshFilter;

        [SerializeField] private List<PlaceDisplacement> placePositions;

        [Space(5)]

        [SerializeField] private InputEvent placeInput;
        [SerializeField] private InputEvent toggleRotateInput;
        [SerializeField] private InputEvent stopPlacingInput;
        [SerializeField] private InputEvent dupInput;

        [SerializeField] private UnityEvent OnStartPlacing;
        [SerializeField] private UnityEvent OnStopPlacing;
        [SerializeField] private UnityEvent OnStartRotate;
        [SerializeField] private UnityEvent OnStopRotate;

        private Displacement currentDisplacement;

        [SerializeField] private Camera refCamera = null; //used for dot product angle comparison

        private Vector3 rotVector => transform.forward;

        private Vector3 initRotVector; //the initial Vector to rotate about

        private Vector3 initMouseVector; //the initial Vector between the mouse position and the selection position
        private Vector3 MouseVector => (MouseInput.Position - refCamera.WorldToScreenPoint(transform.position)).normalized;
        private Quaternion initRot; //the intiial rotation of the selection
        
        private void Awake() {
            placing = false;
            rotating = false;
            
            placeInput.performed += () => {
                if (placing && !MouseInput.overUI)
                    Place();
            };

            toggleRotateInput.performed += () => {
                InitRotation();
                if (placing && !MouseInput.overUI) {
                    rotating = !rotating;

                    if (rotating) OnStartRotate?.Invoke();
                    else OnStopRotate?.Invoke();
                }
            };

            /*dupInput.performed += () => {
                if (currentPlacementData != null && placing) {
                    /*if (currentPlacementData.TryParse(out GameObjectPlacementData objPlaceData)) {
                        SetPlacementLayer(objPlaceData.GetGameObject(), false);

                    }#1#
                }
            };

            dupInput.canceled += () => {
                if (currentPlacementData != null && placing) {
                    if (currentPlacementData.TryParse(out GameObjectPlacementData objPlaceData)) {
                        objPlaceData.GetGameObject().GetConnectedObjects(true).ForEach(x => x.SetActive(false));
                    }
                }
            };*/

            stopPlacingInput.performed += () => {
                if (placing)
                    StopPlacing();
            };

            currentDisplacement = new Displacement(transform);
        }

        void Update() {
            if (placing) {
                if (rotating) {
                    /*
                    if (Keyboard.current.xKey.wasPressedThisFrame) {
                        initRotVector = transform.right;
                        initRot = transform.rotation;
                    }
                    */

                    /*
                    if (Keyboard.current.zKey.wasPressedThisFrame) {
                        initRotVector = transform.forward;
                        initRot = transform.rotation;
                    }
                    */

                    /*
                    if (Keyboard.current.yKey.wasPressedThisFrame) {
                        initRotVector = transform.up;
                        initRot = transform.rotation;
                    }*/
                    
                    Rotate();
                }
                else {
                    foreach (PlaceDisplacement placePosition in placePositions) {
                        if (placePosition.TryGetDisplacement(currentPlacementData, out Displacement displacement)) {
                            var newDisplacement = displacement;
                            if (!placePosition.ModifyRotation) {
                                newDisplacement = new Displacement(displacement.translation.Position,
                                                                   currentDisplacement.rotation.Orientation);
                            }

                            currentDisplacement = newDisplacement;
                            newDisplacement.Displace(gameObject);
                            break;
                        }
                    }
                }
            }

            meshRenderer.enabled = placing;
        }

        public void StartPlacing(PlacementData newPlacementData) {
            currentPlacementData = newPlacementData;
            placing = true;
            rotating = false;
            meshFilter.mesh = newPlacementData.GetDisplayMesh();

            if (currentPlacementData.TryParse(out GameObjectPlacementData objPlaceData)) {
                var objTransform = objPlaceData.GetGameObject().transform;
                transform.rotation = objTransform.rotation;
                transform.position = objTransform.position;
                currentDisplacement = new Displacement(transform);
                
                SetPlacementLayer(objPlaceData.GetGameObject(), true);
            }

            OnStartPlacing?.Invoke();
        }

        public void SetPlacementLayer(GameObject obj, bool value) {
            obj.GetConnectedObjects(true).ForEach(x => {
                if (value && x.layer == 0)
                    x.layer = Placement.PLACEMENT_LAYER;
                
                if (!value && x.layer == PLACEMENT_LAYER)
                    x.layer = 0;
            });
        }

        IEnumerator WaitStopPlacing() {
            yield return new WaitForEndOfFrame();
            OnStopPlacing?.Invoke();
            placing = false;
        }

        public void StopPlacing() {
            if (currentPlacementData.TryParse(out GameObjectPlacementData objPlaceData))
                SetPlacementLayer(objPlaceData.GetGameObject(), false);
            
            transform.rotation = Quaternion.identity;
            OnStopRotate?.Invoke();

            StartCoroutine(WaitStopPlacing());
        }

        private void Place() {
            bool isPartPlacement = currentPlacementData.GetType() == typeof(PartPlacementData);
            bool isGameObjectPlacement = currentPlacementData.GetType() == typeof(GameObjectPlacementData);

            var displaceRot = currentDisplacement.rotation.Orientation;
            var displacePos = currentDisplacement.translation.Position;
            
            if (isPartPlacement) {
                var generator = ((PartPlacementData)currentPlacementData).partGenerator;
                GameObject placedObj = generator.Generate(displacePos, displaceRot);

                // Call OutputPartsList to update the weight display after placing a part
                PartListOutput partListOutput = FindObjectOfType<PartListOutput>();
                if (partListOutput != null) {
                    partListOutput.CalculatePartsList(); // Recalculate and notify weight update
                }

                ObjectElement prevElement = new ObjectElement(placedObj);
                prevElement.existing = false;
                StateSystem.AddElement(prevElement);

                ObjectElement objElement = new ObjectElement(placedObj);
                StateSystem.AddState(objElement);
            }
            else if (isGameObjectPlacement) {
                movementManager.DisplaceTo(currentDisplacement, 0);
                StopPlacing();
            }

            rotating = false;
            OnStopRotate?.Invoke();
        }
        public void InitRotation() {
            initMouseVector = MouseVector;

            initRot = transform.rotation;
            initRotVector = transform.forward;
        }

        public void Rotate() {
            float angle = Vector2.SignedAngle(MouseVector, initMouseVector);
            angle = Mathf.Round(angle * 1 / 22.5f) * 22.5f; //force values to round at 22.5 degree increments
            float camDot = Vector3.Dot(refCamera.transform.forward, initRotVector); //finds the dot product between the camera direction vs. the look direction of this rotate ring
            Quaternion newRot = Quaternion.AngleAxis(angle, -initRotVector * Mathf.Sign(camDot)) * initRot;

            Rotation rot = new Rotation(newRot);
            currentDisplacement = new Displacement(currentDisplacement.translation.Position, newRot);
            rot.Rotate(gameObject);
        }
    }
}