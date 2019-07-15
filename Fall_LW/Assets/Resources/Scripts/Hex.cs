using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using B83.MeshTools;
using HexData;

public class Hex : MonoBehaviour {
    /*
     * LOCAL DATA
    */
    public HexSerializedContent serialData;
    public int x;
    public int y;
    public int z;
    public string id;
    [HideInInspector] public string hexType;
    public bool[] disabledColliders;
    private SphereCollider[] sphereColliders;
    [HideInInspector] public Character occupyingCharacter;
    [HideInInspector] public bool blocked;
    [HideInInspector] public int chanceToHit;
    [HideInInspector] public bool inEnemyRange;
    bool hovered = false;
    Queue<Hex> path;
    List<Hex> immediateNeighbours;
    MeshRenderer meshRend;

    public enum HexType {Walkable, Black, Blocked, Editing, Hover, Danger, Att25, Att50, Att75, Att100, NULL}
    public HexType type;
    public HexType previousType;

    public HexDataHolder hexData;

    private void Awake()
    {
        hexData.SetVariables();
        type = HexType.Black;
        previousType = HexType.NULL;
        chanceToHit = 0;
        disabledColliders = new bool[6];
        GetComponentInChildren<MeshRenderer>().sharedMaterials[0] = hexData.black_Edge;
        GetComponentInChildren<MeshRenderer>().sharedMaterials[1] = hexData.black_Edge;
        meshRend = GetComponentInChildren<MeshRenderer>();
        occupyingCharacter = null;
        sphereColliders = transform.GetChild(0).GetComponents<SphereCollider>();
        inEnemyRange = false;
        blocked = false;
        Mathf.Clamp(chanceToHit, 0, 100);
    }
    private void Start()
    {
        immediateNeighbours = GetImmediateNeighboursNoDir();
    }

    #region File IO
////______________________________________________________________________________________________________________________________________________________________________________________________________________
    public void prepSerialize()
    {
        serialData.x = x;
        serialData.y = y;
        serialData.z = z;
        serialData.id = id;
        serialData.hexType = hexType;

        serialData.loc_x = transform.position.x;
        serialData.loc_y = transform.position.y;
        serialData.loc_z = transform.position.z;

        serialData.scale_x = transform.localScale.x;
        serialData.scale_y = transform.localScale.y;
        serialData.scale_z = transform.localScale.z;

        serialData.disabledColliders = disabledColliders;

        byte[] bytes = MeshSerializer.SerializeMesh(GetComponentInChildren<MeshFilter>().mesh);
        serialData.mesh = bytes;
        Vector3 groundPos = mesh.bounds.center;
        serialData.groundPos = new float[] { groundPos.x, groundPos.y, groundPos.z};
    }

    public void SetHex(HexSerializedContent data)
    {
        x = data.x;
        y = data.y;
        z = data.z;
        id = data.id;
        transform.name = id;
        hexType = data.hexType;
        transform.position = new Vector3(data.loc_x, data.loc_y, data.loc_z);
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(data.scale_x, data.scale_y, data.scale_z);
        disabledColliders = data.disabledColliders;

        MeshFilter mf = GetComponentInChildren<MeshFilter>();
        mf.mesh = MeshSerializer.DeserializeMesh(data.mesh);
        mesh = mf.mesh;
        originalMesh = mesh.vertices;

        float[] gp = data.groundPos;
        GetComponent<SphereCollider>().center = new Vector3(gp[0], gp[1], gp[2]);
        GetComponent<CapsuleCollider>().center = new Vector3(gp[0], gp[1], gp[2]);
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        for (int i = 0; i < disabledColliders.Length; i++)
        {
            sphereColliders[i].enabled = !disabledColliders[i];
        }

        Destroy(GetComponent<CapsuleCollider>());
        Destroy(GetComponent<Rigidbody>());
    }
    #endregion
    #region Colliders
////______________________________________________________________________________________________________________________________________________________________________________________________________________
    public void DisableOverlappingColliders()
    {
        List<KeyValuePair<Hex, string>> neighbours = GetImmediateNeighbours();
        foreach (KeyValuePair<Hex, string> neighbour in neighbours)
        {
            if (neighbour.Value == "NE")
            {
                sphereColliders[0].enabled = false;
                disabledColliders[0] = true;
                neighbour.Key.sphereColliders[3].enabled = false;
                neighbour.Key.disabledColliders[3] = true;
            }
            if (neighbour.Value == "E")
            {
                sphereColliders[1].enabled = false;
                disabledColliders[1] = true;
                neighbour.Key.sphereColliders[4].enabled = false;
                neighbour.Key.disabledColliders[4] = true;
            }
            if (neighbour.Value == "SE")
            {
                sphereColliders[2].enabled = false;
                disabledColliders[2] = true;
                neighbour.Key.sphereColliders[5].enabled = false;
                neighbour.Key.disabledColliders[5] = true;
            }
            if (neighbour.Value == "SW")
            {
                sphereColliders[3].enabled = false;
                disabledColliders[3] = true;
                neighbour.Key.sphereColliders[0].enabled = false;
                neighbour.Key.disabledColliders[0] = true;
            }
            if (neighbour.Value == "W")
            {
                sphereColliders[4].enabled = false;
                disabledColliders[4] = true;
                neighbour.Key.sphereColliders[1].enabled = false;
                neighbour.Key.disabledColliders[1] = true;
            }
            if (neighbour.Value == "NW")
            {
                sphereColliders[5].enabled = false;
                disabledColliders[5] = true;
                neighbour.Key.sphereColliders[2].enabled = false;
                neighbour.Key.disabledColliders[2] = true;
            }
        }
    }

