using System;
using System.Collections.Generic;
using Protobot.Outlining;
using Protobot.StateSystems;
using UnityEngine;

namespace Protobot.SelectionSystem {
    public class GroupOutlineSelectionResponse : SelectionResponse {
        [SerializeField] public SelectionObjectLink selectionObjectLink;

        private List<GameObject> outlinedObjs = new();
        
        public override bool RespondOnlyToSelectors => false;

        public void SetSelectionDisplay() {
            if (selectionObjectLink.active) {
                SetDisplay(selectionObjectLink.obj);
            }
        }

        public void SetDisplay(GameObject obj) {
            var connectedObjs = obj.GetConnectedObjects(false);

            for (int i = 0; i < connectedObjs.Count; i++) {
                GameObject connectedObj = connectedObjs[i];

                connectedObj.EnableOutline(1);
                outlinedObjs.Add(connectedObj);
            }
        }

        public void ClearDisplay() {
            for (int i = 0; i < outlinedObjs.Count; i++) {
                outlinedObjs[i].DisableOutline();
            }
            outlinedObjs.Clear();
        }

        public override void OnSet(ISelection selection) {
            SetDisplay(selection.gameObject);
        }

        public override void OnClear(ClearInfo clearInfo) {
            ClearDisplay();
        }

        private void OnEnable() {
            StateSystem.OnChangeState += SetSelectionDisplay;
        }

        private void OnDisable() {
            StateSystem.OnChangeState -= SetSelectionDisplay;
        }
    }
}