﻿using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using B83.MeshTools;
using HexData;

// Internal dependencies
using FALL.Core;
using FALL.Characters;

public class Hex : MonoBehaviour {
    /*
     * LOCAL DATA
    */
    [HideInInspector] public HexSerializedContent serialData;
    [HideInInspector] public int x;
    [HideInInspector] public int y;
    [HideInInspector] public int z;
    [HideInInspector] public string id;
    [HideInInspector] public string hexType;
    [HideInInspector] public bool[] disabledColliders;
    SphereCollider[] sphereColliders;
    [HideInInspector] public Character occupyingCharacter;
    [HideInInspector] public bool blocked;
    [HideInInspector] public int chanceToHit;
    [HideInInspector] public bool inEnemyRange;
    bool hovered = false;
    Queue<Hex> path;
    List<Hex> immediateNeighbours;
    MeshRenderer meshRend;
    public MeshFilter meshFilter;
    [HideInInspector] public enum HexType {Walkable, Black, Blocked, Editing, Hover, Danger, Att25, Att50, Att75, Att100, NULL}
    [HideInInspector] public HexType type;
    [HideInInspector] public HexType previousType;
    [HideInInspector] public enum Direction {NE, E, SE, SW, W, NW}

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
        meshFilter = transform.GetChild(0).GetComponent<MeshFilter>();
        occupyingCharacter = null;
        sphereColliders = transform.GetChild(0).GetComponents<SphereCollider>();
        inEnemyRange = false;
        blocked = false;
        Mathf.Clamp(chanceToHit, 0, 100);
    }
    private void Start()
    {
        RefreshImmediateNeighbours();
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

        byte[] bytes = MeshSerializer.SerializeMesh(meshFilter.mesh);
        serialData.mesh = bytes;
        Vector3 groundPos = mesh.bounds.center;
        serialData.groundPos = new float[] { groundPos.x, groundPos.y, groundPos.z};
    }

    public void CapsuleTest()
    {
        CapsuleCollider cc = gameObject.AddComponent<CapsuleCollider>();
        cc.isTrigger = true;
        cc.radius = 1.5f;
        cc.height = 6f;
        cc.direction = 1;
        cc.center = mesh.bounds.center;

        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Destroy(cc);
        Destroy(rb);
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

        //float[] gp = data.groundPos;
        //GetComponent<SphereCollider>().center = new Vector3(gp[0], gp[1], gp[2]);
        transform.gameObject.AddComponent<MeshCollider>();
        GetComponent<MeshCollider>().convex = false;
        GetComponent<MeshCollider>().sharedMesh = mesh;

        //GetComponent<CapsuleCollider>().center = new Vector3(gp[0], gp[1], gp[2]);
        CapsuleTest();

        for (int i = 0; i < disabledColliders.Length; i++)
        {
            sphereColliders[i].enabled = !disabledColliders[i];
        }
    }
    #endregion
    #region Colliders