    public void RestoreNeighbouringColliders(List<KeyValuePair<Hex, string>> neighbours)
    {
        foreach (KeyValuePair<Hex, string> neighbour in neighbours)
        {
            if (neighbour.Value == "NE")
            {
                neighbour.Key.sphereColliders[3].enabled = true;
                neighbour.Key.disabledColliders[3] = false;
            }
            else if (neighbour.Value == "E")
            {
                neighbour.Key.sphereColliders[4].enabled = true;
                neighbour.Key.disabledColliders[4] = false;
            }
            else if (neighbour.Value == "SE")
            {
                neighbour.Key.sphereColliders[5].enabled = true;
                neighbour.Key.disabledColliders[5] = false;
            }
            else if (neighbour.Value == "SW")
            {
                neighbour.Key.sphereColliders[0].enabled = true;
                neighbour.Key.disabledColliders[0] = false;
            }
            else if (neighbour.Value == "W")
            {
                neighbour.Key.sphereColliders[1].enabled = true;
                neighbour.Key.disabledColliders[1] = false;
            }
            else if (neighbour.Value == "NW")
            {
                neighbour.Key.sphereColliders[2].enabled = true;
                neighbour.Key.disabledColliders[2] = false;
            }
        }
    }
    #endregion
    #region Neighbours
////______________________________________________________________________________________________________________________________________________________________________________________________________________
    public List<KeyValuePair<Hex, string>> GetImmediateNeighbours()
    {
        List<KeyValuePair<Hex, string>> neighbours = new List<KeyValuePair<Hex, string>>();

        string neighbour_id_NE = (x-1) + "_" + (y+1) + "_" + (z);
        string neighbour_id_E = (x) + "_" + (y+1) + "_" + (z+1);
        string neighbour_id_SE = (x+1) + "_" + (y) + "_" + (z+1);
        string neighbour_id_SW = (x+1) + "_" + (y-1) + "_" + (z);
        string neighbour_id_W = (x) + "_" + (y-1) + "_" + (z-1);
        string neighbour_id_NW = (x-1) + "_" + (y) + "_" + (z-1);

        List<KeyValuePair<string, string>> ids = new List<KeyValuePair<string, string>>() {
            new KeyValuePair<string, string>(neighbour_id_NE, "NE"),
            new KeyValuePair<string, string>(neighbour_id_E, "E"),
            new KeyValuePair<string, string>(neighbour_id_SE, "SE"),
            new KeyValuePair<string, string>(neighbour_id_SW, "SW"),
            new KeyValuePair<string, string>(neighbour_id_W, "W"),
            new KeyValuePair<string, string>(neighbour_id_NW, "NW"),
        };

        foreach (KeyValuePair<string, string> id_ in ids)
        {
            if (GameControl.map.HexExists(id_.Key))
            {
                string neighbourDirection = id_.Value;
                neighbours.Add(new KeyValuePair<Hex, string>(GameControl.map.map[id_.Key], neighbourDirection));
            }
        }

        return neighbours;
    }

    public List<Hex> GetDistantNeighboursConnected(int distance)
    {
        HashSet<Hex> output = new HashSet<Hex>(immediateNeighbours);
        while (distance > 1)
        {
            HashSet<Hex> toAdd = new HashSet<Hex>();
            foreach (Hex neighbour in output)
            {
                List<Hex> _neighbours = neighbour.immediateNeighbours;
                foreach (Hex n in _neighbours)
                {
                    if (!output.Contains(n) && !toAdd.Contains(n) && !n.blocked) toAdd.Add(n);
                }
            }
            output.UnionWith(toAdd);
            distance--;
        }

        return output.ToList();
    }

