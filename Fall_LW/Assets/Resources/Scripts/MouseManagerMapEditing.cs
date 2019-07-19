using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseManagerMapEditing : MouseManager
{
    private float hexWidth;
    private float hexHalfWidth;
    private float hexHeight;
    private float hexHalfHeight;

    // Shift vectors
    Vector3 northeast;
    Vector3 east;
    Vector3 southeast;
    Vector3 southwest;
    Vector3 west;
    Vector3 northwest;

    private void Awake()
    {
        enabled = false;

        MeshRenderer meshRenderer = GameControl.hexPrefab.GetComponentInChildren<MeshRenderer>();
        hexWidth = meshRenderer.bounds.size.x;
        hexHalfWidth = hexWidth / 2;
        hexHeight = meshRenderer.bounds.size.z;
        hexHalfHeight = hexHeight / 2;

        northeast = new Vector3(hexHalfWidth, 0, 1.5f * hexHalfHeight);
        east = new Vector3(hexWidth, 0, 0);
        southeast = new Vector3(hexHalfWidth, 0, -1.5f * hexHalfHeight);
        southwest = new Vector3(-hexHalfWidth, 0, -1.5f * hexHalfHeight);
        west = new Vector3(-hexWidth, 0, 0);
        northwest = new Vector3(-hexHalfWidth, 0, 1.5f * hexHalfHeight);
    }

    private void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Input.GetMouseButtonDown(0) | Input.GetMouseButton(0) | Input.GetMouseButtonDown(1) | Input.GetMouseButton(1))
        {
            Ray ray = GameControl.orthoCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo) && GUIUtility.hotControl == 0)
            {
                GameObject hitObject = hitInfo.transform.gameObject;

                if (hitObject.tag == "Hex")
                {
                    if (Input.GetMouseButtonDown(0) | Input.GetMouseButton(0))
                    {
                        if (hitInfo.collider.GetType() == typeof(SphereCollider))
                        {
                            if (hitInfo.collider.transform.parent.GetComponent<Hex>() != null)
                            {
                                //string colliderDirection = DetermineDirection(hitInfo.collider);
                                Hex.Direction colliderDirection = DetermineDirection(hitInfo.collider);
                                BuildHex(colliderDirection, hitInfo.transform.parent.gameObject);
                            }
                        }
                    }
                    if (Input.GetMouseButtonDown(1) | Input.GetMouseButton(1))
                    {
                        if (hitInfo.collider.tag == "Hex")
                        {
                            if (hitObject.GetComponent<Hex>() != null)
                            {
                                RightClickHex(hitObject);
                            }
                        }
                    }
                }
            }
        }
    }

    void RightClickHex(GameObject hitObject)
    {
        Hex hitHex = hitObject.GetComponent<Hex>();
        hitHex.DeleteHex();
    }

    Hex.Direction DetermineDirection(Collider collider)
    {
        // Read in the collider data
        SphereCollider sphereCollider = (SphereCollider)collider;
        Vector3 center = sphereCollider.center;
        float x = center.x;
        float z = center.z;

        // Use that data to determine which direction the collider represents
        if (x > 0 & z > 0) return Hex.Direction.NE;
        else if (x > 0 & z == 0) return Hex.Direction.E;
        else if (x > 0 & z < 0) return Hex.Direction.SE;
        else if (x < 0 & z < 0) return Hex.Direction.SW;
        else if (x < 0 & z == 0) return Hex.Direction.W;
        else if (x < 0 & z > 0) return Hex.Direction.NW;
        else throw new System.Exception("Could not determine the direction of the collider.");
    }
    /*
    void BuildHex(Hex.Direction direction, GameObject origin)
    {
        Hex originHex = origin.GetComponent<Hex>();
        Vector3 originPosition = origin.transform.position;
        int originX = originHex.x;
        int originY = originHex.y;
        int originZ = originHex.z;

        GameObject newHex = Instantiate(GameControl.hexPrefab, originHex.transform.position, Quaternion.identity, GameControl.map.transform.Find("Hexes"));
        Hex newHexHex = newHex.GetComponent<Hex>();

        if (direction == Hex.Direction.NE)
        {
            newHexHex.x = originX - 1;
            newHexHex.y = originY + 1;
            newHexHex.z = originZ;
            newHexHex.transform.position = originPosition + northeast;
        }
        else if (direction == Hex.Direction.E)
        {
            newHexHex.x = originX;
            newHexHex.y = originY + 1;
            newHexHex.z = originZ + 1;
            newHex.transform.position = originPosition + east;
        }
        else if (direction == Hex.Direction.SE)
        {
            newHexHex.x = originX + 1;
            newHexHex.y = originY;
            newHexHex.z = originZ + 1;
            newHex.transform.position = originPosition + southeast;
        }
        else if (direction == Hex.Direction.SW)
        {
            newHexHex.x = originX + 1;
            newHexHex.y = originY - 1;
            newHexHex.z = originZ;
            newHex.transform.position = originHex.transform.position + southwest;
        }
        else if (direction == Hex.Direction.W)
        {
            newHexHex.x = originX;
            newHexHex.y = originY - 1;
            newHexHex.z = originZ - 1;
            newHex.transform.position = originHex.transform.position + west;
        }
        else if (direction == Hex.Direction.NW)
        {
            newHexHex.x = originX - 1;
            newHexHex.y = originY;
            newHexHex.z = originZ - 1;
            newHex.transform.position = originHex.transform.position + northwest;
        }

        newHexHex.setId();
        newHexHex.name = newHexHex.id;

        GameControl.map.AddHexToMap(newHexHex);
        newHexHex.DisableOverlappingColliders();
        newHexHex.Highlight();
        GameControl.map.vertexDisplacer.DisplaceVertices(newHexHex);
    }
    */
    void BuildHex(Hex.Direction direction, GameObject origin)
    {
        Hex originHex = origin.GetComponent<Hex>();
        Vector3 originPosition = origin.transform.position;
        int originX = originHex.x;
        int originY = originHex.y;
        int originZ = originHex.z;

        GameObject newHex = Instantiate(GameControl.hexPrefab, originHex.transform.position, Quaternion.identity, GameControl.map.transform.Find("Hexes"));
        Hex newHexHex = newHex.GetComponent<Hex>();

        if (direction == Hex.Direction.NE)
        {
            newHexHex.x = originX + 1;
            newHexHex.y = originY;
            newHexHex.z = originZ - 1;
            newHexHex.transform.position = originPosition + northeast;
        }
        else if (direction == Hex.Direction.E)
        {
            newHexHex.x = originX + 1;
            newHexHex.y = originY - 1;
            newHexHex.z = originZ;
            newHex.transform.position = originPosition + east;
        }
        else if (direction == Hex.Direction.SE)
        {
            newHexHex.x = originX;
            newHexHex.y = originY - 1;
            newHexHex.z = originZ + 1;
            newHex.transform.position = originPosition + southeast;
        }
        else if (direction == Hex.Direction.SW)
        {
            newHexHex.x = originX - 1;
            newHexHex.y = originY;
            newHexHex.z = originZ + 1;
            newHex.transform.position = originHex.transform.position + southwest;
        }
        else if (direction == Hex.Direction.W)
        {
            newHexHex.x = originX - 1;
            newHexHex.y = originY + 1;
            newHexHex.z = originZ;
            newHex.transform.position = originHex.transform.position + west;
        }
        else if (direction == Hex.Direction.NW)
        {
            newHexHex.x = originX;
            newHexHex.y = originY + 1;
            newHexHex.z = originZ - 1;
            newHex.transform.position = originHex.transform.position + northwest;
        }

        newHexHex.setId();
        newHexHex.name = newHexHex.id;

        GameControl.map.AddHexToMap(newHexHex);
        newHexHex.DisableOverlappingColliders();
        newHexHex.Highlight();
        GameControl.map.vertexDisplacer.DisplaceVertices(newHexHex);
    }

    private void OnEnable()
    {
        if (GameControl.map != null)
        {
            foreach (Hex hex in GameControl.map.GetAllHexes())
            {
                hex.Highlight();
            }
        }
        GameControl.terrain.drawTreesAndFoliage = false;
    }
}
