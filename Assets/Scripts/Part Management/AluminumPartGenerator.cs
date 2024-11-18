using System.Collections;
using System.Collections.Generic;
using Parts_List;
using Protobot;
using TMPro;
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
            //This check should *NEVER* return true but somehow Michael was able to create a 2x35x24 which triggers this????
            if (param1Options.IndexOf(param1.value) == -1)
            {
                print(param1.value);
                print(HoleCount);
                print("WHJAT");
                if (GameObject.Find("Error") == null)
                {
                    var notfication = new GameObject();
                    notfication.gameObject.name = "Error";
                    var text = notfication.AddComponent<TextMeshPro>();
                    text.text =
                        "Please send this save to @breadsoup on discord or breadsoup64@gmail.com as it contains an error I cannot replicate " +
                        "This should never be seen under normal circumstances. To remove click on the red text and press delete.";
                    text.color = Color.red;
                    text.rectTransform.sizeDelta = new Vector2(187, 5);
                    notfication.AddComponent<SavedObject>().id = "Error";
                    //little jank method to delete the text but it works
                    var boxCollider = notfication.AddComponent<BoxCollider>();
                    boxCollider.size = new Vector2(187, 10);
                }
                return null;
            }
            var partObj = subParts[param1Options.IndexOf(param1.value)].GeneratePart(HoleCount);
            partObj.transform.position = position;
            partObj.transform.rotation = rotation;
            var partName = partObj.AddComponent<PartName>();
            //messy code that could probably been simplified but the general purpose is to assign the part name
            //since we're not able to do it in inspector for these parts and this is the
            //most plausable way that I (Rose) could figure out
            if(gameObject.name == "C-Channel")
            {
                partName.name = param1.value + "x1 C-Channel " + "(" + HoleCount + ")";
            }else if(gameObject.name == "Angle")
            {
                partName.name = param1.value + " Angle " + "(" + HoleCount + ")";
            }else if(gameObject.name == "Rails")
            {
                partName.name = param1.value + " (" + HoleCount + ")";
            }
            else if(gameObject.name == "U-Channel")
            {
                partName.name = param1.value + "x2 U-Channel " + "(" + HoleCount + ")";
            }

            RemoveDataScripts(partObj);
            SetId(partObj);
            
            return partObj;
        }
    }
}