    public List<Hex> GetDistantNeighbours(int distance)
    {
        List<Hex> neighbours = new List<Hex>();

        for (int dx = -distance; dx <= distance; dx++)
        {
            string id_x = (x + dx).ToString();
            for (int dy = -distance; dy <= distance; dy++)
            {
                string id_y = (y + dy).ToString();
                for (int dz = -distance; dz <= distance; dz++)
                {
                    string id_z = (z + dz).ToString();
                    string id = string.Join("_", new string[] { id_x, id_y, id_z });
 
                    if (GameControl.map.HexExists(id))
                    {
                        Hex hex = GameControl.map.GetHex(id);
                        if (!hex.blocked) neighbours.Add(hex);
                    }
                }
            }
        }

        return neighbours;
    }

    public int DistanceTo(Hex end)
    {
        int maxDist = 0;
        int xDist = Math.Abs(end.x - x);
        int yDist = Math.Abs(end.y - y);
        int zDist = Math.Abs(end.z - z);
        if (xDist > maxDist) maxDist = xDist;
        if (yDist > maxDist) maxDist = yDist;
        if (zDist > maxDist) maxDist = zDist;
        return maxDist;
    }

    private List<Hex> removeNeighbourDirections(List<KeyValuePair<Hex, string>> immediateNeighbours)
    {
        List<Hex> _immediateNeighbours = new List<Hex>();
        foreach (KeyValuePair<Hex, string> pair in immediateNeighbours)
        {
            _immediateNeighbours.Add(pair.Key);
        }
        return _immediateNeighbours;
    }    

    public List<Hex> GetImmediateNeighboursNoDir()
    {
        return removeNeighbourDirections(GetImmediateNeighbours());
    }
    #endregion
    #region Highlighting
////______________________________________________________________________________________________________________________________________________________________________________________________________________
    public void SwapMaterials(HexType newType)
    {
        if (blocked) return;
        type = newType;
        Material[] materials = GetComponentInChildren<MeshRenderer>().sharedMaterials;

        if (newType == HexType.Walkable)
        {
            materials[0] = hexData.walkable_Edge;
            materials[1] = hexData.emptyCenter;
        }
        else if (newType == HexType.Black)
        {
            materials[0] = hexData.black_Edge;
            materials[1] = hexData.emptyCenter;
        }
        else if (newType == HexType.Blocked)
        {
            materials[0] = hexData.black_Edge;
            materials[1] = hexData.base_Center;
        }
        else if (newType == HexType.Editing)
        {
            materials[0] = hexData.editingHexMaterialEdge;
            materials[1] = hexData.editingHexMaterialCenter;
        }
        else if (newType == HexType.Hover)
        {
            materials[0] = hexData.hover_Edge;

        }
        else if (newType == HexType.Danger)
        {
            materials[0] = hexData.walkable_Edge;
            materials[1] = hexData.hex_Danger_Center;
        }
        else if (newType == HexType.Att25)
        {
            materials[0] = hexData.hex_25_Edge;
            materials[1] = hexData.hex_25_Center;
        }
        else if (newType == HexType.Att50)
        {
            materials[0] = hexData.hex_50_Edge;
            materials[1] = hexData.hex_50_Center;
        }
        else if (newType == HexType.Att75)
        {
            materials[0] = hexData.hex_75_Edge;
            materials[1] = hexData.hex_75_Center;
        }
        else if (newType == HexType.Att100)
        {
            materials[0] = hexData.hex_100_Edge;
            materials[1] = hexData.hex_100_Center;
        }
        else if (newType == HexType.NULL)
        {
            materials[0] = hexData.black_Edge;
            materials[1] = hexData.base_Center;
        }

        GetComponentInChildren<MeshRenderer>().sharedMaterials = materials;
    }