////______________________________________________________________________________________________________________________________________________________________________________________________________________
    public void DisableOverlappingColliders()
    {
        List<KeyValuePair<Hex, string>> neighbours = GetImmediateNeighboursWithDirection();
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
    public void RefreshImmediateNeighbours()
    {
        immediateNeighbours = GetImmediateNeighboursNoDir();
    }
    public List<KeyValuePair<Hex, string>> GetImmediateNeighboursWithDirection()
    {
        List<KeyValuePair<Hex, string>> neighbours = new List<KeyValuePair<Hex, string>>();

        if (GameControl.map.HexExists(x + 1, y, z - 1))
            neighbours.Add(new KeyValuePair<Hex, string>(GameControl.map.GetHex(x + 1, y, z - 1), "NE"));
        if (GameControl.map.HexExists(x + 1, y - 1, z))
            neighbours.Add(new KeyValuePair<Hex, string>(GameControl.map.GetHex(x + 1, y - 1, z), "E"));
        if (GameControl.map.HexExists(x, y - 1, z + 1))
            neighbours.Add(new KeyValuePair<Hex, string>(GameControl.map.GetHex(x, y - 1, z + 1), "SE"));
        if (GameControl.map.HexExists(x - 1, y, z + 1))
            neighbours.Add(new KeyValuePair<Hex, string>(GameControl.map.GetHex(x - 1, y, z + 1), "SW"));
        if (GameControl.map.HexExists(x - 1, y + 1, z))
            neighbours.Add(new KeyValuePair<Hex, string>(GameControl.map.GetHex(x - 1, y + 1, z), "W"));
        if (GameControl.map.HexExists(x, y + 1, z - 1))
            neighbours.Add(new KeyValuePair<Hex, string>(GameControl.map.GetHex(x, y + 1, z - 1), "NW"));

        return neighbours;
    }

    public List<Hex> GetImmediateNeighboursNoDir()
    {
        List<Hex> neighbours = new List<Hex>();

        if (GameControl.map.HexExists(x + 1, y, z - 1))
            neighbours.Add(GameControl.map.GetHex(x + 1, y, z - 1));
        if (GameControl.map.HexExists(x + 1, y - 1, z))
            neighbours.Add(GameControl.map.GetHex(x + 1, y - 1, z));
        if (GameControl.map.HexExists(x, y - 1, z + 1))
            neighbours.Add(GameControl.map.GetHex(x, y - 1, z + 1));
        if (GameControl.map.HexExists(x - 1, y, z + 1))
            neighbours.Add(GameControl.map.GetHex(x - 1, y, z + 1));
        if (GameControl.map.HexExists(x - 1, y + 1, z))
            neighbours.Add(GameControl.map.GetHex(x - 1, y + 1, z));
        if (GameControl.map.HexExists(x, y + 1, z - 1))
            neighbours.Add(GameControl.map.GetHex(x, y + 1, z - 1));

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

    public List<Hex> GetDistantNeighbours(int distance, bool inclBlocked)
    {
        List<Hex> neighbours = new List<Hex>();

        for (int dx = -distance; dx <= distance; dx++)
        {
            int id_x = x + dx;
            for (int dy = -distance; dy <= distance; dy++)
            {
                int id_y = y + dy;
                for (int dz = -distance; dz <= distance; dz++)
                {
                    int id_z = z + dz;
                    if (GameControl.map.HexExists(id_x, id_y, id_z))
                    {
                        Hex hex = GameControl.map.GetHex(id_x, id_y, id_z);
                        if (inclBlocked)
                        {
                            neighbours.Add(hex);
                        }
                        else if (!hex.blocked) neighbours.Add(hex);
                    }
                }
            }
        }

        return neighbours;
    }

    public static Direction GetDirectionalRelation(Hex hexOrigin, Hex hexEnd)
    {
        if ((hexEnd.x > hexOrigin.x) && (hexEnd.z < hexOrigin.z)) return Direction.NE;
        else if ((hexEnd.x > hexOrigin.x) && (hexEnd.y < hexOrigin.y)) return Direction.E;
        else if ((hexEnd.y < hexOrigin.y) && (hexEnd.z > hexOrigin.z)) return Direction.SE;
        else if ((hexEnd.x < hexOrigin.x) && (hexEnd.z > hexOrigin.z)) return Direction.SW;
        else if ((hexEnd.x < hexOrigin.x) && (hexEnd.y > hexOrigin.y)) return Direction.W;
        else if ((hexEnd.y > hexOrigin.y) && (hexEnd.z < hexOrigin.z)) return Direction.NW;
        else throw new Exception("Failed to find the directional relation.");
    }

    public Hex GetNeighbour(Hex.Direction direction, int distance)
    // Use "HexExists" before calling this method.
    // distance = 1 is immediate neighbours.
    {
        if (distance < 0) throw new Exception("Attempted to find a neighbour using a negative distance parameter.");
        else if (distance == 0) return this;

        try
        {
            if      (direction == Hex.Direction.NE) return GameControl.map.GetHex(x + distance, y, z - distance);
            else if (direction == Hex.Direction.E) return GameControl.map.GetHex(x + distance, y - distance, z);
            else if (direction == Hex.Direction.SE) return GameControl.map.GetHex(x, y - distance, z + distance);
            else if (direction == Hex.Direction.SW) return GameControl.map.GetHex(x - distance, y, z + distance);
            else if (direction == Hex.Direction.W) return GameControl.map.GetHex(x - distance, y + distance, z);
            else return GameControl.map.GetHex(x, y + distance, z - distance);
        }
        catch (Exception e)
        {
            throw e;
        }
    }

    public static int[] GetAxis(Direction direction)
    {
        int[] axis = new int[3];
        if (direction == Direction.NE)
        {
            axis[0] = +1;
            axis[1] = 0;
            axis[2] = -1;
        }
        if (direction == Direction.E)
        {
            axis[0] = +1;
            axis[1] = -1;
            axis[2] = 0;
        }
        if (direction == Direction.SE)
        {
            axis[0] = 0;
            axis[1] = -1;
            axis[2] = +1;
        }
        if (direction == Direction.SW)
        {
            axis[0] = -1;
            axis[1] = 0;
            axis[2] = +1;
        }
        if (direction == Direction.W)
        {
            axis[0] = -1;   
            axis[1] = 1;
            axis[2] = 0;
        }
        if (direction == Direction.NW)
        {
            axis[0] = 0;
            axis[1] = +1;
            axis[2] = -1;
        }
        return axis;
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

    #endregion
    #region Highlighting
////______________________________________________________________________________________________________________________________________________________________________________________________________________
    public void SwapMaterials(HexType newType)
    {
        if (blocked) return;
        type = newType;
        Material[] materials = meshRend.sharedMaterials;

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

        meshRend.sharedMaterials = materials;
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
        else if (GameControl.playerState == GameControl.PlayerState.Move || GameControl.playerState == GameControl.PlayerState.Exploring)
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

        else if (GameControl.playerState == GameControl.PlayerState.Attack)
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
        if ((GameControl.playerState != GameControl.PlayerState.Move) && (GameControl.playerState != GameControl.PlayerState.Exploring))
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
        if (GameControl.playerState != GameControl.PlayerState.Attack)
            throw new Exception("Something tried to highlight attack area when the player is not in ATTACK mode!");
        GameControl.player.UnHighLightSurrounding();

        List<Hex> attackableHexes = FilterAttackableHexes(GetDistantNeighbours(distance, false));
        
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
        return meshRend.bounds.center;
    }
    public void DeleteHex()
    {
        List<KeyValuePair<Hex, string>> neighbours = GetImmediateNeighboursWithDirection();
        GameControl.map.RemoveFromMap(this);
        Destroy(this.gameObject);
        RestoreNeighbouringColliders(neighbours);
    }

    // These values are set by HexVertexDisplacer
    [HideInInspector] public Mesh mesh;
    [HideInInspector] public Vector3[] originalMesh;
    // // // // // // // // // // // // // // // //

    public void Hover()
    {
        if (blocked
            || hovered
            || GameControl.player.animator.GetBool("Moving")) return;

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
        meshFilter.mesh.vertices = originalMesh;
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
            GameControl.playerState == GameControl.PlayerState.Move
            || GameControl.playerState == GameControl.PlayerState.Exploring)
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

    public Hex(int id_x, int id_y, int id_z)
    // This constructor was created for UpdateHexRender specifically, not intended to be used
    // elsewhere in the project.
    {
        x = id_x;
        y = id_y;
        z = id_z;
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
