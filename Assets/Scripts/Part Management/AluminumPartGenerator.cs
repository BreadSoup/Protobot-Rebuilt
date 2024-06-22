using System.Collections;
using System.Collections.Generic;
using Protobot;
using UnityEngine;

namespace Protobot {
    public class AluminumPartGenerator : PartGenerator {
        [SerializeField] private List<string> param1Options;
        [SerializeField] private List<AluminumSubParts> subParts;
        
        private int HoleCount {
            get {
                if (int.TryParse(param2.value, out var val))
                    return val;
                
                return int.Parse(param2.customDefault);
            }
        }

        public override List<string> GetParam1Options() => param1Options;
        public override List<string> GetParam2Options() => new List<string>{" "};

        public override Mesh GetMesh() => subParts[param1Options.IndexOf(param1.value)].GetMesh(HoleCount);

        public override GameObject Generate(Vector3 position, Quaternion rotation) {
            var partObj = subParts[param1Options.IndexOf(param1.value)].GeneratePart(HoleCount);
            partObj.transform.position = position;
            partObj.transform.rotation = rotation;
            var partName = partObj.AddComponent<PartName>();
            //messy code that could probably been simplified but the general purpose is to assign the part name
            //since we're not able to do it in inspector for these parts and this is the
            //most plausable way that I (Rose) could figure out
            if(gameObject.name == "C-Channel")
            {
                partName.name = param1.value + " C-Channel " + "(" + HoleCount + ")";
            }else if(gameObject.name == "Angle")
            {
                partName.name = param1.value + " Angle " + "(" + HoleCount + ")";
            }else if(gameObject.name == "Rails")
            {
                partName.name = param1.value + " (" + HoleCount + ")";
            }
            else if(gameObject.name == "U-Channel")
            {
                partName.name = param1.value + " U-Channel " + "(" + HoleCount + ")";
            }

            RemoveDataScripts(partObj);
            SetId(partObj);
            
            return partObj;
        }
    }
}
