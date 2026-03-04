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

        // ── Mesh generation ──────────────────────────────────────────────────────

        /// <summary>Builds a combined mesh using the current param1/param2 values.</summary>
        public override Mesh GetMesh() => GetMesh(Length, Width);

        /// <summary>
        /// Builds a combined plate mesh for the given dimensions.
        /// Used by <see cref="PlateResizer"/> during runtime resizing so the
        /// generator's param fields do not need to be mutated.
        /// </summary>
        public Mesh GetMesh(int length, int width) {
            CombineInstance[] combine = new CombineInstance[length * width];

            var i = 0;
            for (var x = 0; x < length; x++) {
                for (var y = 0; y < width; y++) {
                    var meshFilter = plateTemplate.GetComponent<MeshFilter>();

                    combine[i].mesh = meshFilter.sharedMesh;

                    var tMatrix = meshFilter.transform.localToWorldMatrix;
                    tMatrix[0, 3] = GetPos(x, length);
                    tMatrix[1, 3] = GetPos(y, width);
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

        // ── Part generation ──────────────────────────────────────────────────────

        public override GameObject Generate(Vector3 position, Quaternion rotation) {
            var newPart = new GameObject("Plate (" + Length + "x" + Width + ")");

            Mesh mesh = GetMesh();
            newPart.AddComponent<MeshFilter>().sharedMesh = mesh;
            newPart.AddComponent<MeshCollider>().sharedMesh = mesh;

            newPart.AddComponent<MeshRenderer>().material = material;

            GenerateHoles(newPart);

            newPart.transform.position = position;
            newPart.transform.rotation = rotation;

            var partList = newPart.AddComponent<PartName>();

            partList.name = newPart.name;
            partList.weightInGrams = Length * Width * .53f;
            partList.param1Label   = "Length";
            partList.param1Display = Length + " holes";
            partList.param2Label   = "Width";
            partList.param2Display = Width + " holes";

            // ── Attach PlateResizeData for runtime resizing ───────────────────
            var prd = newPart.AddComponent<PlateResizeData>();
            prd.generator    = this;
            prd.length       = Length;
            prd.width        = Width;
            prd.weightPerHole = 0.53f;

            RemoveDataScripts(newPart);
            SetId(newPart);

            return newPart;
        }

        // ── Hole generation ──────────────────────────────────────────────────────

        /// <summary>Generates holes using the current param1/param2 values.</summary>
        private void GenerateHoles(GameObject obj) => GenerateHoles(obj, Length, Width);

        /// <summary>
        /// Instantiates hole colliders at the correct grid positions for the
        /// given dimensions.  The caller must ensure <paramref name="obj"/>'s
        /// transform is at world origin before calling (world pos == local pos).
        /// Used by <see cref="PlateResizer"/> during runtime resizing.
        /// </summary>
        public void GenerateHoles(GameObject obj, int length, int width) {
            for (var x = 0; x < length; x++) {
                for (var y = 0; y < width; y++) {
                    var pos = plateHole.transform.localPosition;
                    pos.x = GetPos(x, length);
                    pos.y = GetPos(y, width);

                    var rot = plateHole.transform.rotation;

                    Instantiate(plateHole.gameObject, pos, rot, obj.transform);
                }
            }
        }
    }
}
