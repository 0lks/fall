using UnityEngine;

public class OrthographicCameraMovement : MonoBehaviour {
    public float horizontalSpeed;
    public float verticalSpeed;
    private Character focusTarget;
    public GameObject jumpTarget;
    private float baseOrthoDistance;
    public float cameraHeight;

    private void Awake()
    {
        baseOrthoDistance = transform.GetComponent<Camera>().orthographicSize;
    }

    private void Start()
    {
        if (jumpTarget == null) jumpTarget = GameControl.player.gameObject;
        jumpToTarget();
    }

    private void Update()
    {
        transform.GetComponent<Camera>().orthographicSize = baseOrthoDistance + GameControl.orthoDistancePlus;
        float axisMovementH = Input.GetAxis("CameraHorizontal");
        if (axisMovementH != 0)
        {
            float x = transform.right.x * axisMovementH * horizontalSpeed * Time.deltaTime;
            transform.position += new Vector3(x, 0, 0);
        }

        float axisMovementV = Input.GetAxis("CameraVertical");
        if (axisMovementV != 0)
        {
            float z = transform.up.z * axisMovementV * verticalSpeed * Time.deltaTime;
            transform.position += new Vector3(0, 0, z);
        }
    }

    // Debugging scripts
    public void jumpToTarget()
    // Target provided via inspector.
    {
        Vector3 jumpTargetT = jumpTarget.transform.position;
        transform.position = new Vector3(jumpTargetT.x, cameraHeight, jumpTargetT.z);
        jumpTarget = null;
    }
    
    public void jumpToTarget(GameObject target)
    {
        transform.position = new Vector3(target.transform.position.x, cameraHeight, target.transform.position.z);
    }
}