    public bool Highlight()
    {
        if (blocked)
        {
            SwapMaterials(HexType.Blocked);
            return false;
        }
        if (GameControl.activeMouseMode == "MapEditing")
        {
            SwapMaterials(HexType.Editing);
            return true;
        }

        if (previousType != HexType.NULL)
        // Exiting hover
        {
            SwapMaterials(previousType);
            previousType = HexType.NULL;
            return true;
        }
        if (GameControl.player.currentPosition == this)
        {
            SwapMaterials(HexType.Hover);
            return true;
        }
        else if (GameControl.playerState == "MOVE" || GameControl.playerState == "EXPLORING")
        {
            GameControl.player.highlightedNeighbours.Add(this);

            if (inEnemyRange)
            {
                SwapMaterials(HexType.Danger);
                return true;
            }
            else if (GameControl.player.currentPosition.DistanceTo(this) <= GameControl.player.movementAmount)
            {
                SwapMaterials(HexType.Walkable);
                return true;
            }
        }

        else if (GameControl.playerState == "ATTACK")
        {
            if (chanceToHit == 25f)
            {
                SwapMaterials(HexType.Att25);
                GameControl.player.highlightedNeighbours.Add(this);
                return true;
            }
            else if (chanceToHit == 50f)
            {
                SwapMaterials(HexType.Att50);
                GameControl.player.highlightedNeighbours.Add(this);
                return true;
            }
            else if (chanceToHit == 75f)
            {
                SwapMaterials(HexType.Att75);
                GameControl.player.highlightedNeighbours.Add(this);
                return true;
            }
            else if (chanceToHit == 100f)
            {
                SwapMaterials(HexType.Att100);
                GameControl.player.highlightedNeighbours.Add(this);
                return true;
            }
        }

        return false;
    }
    public void HighLightSurroundingMoveState(int distance)
    {
        if ((GameControl.playerState != "MOVE") && (GameControl.playerState != "EXPLORING"))
            throw new Exception("Something tried to highlight movement area when the player is not in MOVE/EXPLORING mode!");
        GameControl.player.UnHighLightSurrounding();

        if (distance == 0)
        {
            return;
        }
        else
        {
            List<Hex> neighbours = GetDistantNeighboursConnected(distance);
            GameControl.player.DetermineTriggerZone(neighbours);
            foreach (Hex neighbour in neighbours)
            {
                if (!neighbour.blocked)
                {
                    neighbour.Highlight();
                }
            }
        }
    }

    public void HighLightSurroundingAttackState(int distance)
    {
        if (GameControl.playerState != "ATTACK")
            throw new Exception("Something tried to highlight attack area when the player is not in ATTACK mode!");
        GameControl.player.UnHighLightSurrounding();

        List<Hex> attackableHexes = FilterAttackableHexes(GetDistantNeighbours(distance));
        
        foreach (Hex neighbour in attackableHexes)
        {
            if (!neighbour.blocked)
            {
                neighbour.Highlight();
            }
        }
    }

    public void Unhighlight()
    {
        SwapMaterials(HexType.Black);
        inEnemyRange = false;
        previousType = HexType.NULL;
    }


    #endregion
////______________________________________________________________________________________________________________________________________________________________________________________________________________
    public void setId() { id = x + "_" + y + "_" + z; }

    public List<Hex> FilterAttackableHexes(List<Hex> hexesInRange)
    {
        List<Hex> attackable = new List<Hex>();
        Vector3 bowPosition = GameControl.player.transform.FindDeepChild("Bow").position;
        foreach (Hex hex in hexesInRange)
        {
            if (hex.occupyingCharacter == GameControl.player && !hex.blocked)continue;
            if (hex.CalculateChanceToHitHere() > 0 && !hex.blocked) attackable.Add(hex);
        }
        GameControl.player.attackableHexes = attackable;
        return attackable;
    }

    private float CalculateChanceToHitHere()
    {
        Transform helper = GameControl.player.transform.GetChild(3);
        RaycastHit[] capsule1;
        RaycastHit[] capsule2;
        RaycastHit[] capsule3;
        RaycastHit[] capsule4;
        var relevantLayers = (1 << 14 | 1 << 18 | 1 << 11);
        float hexWidthFraction = hexData.hexWidth * 0.6f;
        float quarter = hexWidthFraction / 4f;

        helper.LookAt(this.transform);
        Vector3 hexBasePos =
                            GetPositionOnGround()
                            + (Vector3.up
                            * helper.transform.localPosition.y)
                            - (helper.right
                            * quarter * 1.5f);
        Vector3 playerBasePos =
                            helper.transform.position
                            - (helper.right
                            * quarter * 1.5f);


        Vector3 hexPos1 = hexBasePos;
        Vector3 playerPos1 = playerBasePos;
        Vector3 direction1 = hexBasePos - playerBasePos;

        Vector3 hexPos2 = hexBasePos + helper.right * quarter;
        Vector3 playerPos2 = playerBasePos + helper.right * quarter;
        Vector3 direction2 = hexPos2 - playerPos2;

        Vector3 hexPos3 = hexBasePos + helper.right * quarter * 2;
        Vector3 playerPos3 = playerBasePos + helper.right * quarter * 2;
        Vector3 direction3 = hexPos3 - playerPos3;

        Vector3 hexPos4 = hexBasePos + helper.right * quarter * 3;
        Vector3 playerPos4 = playerBasePos + helper.right * quarter * 3;
        Vector3 direction4 = hexPos4 - playerPos4;

        capsule1 = Physics.CapsuleCastAll(playerPos1 + Vector3.up * 1f, playerPos1 - Vector3.up * 1f, 0.3f, direction1, Vector3.Distance(hexPos1, playerPos1), relevantLayers);
        capsule2 = Physics.CapsuleCastAll(playerPos2 + Vector3.up * 1f, playerPos2 - Vector3.up * 1f, 0.3f, direction2, Vector3.Distance(hexPos2, playerPos2), relevantLayers);
        capsule3 = Physics.CapsuleCastAll(playerPos3 + Vector3.up * 1f, playerPos3 - Vector3.up * 1f, 0.3f, direction3, Vector3.Distance(hexPos3, playerPos3), relevantLayers);
        capsule4 = Physics.CapsuleCastAll(playerPos4 + Vector3.up * 1f, playerPos4 - Vector3.up * 1f, 0.3f, direction4, Vector3.Distance(hexPos4, playerPos4), relevantLayers);

        chanceToHit = 100;
        if (capsule1.Length > 0) chanceToHit -= 25;
        if (capsule2.Length > 0) chanceToHit -= 25;
        if (capsule3.Length > 0) chanceToHit -= 25;
        if (capsule4.Length > 0) chanceToHit -= 25;

        if (capsule2.Length > 0 && capsule3.Length > 0) chanceToHit = 0;
        return chanceToHit;
    }

