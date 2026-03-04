using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Protobot {
    [Serializable]
    public class AluminumSubParts : MonoBehaviour {
        [SerializeField] private GameObject start;
        [SerializeField] private GameObject mid;
        [SerializeField] private GameObject mid5Start;
        [SerializeField] private GameObject mid5End;
        [SerializeField] private GameObject end;
        [SerializeField] private Material material;
        [SerializeField] private int maxHoleCount = 35;

        /// <summary>Public read-only access to maxHoleCount, used by AluminumResizeData.</summary>
        public int MaxHoleCount => maxHoleCount;

        public Mesh GetMesh(int holeCount) {
            CombineInstance[] combine = new CombineInstance[holeCount];

            for (int i = 0; i < holeCount; i++) {
                var subPart = GetSubPart(i + 1);
                var meshFilter = subPart.GetComponent<MeshFilter>();

                combine[i].mesh = meshFilter.sharedMesh;

                var tMatrix = meshFilter.transform.localToWorldMatrix;
                tMatrix[0, 3] = GetXPos(i, holeCount);
                tMatrix[1, 3] = 0; // resets Y pos
                tMatrix[2, 3] = 0; // resets Z pos

                combine[i].transform = tMatrix;
            }

            Mesh newMesh = new Mesh();
            newMesh.indexFormat = IndexFormat.UInt32;
            newMesh.CombineMeshes(combine);
            newMesh.RecalculateNormals();

            return newMesh;
        }

        public float GetXPos(int holeIndex, int holeCount) => 0.5f * ((-holeCount + 1) / 2f + holeIndex);

        public GameObject GeneratePart(int holeCount) {
            // make a new game object with a mesh from GetMesh(holeCount)
            // iterate thru all sub parts and add their holes with their LOCAL POSITIONS shifted by x values

            var newPart = new GameObject("Aluminum Part (" + holeCount + ")");
            Mesh mesh = GetMesh(holeCount);
            newPart.AddComponent<MeshFilter>().sharedMesh = mesh;
            newPart.AddComponent<MeshCollider>().sharedMesh = mesh;
            newPart.AddComponent<MeshRenderer>().material = material;
            
            GenerateHoles(newPart, holeCount);
            
            return newPart;
        }
        
        public void GenerateHoles(GameObject obj, int holeCount) {
            // iterate thru each subpart
            for (int i = 1; i <= holeCount; i++) {
                GameObject subPart = GetSubPart(i);

                foreach (var holeCollider in subPart.GetComponentsInChildren<HoleCollider>()) {
                    var pos = holeCollider.transform.localPosition;
                    pos.x += GetXPos(i - 1, holeCount); // correctly positions hole

                    var rot = holeCollider.transform.rotation;
                    
                    Instantiate(holeCollider.gameObject, pos, rot, obj.transform);
                }
            }
        }

        public GameObject GetSubPart(int curHole) {
            if (curHole == 1) 
                return start;
            
            if (curHole == maxHoleCount)
                return end;

            if (curHole % 5 == 0)
                return mid5Start;

            if ((curHole - 1) % 5 == 0)
                return mid5End;

            return mid;
        }
    }
}