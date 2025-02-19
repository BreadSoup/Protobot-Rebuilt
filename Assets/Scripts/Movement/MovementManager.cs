using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Protobot.Transformations;
using DG.Tweening;
using System;
using Protobot.StateSystems;

namespace Protobot {
    public class MovementManager : MonoBehaviour {
        [SerializeField] private ObjectLink objectLink;
        public GameObject MovingObj => objectLink.obj;

        private Pivot movementPivot;

        private Tween movementTween;

        private bool displaceContinous = false;

        public Action OnStartMoving;
        public Action OnFinishMoving;

        List<GameObject> movingObjs;


        private void Start() { 
            OnStartMoving += () => {
                ObjectElement.AddObjectElements(GetMovingConnectedObjs());
            };

            OnFinishMoving += () => {
                StateSystem.AddEmptyState();
                ObjectElement.AddObjectElements(GetMovingConnectedObjs());
            };

            movementPivot = Pivot.Create("Movement Pivot");
        }

        private void Update() {
            if (!movementPivot.IsEmpty && movementTween != null && !movementTween.active && !displaceContinous) {
                OnFinishMoving?.Invoke();
                movementPivot.SetEmpty();
            }
        }

        public List<GameObject> GetMovingConnectedObjs() => MovingObj.GetConnectedObjects(true);

        public void FormPivot(List<GameObject> objs) {
            movementPivot.SetEmpty();
            movementPivot.transform.position = MovingObj.transform.position;
            movementPivot.transform.rotation = MovingObj.transform.rotation;
            
            movementPivot.AddObjects(objs);
        }

        public void StopContinuous() {
            displaceContinous = false;
        }

        /// <summary>
        /// Prepares movementmanager by setting up all pivots and actions for the displacements to be running every frame
        /// </summary>
        /// <param name="value"></param>
        public void StartContinuous() {
            displaceContinous = true;

            if (displaceContinous) {
                movingObjs = GetMovingConnectedObjs();

                FormPivot(movingObjs);
                OnStartMoving?.Invoke();
            }
        }

        public void DisplaceTo(Displacement displacement, float duration = 0.25f) {
            if (!displaceContinous) {
                movingObjs = GetMovingConnectedObjs();
                OnStartMoving?.Invoke();
            }

            if (!displaceContinous) {
                FormPivot(movingObjs);
            }
            movementTween = displacement.Displace(movementPivot.gameObject, duration);

            Debug.Log(MovingObj.transform.position);
        }

        public void MoveTo(Vector3 position) {
            Debug.Log("Moved");
            var displacement = new Displacement(position, MovingObj.transform.rotation);
            DisplaceTo(displacement);
        }

        public void RotateTo(Quaternion rotation) {
            var displacement = new Displacement(MovingObj.transform.position, rotation);
            DisplaceTo(displacement);
        }
    }
}