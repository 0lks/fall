using UnityEngine;

public class CapsuleCastHelper : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 3f);
    }
}
