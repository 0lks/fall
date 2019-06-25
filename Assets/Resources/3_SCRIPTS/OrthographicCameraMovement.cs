using UnityEngine;

public class OrthographicCameraMovement : MonoBehaviour {
    public float horizontalSpeed;
    public float verticalSpeed;
    private Character focusTarget;
    public GameObject jumpTarget;
    private float baseOrthoDistance;

    private void Awake()
    {
        baseOrthoDistance = transform.GetComponent<Camera>().orthographicSize;
    }

    private void Start()
    {
        if (jumpTarget == null) jumpTarget = GameControl.player.gameObject;
        jumpToTarget();
    }

    public void SetInitialPosition(Player player)
    {
        float y = transform.position.y;
        transform.position = new Vector3(player.transform.position.x, y, player.transform.position.z);
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
    {
        Vector3 jumpTargetT = jumpTarget.transform.position;
        float map_y = GameObject.FindGameObjectWithTag("Map").transform.position.y;
        transform.position = new Vector3(jumpTargetT.x, map_y + 50, jumpTargetT.z);
        jumpTarget = null;
    }
}
