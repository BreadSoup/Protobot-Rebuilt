using System;
using System.Collections;
using System.Collections.Generic;
using Protobot;
using UnityEngine;
using UnityEngine.UIElements;

public class ShaftPartGenerator : PartGenerator {
    [SerializeField] private GameObject normalShaftInch;
    [SerializeField] private GameObject hsShaftInch;
    
    private List<string> Types => new List<string>(){"Normal", "High Strength"};
    public override List<string> GetParam1Options() => Types;

    public override List<string> GetParam2Options() => new List<string>{" "};

    public override Mesh GetMesh() {
        Mesh refMesh;

        if (param1.value == "Normal") {
            refMesh = normalShaftInch.GetComponent<MeshFilter>().sharedMesh;
            param2.customLimits.y = 12;
        }
        else {
            refMesh = hsShaftInch.GetComponent<MeshFilter>().sharedMesh;
            param2.customLimits.y = 24;
        }

        Mesh mesh = new Mesh() {
            vertices = refMesh.vertices,
            triangles = refMesh.triangles,
            tangents = refMesh.tangents,
            normals = refMesh.normals
        };

        float zScale = float.Parse(param2.value);

        var vertices = new Vector3[mesh.vertexCount];

        for (int i = 0; i < mesh.vertexCount; i++) {
            var vert = mesh.vertices[i];
            vert.z *= zScale;

            vertices[i] = vert;
        }
        
        mesh.SetVertices(vertices);
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }

    public override GameObject Generate(Vector3 position, Quaternion rotation) {
        GameObject temp = normalShaftInch;
        if (param1.value == "High Strength") temp = hsShaftInch;
        
        Vector3 scale = temp.transform.localScale;
        scale.z = float.Parse(param2.value);
        temp.transform.localScale = scale;

        GameObject newObj = Instantiate(temp, position, rotation);
        string shaftType;
        if(param1.value == "High Strength") { shaftType = "in HS Shaft"; } else { shaftType = "in Shaft"; }
        newObj.AddComponent<PartName>().name = float.Parse(param2.value) + shaftType;
        
        SetId(newObj);
        RemoveDataScripts(newObj);

        return newObj;
    }
}