    public static bool Occupied(Hex hex)
    {
        if (hex.occupyingCharacter != null || hex.blocked) return true;
        else return false;
    }

    public Vector3 GetPositionOnGround()
    {
        return GetComponentInChildren<MeshRenderer>().bounds.center;
    }
    public void DeleteHex()
    {
        List<KeyValuePair<Hex, string>> neighbours = GetImmediateNeighbours();
        GameControl.map.RemoveFromMap(this);
        Destroy(this.gameObject);
        RestoreNeighbouringColliders(neighbours);
    }

    // These values are set by HexVertexDisplacer
    public Mesh mesh;
    public Vector3[] originalMesh;
    // // // // // // // // // // // // // // // //

    public void Hover()
    {
        if (blocked
            || hovered
            || GameControl.player.GetComponent<Animator>().GetBool("Moving")) return;

        hovered = true;
        previousType = type;
        SwapMaterials(HexType.Hover);

        // Raise the mesh off the ground
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] += Vector3.up * 0.25f;
        }
        mesh.vertices = vertices;
    }

    public void UnHover()
    {
        if (!hovered) return;
        hovered = false;
        Highlight();
        //Lower the mesh back on the ground
        transform.GetChild(0).GetComponent<MeshFilter>().mesh.vertices = originalMesh;
    }

    private void OnMouseEnter()
    {
        if (GameControl.movePath != null && GameControl.movePath.Contains(this)) return;
        if (!Input.GetMouseButton(1)) Hover();
    }

    public bool Selected()
    {
        if (GameControl.movePath != null) foreach (Hex hex in GameControl.movePath)
            {
                hex.UnHover();
                GameControl.movePath = null;
            }

        if ((
            GameControl.playerState == "MOVE"
            || GameControl.playerState == "EXPLORING")
            && GameControl.player.movementAmount > 0
            && type != HexType.Black
            && GameControl.activeMouseMode != "MapEditing"
            )
        {
            GameControl.movePath = GameControl.graph.Path(GameControl.player.currentPosition, this);
            if (GameControl.movePath.Count <= 0) return false;
            foreach (Hex hex in GameControl.movePath)
            {
                hex.Hover();
            }
            return true;
        }

        return false;
    }

    private void OnMouseExit()
    {
        if (GameControl.movePath != null && GameControl.movePath.Contains(this)) return;
        UnHover();
    }

    private void OnTriggerEnter(Collider other)
    // Called by the capsulecollider and means that this hex is not suitable for movement (blocked by other objects)
    {
        //Debug.Log("Hex " + id + " was disabled because its space is occupied by other objects.");
        Unhighlight();
        SwapMaterials(HexType.Blocked);
        blocked = true;

        Destroy(GetComponent<SphereCollider>());
        Destroy(GetComponent<CapsuleCollider>());
        Destroy(GetComponent<Rigidbody>());
    }
}

[Serializable]
public class HexSerializedContent
{
    public int x;
    public int y;
    public int z;
    public string id;
    public string hexType;
    public float loc_x;
    public float loc_y;
    public float loc_z;
    public float scale_x;
    public float scale_y;
    public float scale_z;
    public bool[] disabledColliders;
    public byte[] mesh;
    public float[] groundPos;
}
