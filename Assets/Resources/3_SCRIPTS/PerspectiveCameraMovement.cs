using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerspectiveCameraMovement : MonoBehaviour
{
    public float horizontalSpeed;
    public float verticalSpeed;
    public float rotationSpeed;
    private float cameraHeight;
    private float baseDist = 40f;
    public float cameraMaxDistance;
    public float dampFactor;
    public float zoomFactor;
    public float focusTargetHeightModifier;
    public float cameraFallSpeed;

    [HideInInspector]
    public Quaternion baseRot;
    Player player;

    [HideInInspector]
    public Character focusTarget = null;

    Vector3 mod =  new Vector3(0, 0, 0);
    Vector3 yVelocity = new Vector3(0, 0, 0);

    public void FocusOnCharacter(Character character)
    {
        focusTarget = character;
        mod.y = cameraHeight / focusTargetHeightModifier;

        // Set "CameraArm" as the parent
        transform.SetParent(focusTarget.transform.GetChild(2));

        Vector3 point = new Vector3(focusTarget.transform.position.x, focusTarget.transform.position.y + cameraHeight, focusTarget.transform.position.z);
        transform.LookAt(point);
        
        if ((point - transform.position).magnitude > baseDist)
        {
            while ((point - transform.position).magnitude > baseDist)
            {
                transform.position += this.transform.forward;
            }
        }
        else
        {
            if ((point - transform.position).magnitude < baseDist)
            {
                while ((point - transform.position).magnitude < baseDist)
                {
                    transform.position -= this.transform.forward;
                }
            }
        }
    }

    public void ExitFocus()
    {
        focusTarget = null;
        transform.parent = GameObject.FindGameObjectWithTag("Cameras").transform;
        cameraHeight = 50f;
    }

    private void Start()
    {
        player = GameControl.player;
        cameraHeight = 50f;
        baseRot = transform.rotation;
    }

    public void SetInitialPosition(Player player)
    {
        transform.position = player.transform.position + Vector3.up * 10f;
    }

    private void OnEnable()
    // Triggered by "SetActive(true)" on the parent gameobject
    {
        if (GameControl.map != null)
        {
            foreach (Hex hex in GameControl.map.GetAllHexes())
            {
                SphereCollider sc = hex.GetComponent<SphereCollider>();
                if (sc != null) sc.enabled = true;
            }
        }
    }
    private void OnDisable()
    // Triggered by "SetActive(false)" on the parent gameobject
    {
        if (GameControl.map != null)
        {
            foreach (Hex hex in GameControl.map.GetAllHexes())
            {
                SphereCollider sc = hex.GetComponent<SphereCollider>();
                if (sc != null) sc.enabled = false;
            }
        }
    }

    private void Update()
    {
        if (focusTarget == null)
        {
            float axisMovementH = Input.GetAxis("CameraHorizontal");
            if (axisMovementH != 0)
            {
                float x = transform.right.x * axisMovementH * horizontalSpeed * Time.deltaTime;
                float z = transform.right.z * axisMovementH * horizontalSpeed * Time.deltaTime;

                Vector3 newPos = transform.position + new Vector3(x, 0, z);

                Vector3 playerPos = player.transform.position;
                Vector3 vectorToPlayer = newPos - playerPos;
                vectorToPlayer = Vector3.ClampMagnitude(vectorToPlayer, cameraMaxDistance);
                transform.position = playerPos + vectorToPlayer;
            }
            
            float axisMovementV = Input.GetAxis("CameraVertical");
            if (axisMovementV != 0)
            {
                float x = transform.forward.x * axisMovementV * verticalSpeed * Time.deltaTime;
                float z = transform.forward.z * axisMovementV * verticalSpeed * Time.deltaTime;

                Vector3 newPos = transform.position + new Vector3(x, 0, z);

                Vector3 playerPos = player.transform.position;
                Vector3 vectorToPlayer = newPos - playerPos;
                vectorToPlayer = Vector3.ClampMagnitude(vectorToPlayer, cameraMaxDistance);
                transform.position = playerPos + vectorToPlayer;
            }

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            float axisUpDown = Input.GetAxis("CameraQE");

            if (scrollWheel != 0 || axisUpDown != 0)
            {
                if (cameraHeight >= 20 && cameraHeight <= 50)
                {
                    Vector3 vec;
                    if (axisUpDown != 0) vec = new Vector3(0, axisUpDown, 0);
                    else vec = new Vector3(0, scrollWheel * 25f, 0);

                    float newCamHeight;

                    if ((scrollWheel > 0 || axisUpDown < 0) && (cameraHeight <= 50) && (cameraHeight > 20))
                    {
                        newCamHeight = cameraHeight - vec.magnitude;
                    }
                    else
                    {
                        newCamHeight = cameraHeight + vec.magnitude;

                    }
                    if (!(cameraHeight == 50 && (scrollWheel < 0 || axisUpDown > 0)) &&
                        !(cameraHeight == 20 && (scrollWheel > 0 || axisUpDown < 0)))
                    {
                        transform.position += vec;
                        cameraHeight = newCamHeight;
                    }
                }

                // Needed because if you scroll fast enough it is possible to go over the bounds
                if (cameraHeight > 50) cameraHeight = 50;
                if (cameraHeight < 20) cameraHeight = 20;
            }

            if (Input.GetMouseButton(1))
            {
                Cursor.visible = false;
                transform.RotateAround(transform.position, Vector3.up, Input.GetAxis("Mouse X") * 2f);
                transform.RotateAround(transform.position, transform.right, -Input.GetAxis("Mouse Y") * 2f);
            }

            if (Input.GetMouseButtonUp(1))
            {
                Cursor.visible = true;
            }
        }

        else
        {
            float axisMovementRot = Input.GetAxis("CameraQE");
            if (axisMovementRot != 0)
            {
                transform.RotateAround(focusTarget.transform.position, focusTarget.currentPosition.transform.up, axisMovementRot * rotationSpeed * Time.deltaTime);
            }

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0)
            {
                if (scrollWheel < 0)
                {
                    if (cameraHeight < 20) return;
                    Vector3 newPos = transform.position + transform.forward.normalized * (cameraHeight * zoomFactor) * Time.deltaTime;
                    transform.position = newPos;

                    mod.y = cameraHeight / focusTargetHeightModifier;
                    cameraHeight *= cameraFallSpeed;
                }
                else
                {
                    if (cameraHeight >= 50) return;
                    Vector3 newPos = transform.position - transform.forward.normalized * (cameraHeight * zoomFactor) * Time.deltaTime;
                    transform.position = newPos;

                    mod.y = cameraHeight / focusTargetHeightModifier;
                    cameraHeight /= cameraFallSpeed;
                }
            }
        }

        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hitInfo;

        //if (Physics.Raycast(ray, out hitInfo, cameraHeight + 10f, 1 << 11 | 1 << 18)) // Alternative: enables peeking over cliffs, but can introduce fast camera falling when the player moves downhill
        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, 1 << 11 | 1 << 18))
        {
            Vector3 current;
            Vector3 target;
            current = transform.position;
            target = new Vector3(transform.position.x, hitInfo.point.y + cameraHeight, transform.position.z);

            if (current.magnitude != cameraHeight)
            {
                transform.position = Vector3.SmoothDamp(current, target, ref yVelocity, dampFactor);
            }
        }
        
        if (focusTarget != null)
        {
            Vector3 lookattarget = focusTarget.transform.position + mod;
            transform.LookAt(lookattarget);
        }
    }
}

