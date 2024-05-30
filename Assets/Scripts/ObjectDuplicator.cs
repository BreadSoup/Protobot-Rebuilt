using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Protobot.StateSystems;
using Protobot.InputEvents;
using Protobot.Outlining;

namespace Protobot {
    public class ObjectDuplicator : MonoBehaviour {
        [SerializeField] private MovementManager movementManager;
        [SerializeField] private InputEvent input;

        private List<GameObject> prevDuplicatedObjs = new List<GameObject>();
        private bool duplicatedOnMove;

        public void Awake() {
            movementManager.OnStartMoving += () => {
                if (input.IsPressed) {
                    DuplicateObject();
                    
                    foreach (GameObject obj in prevDuplicatedObjs) {
                        ObjectElement objElement = new ObjectElement(obj);
                        objElement.existing = false;

                        StateSystem.AddElement(objElement);
                    }

                    duplicatedOnMove = true;
                }
            };

            StateSystem.OnChangeState += () => {
                if (duplicatedOnMove) {
                    ObjectElement.AddObjectElements(prevDuplicatedObjs);
                    duplicatedOnMove = false;
                }
            };
        }

        private void DuplicateObject() {
            if (movementManager.MovingObj != null) {
                var movingObjs = movementManager.MovingObj.GetConnectedObjects(true, true);

                prevDuplicatedObjs = new List<GameObject>();

                foreach (GameObject obj in movingObjs) {
                    GameObject clone = Instantiate(obj, obj.transform.position, obj.transform.rotation);
                    clone.DisableOutline();
                    if(clone.name != "MultiPivot(Clone)")
                    {
                        clone.GetComponent<Renderer>().material = new Material(obj.GetComponent<Renderer>().material);
                    }
                    prevDuplicatedObjs.Add(clone);
                }
            }
        }
    }
}
