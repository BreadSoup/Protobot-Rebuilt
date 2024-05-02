using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class InstanceCombiner : MonoBehaviour
{
#if UNITY_EDITOR
    // Source Meshes you want to combine
    [SerializeField] private List<MeshFilter> listMeshFilter;

    // Make a new mesh to be the target of the combine operation
    [SerializeField] private MeshFilter TargetMesh;

    [ContextMenu("Combine Meshes")]
    private void CombineMesh()
    {
        // Key: shared mesh instance ID, Value: arguments to combine meshes
        var helper = new Dictionary<int, List<CombineInstance>>();

        // Build combine instances for each type of mesh
        foreach (var m in listMeshFilter)
        {
            List<CombineInstance> tmp;
            if (!helper.TryGetValue(m.sharedMesh.GetInstanceID(), out tmp))
            {
                tmp = new List<CombineInstance>();
                helper.Add(m.sharedMesh.GetInstanceID(), tmp);
            }
            var ci = new CombineInstance();
            ci.mesh = m.sharedMesh;
            ci.transform = m.transform.localToWorldMatrix;
            tmp.Add(ci);
        }

        // Combine meshes and build combine instance for combined meshes
        var list = new List<CombineInstance>();
        foreach (var e in helper)
        {
            var m = new Mesh();
            m.CombineMeshes(e.Value.ToArray());
            var ci = new CombineInstance();
            ci.mesh = m;
            list.Add(ci);
        }

        // And now combine everything
        var result = new Mesh();
        result.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Add this line
        result.CombineMeshes(list.ToArray(), false, false);

        // It is a good idea to clean unused meshes now
        foreach (var m in list)
        {
            Destroy(m.mesh);
        }

        //Assign the target mesh to the mesh filter of the combination game object.
        TargetMesh.mesh = result;

        // Save The Mesh To Location
        SaveMesh(TargetMesh.sharedMesh, gameObject.name, false, true);

        // Print Results
        print($"<color=#20E7B0>Combine Meshes was Successful!</color>");
    }



    public static void SaveMesh(Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh)
    {
        string path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", name, "asset");
        if (string.IsNullOrEmpty(path)) return;

        path = FileUtil.GetProjectRelativePath(path);

        Mesh meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;

        if (optimizeMesh)
            MeshUtility.Optimize(meshToSave);

        AssetDatabase.CreateAsset(meshToSave, path);
        AssetDatabase.SaveAssets();
    }
    #endif
}
