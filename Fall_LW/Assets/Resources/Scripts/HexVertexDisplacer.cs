using System.Collections;
using UnityEngine;

using FALL.Core;

public class HexVertexDisplacer : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;

    public void DisplaceVertices(Hex hex)
    {
        ResetMeshVertices(hex);
        mesh = hex.mesh;
        vertices = mesh.vertices;
        RaycastHit hitInfo;

        for (var i = 0; i < vertices.Length; i++)
        {
            Ray ray = new Ray(hex.transform.position + vertices[i], Vector3.down);
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, 1 << 11 | 1 << 18))
            {
                vertices[i] += Vector3.down * hitInfo.distance + new Vector3(0, 1, 0);
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();

        // Initialize variables inside this hex for use elsewhere
        hex.mesh = mesh;
        hex.originalMesh = mesh.vertices;

        Vector3 max = hex.GetComponentInChildren<MeshRenderer>().bounds.max;
        Vector3 min = hex.GetComponentInChildren<MeshRenderer>().bounds.min;
        if (Vector3.Distance(max, min) > 18.5f)
        {
            hex.DeleteHex();
            Debug.Log("Discarded hex " + hex.id + " because the ground below it is too uneven");
            return;
        }

        hex.CapsuleTest();
    }
    
    public void ResetMeshVertices(Hex hex)
    {
        if (hex.originalMesh == null) return;
        else
        {
            hex.meshFilter.sharedMesh.vertices = GameControl.hexPrefab.GetComponentInChildren<MeshFilter>().sharedMesh.vertices;
        }
    }
}
