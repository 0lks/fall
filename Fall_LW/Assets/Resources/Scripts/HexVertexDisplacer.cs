using System.Collections;
using UnityEngine;

public class HexVertexDisplacer : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;

    public void DisplaceVertices(Hex hex)
    {
        mesh = hex.GetComponentInChildren<MeshFilter>().mesh;
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

        CheckIfOccupied(hex);
    }

    private void CheckIfOccupied(Hex hex)
    {
        /*
        SphereCollider sc = hex.GetComponent<SphereCollider>();
        Vector3 groundPos = hex.GetComponentInChildren<MeshRenderer>().bounds.center;
        float dist = Vector3.Distance(hex.transform.position, groundPos);
        sc.center = new Vector3(sc.center.x, sc.center.y - dist, sc.center.z);
        */
        CapsuleCollider cc = hex.GetComponent<CapsuleCollider>();
        Rigidbody rb = hex.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        //cc.center = new Vector3(sc.center.x, sc.center.y + 3f, sc.center.z);
        cc.center = mesh.bounds.center;

        Destroy(cc);
        Destroy(rb);
    }
}
