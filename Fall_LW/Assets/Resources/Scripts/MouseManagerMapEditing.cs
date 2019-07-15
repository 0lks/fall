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

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
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
                            string colliderDirection = DetermineDirection(hitInfo.collider);
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

    void RightClickHex(GameObject hitObject)
    {
        Hex hitHex = hitObject.GetComponent<Hex>();
        hitHex.DeleteHex();
    }

    string DetermineDirection(Collider collider)
    {
        // Read in the collider data
        SphereCollider sphereCollider = (SphereCollider) collider;
        Vector3 center = sphereCollider.center;
        float x = center.x;
        float z = center.z;

        // Use that data to determine which direction the collider represents
        string direction = "";
        if (x > 0 & z > 0) direction = "NE";
        else if (x > 0 & z == 0) direction = "E";
        else if (x > 0 & z < 0) direction = "SE";
        else if (x < 0 & z < 0) direction = "SW";
        else if (x < 0 & z == 0) direction = "W";
        else if (x < 0 & z > 0) direction = "NW";
        
        return direction;
    }

    void BuildHex(string direction, GameObject origin)
    {
        Hex originHex = origin.GetComponent<Hex>();
        Vector3 originPosition = origin.transform.position;
        int originX = originHex.x;
        int originY = originHex.y;
        int originZ = originHex.z;

        GameObject newHex = Instantiate(GameControl.hexPrefab, originHex.transform.position, Quaternion.identity, GameControl.map.transform.Find("Hexes"));
        Hex newHexHex = newHex.GetComponent<Hex>();

        if (direction == "NE")
        {
            newHexHex.x = originX - 1;
            newHexHex.y = originY + 1;
            newHexHex.z = originZ;
            newHexHex.transform.position = originPosition + northeast;
        }
        else if (direction == "E")
        {
            newHexHex.x = originX;
            newHexHex.y = originY + 1;
            newHexHex.z = originZ + 1;
            newHex.transform.position = originPosition + east;
        }
        else if (direction == "SE")
        {
            newHexHex.x = originX + 1;
            newHexHex.y = originY;
            newHexHex.z = originZ + 1;
            newHex.transform.position = originPosition + southeast;
        }
        else if (direction == "SW")
        {
            newHexHex.x = originX + 1;
            newHexHex.y = originY - 1;
            newHexHex.z = originZ;
            newHex.transform.position = originHex.transform.position + southwest;
        }
        else if (direction == "W")
        {
            newHexHex.x = originX;
            newHexHex.y = originY - 1;
            newHexHex.z = originZ - 1;
            newHex.transform.position = originHex.transform.position + west;
        }
        else if (direction == "NW")
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
