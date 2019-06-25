using UnityEngine;

public class FixedRotation : MonoBehaviour
// This script is attached to the CameraArm gameobject on the player
{
    Quaternion rotation;
    void Awake()
    {
        rotation = transform.rotation;
    }
    void LateUpdate()
    {
        transform.rotation = rotation;
    }
}
