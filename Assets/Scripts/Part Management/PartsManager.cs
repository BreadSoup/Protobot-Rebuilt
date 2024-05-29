using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Protobot {
    public static class PartsManager {
        public static PartType[] partTypes;

        [RuntimeInitializeOnLoadMethod]
        public static void LoadPartTypes() {
            partTypes = Resources.LoadAll("Part Prefabs").Select(p => (p as GameObject)?.GetComponent<PartType>()).ToArray();
            foreach (var p in partTypes)
                p.GetComponent<PartGenerator>().InitParamValues();
        }

        public static PartType GetPartType(string id) {
            var idSplit = id.Split('-');
            var typeId = idSplit[0];
            return partTypes.FirstOrDefault(partType => partType.id == typeId);
        }
        
        public static GameObject GeneratePart(string id, Vector3 pos, Quaternion rot) {
            var idSplit = id.Split('-');
            var typeId = idSplit[0];

            var p1Val = "";
            var p2Val = "";

            if (idSplit.Length >= 2) p1Val = idSplit[1];
            if (idSplit.Length == 3) p2Val = idSplit[2];
    
            if (id == "NUT") { //VERY TEMPORARY ONLY FOR BETA 1.3.1 PLEASE FIX WITH VERSION CONTROL
                p1Val = "Lock";
            }

            GameObject partObj = null;
            
            foreach (var partType in partTypes) {
                if (partType.id == typeId) {
                    var gen = partType.GetComponent<PartGenerator>();
                    
                    Parameter p1 = gen.param1;
                    Parameter p2 = gen.param2;

                    gen.param1.value = p1Val;
                    gen.param2.value = p2Val;
                    
                    //TODO This is awful and should be compared against an array in another file/method to make sure legacy files work but this is a quick fix
                    if (gen.name == "Omni Wheel" || gen.name == "Traction Wheel")
                    {
                        if (p2Val == "")
                        {
                            p2.value = p1Val;
                            p1.value = "V1";
                        }
                    }
                    if (gen.name == "Motor" && p1Val == "")
                    {
                        p1.value = "11W";
                    }
                    if (gen.name == "Block Bearing" && p1Val == "")
                    {
                        p1.value = "Normal";
                    }
                    if (gen.name == "Cylinder" && p2Val == "")
                    {
                        p2.value = "Normal";
                    }

                    partObj = gen.Generate(pos, rot);

                    gen.param1.value = p1.value;
                    gen.param2.value = p2.value;
                }
            }

            return partObj;
        }

        /// <Summary> Returns a list of all loaded parts in the current scene </Summary>
        public static List<GameObject> FindLoadedObjects() {
            return GameObject.FindObjectsOfType<SavedObject>().Select(x => x.gameObject).ToList();
        }

        /// <Summary> Destroys all loaded parts in the current scene </Summary>
        public static void DestroyLoadedObjects() {
            foreach (var obj in FindLoadedObjects())
                GameObject.Destroy(obj);
        }
    }
}
