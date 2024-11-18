using System.Collections;
using System.Collections.Generic;
using Parts_List;
using UnityEngine;
using UnityEngine.Rendering;

namespace Protobot {
    public class PlatePartGenerator : PartGenerator {
        [SerializeField] private GameObject plateTemplate;
        [SerializeField] private HoleCollider plateHole;
        
        [SerializeField] private Material material;
        
        private List<string> EmptyList => new List<string>{" "};
        public override List<string> GetParam1Options() => EmptyList;

        public override List<string> GetParam2Options() => EmptyList;

        private int Length => int.Parse(param1.value);
        private int Width => int.Parse(param2.value);
        
        private int HoleCount => Length * Width;

        private float GetPos(int val, int max) => 0.5f * ((-max + 1) / 2f + val);

        public override Mesh GetMesh() {
            CombineInstance[] combine = new CombineInstance[HoleCount];

            var i = 0;
            for (var x = 0; x < Length; x++) {
                for (var y = 0; y < Width; y++) {
                    var meshFilter = plateTemplate.GetComponent<MeshFilter>();

                    combine[i].mesh = meshFilter.sharedMesh;

                    var tMatrix = meshFilter.transform.localToWorldMatrix;
                    tMatrix[0, 3] = GetPos(x, Length);
                    tMatrix[1, 3] = GetPos(y, Width);
                    tMatrix[2, 3] = 0; // resets Z pos
                    
                    combine[i].transform = tMatrix;

                    i++;
                }
            }
            
            var newMesh = new Mesh {
                indexFormat = IndexFormat.UInt32
            };
            
            newMesh.CombineMeshes(combine);
            newMesh.RecalculateNormals();

            return newMesh;
        }

        public override GameObject Generate(Vector3 position, Quaternion rotation) {
            var newPart = new GameObject("Plate (" + Length + "x" + Width + ")");
            
            Mesh mesh = GetMesh();
            newPart.AddComponent<MeshFilter>().sharedMesh = mesh;
            newPart.AddComponent<MeshCollider>().sharedMesh = mesh;
            
            newPart.AddComponent<MeshRenderer>().material = material;
            
            GenerateHoles(newPart);

            newPart.transform.position = position;
            newPart.transform.rotation = rotation;
            newPart.AddComponent<PartName>().name = newPart.name;
            
            RemoveDataScripts(newPart);
            SetId(newPart);
            
            return newPart;
        }
        
        private void GenerateHoles(GameObject obj) {
            for (var x = 0; x < Length; x++) {
                for (var y = 0; y < Width; y++) {
                    var pos = plateHole.transform.localPosition;
                    pos.x = GetPos(x, Length); // correctly positions hole
                    pos.y = GetPos(y, Width); // correctly positions hole

                    var rot = plateHole.transform.rotation;

                    Instantiate(plateHole.gameObject, pos, rot, obj.transform);
                }
            }
        }
    }
}
