using System;
using System.Collections.Generic;
using Parts_List;
using UnityEngine;
using System.Linq;

namespace Protobot {
    /// <summary>
    /// A type of Part Generator that involves a Part Type where all children's PartData can directly be considered a part on their own
    /// </summary>
    public class ChildPartGenerator : PartGenerator {
        public override GameObject Generate(Vector3 position, Quaternion rotation) {
            GameObject newObj = Instantiate(GetChildObj(), position, rotation);

            // Copy the generator's parameter names + the child's PartData values into
            // PartName so the Properties Menu can show them (e.g. "Size: 1/4in").
            // This must happen before RemoveDataScripts destroys the PartData component.
            if (newObj.TryGetComponent(out PartName pn) &&
                newObj.TryGetComponent(out PartData pd)) {
                if (UsesParams && !string.IsNullOrEmpty(param1.name)) {
                    pn.param1Label   = param1.name;
                    pn.param1Display = pd.param1Value;
                }
                if (UsesTwoParams && !string.IsNullOrEmpty(param2.name)) {
                    pn.param2Label   = param2.name;
                    pn.param2Display = pd.param2Value;
                }
            }

            RemoveDataScripts(newObj);
            SetId(newObj);

            // Attach ChildPartResizeData so the Properties Menu can offer a
            // size-change selector (◄ / ►) to swap this part for another variant.
            var crd = newObj.AddComponent<ChildPartResizeData>();
            crd.generator          = this;
            crd.currentParam1Value = param1.value;
            if (UsesTwoParams) crd.currentParam2Value = param2.value;

            return newObj;
        }

        public override Mesh GetMesh() => GetChildObj().GetComponent<MeshFilter>().sharedMesh;

        private GameObject GetChildObj() {
            var childParts = GetComponentsInChildren<PartData>();

            foreach (PartData partData in childParts) {
                if (partData.param1Value != param1.value) continue;
                
                if (!UsesTwoParams || (UsesTwoParams && partData.param2Value == param2.value))
                    return partData.gameObject;
            }

            throw new IndexOutOfRangeException("No matching parameters found in children for " + gameObject.name);
        }
        
        public override bool TryGetPartData(out PartData partData) {
            partData = GetChildObj().GetComponent<PartData>();
            return true;
        }

        public override List<string> GetParam1Options() {
            var uniqueOptions = new HashSet<string>();
            var childParts = GetComponentsInChildren<PartData>();

            foreach (var partData in childParts) {
                uniqueOptions.Add(partData.param1Value);
            }

            return uniqueOptions.ToList();
        }

        public override List<string> GetParam2Options() {
            var uniqueOptions = new HashSet<string>();
            var childParts = GetComponentsInChildren<PartData>();

            foreach (var partData in childParts) {
                if (partData.param1Value == param1.value)
                    uniqueOptions.Add(partData.param2Value);
            }

            return uniqueOptions.ToList();
        }
    }
}