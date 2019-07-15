using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;

public class Map : MonoBehaviour {
    public Dictionary<string, Hex> map = new Dictionary<string, Hex>();
    public MapSerializedContent data = new MapSerializedContent();
    public HexVertexDisplacer vertexDisplacer;

    private void Awake()
    {
        GameControl.map = this;
        vertexDisplacer = GetComponentInChildren<HexVertexDisplacer>();
    }

    public Dictionary<string, Hex> GetAllHexesWID()
    {
        return map;
    }

    public List<Hex> GetAllHexes()
    {
        List<Hex> _hexes = new List<Hex>();
        foreach (Hex h in map.Values) _hexes.Add(h);
        return _hexes;
    }

    public void AddHexToMap(Hex hex)
    {
        string key = hex.id;
        if (!map.ContainsKey(key))
        {
            map.Add(key, hex.GetComponent<Hex>());
        }
    }

    public bool HexExists (string id)
    {
        if (map.ContainsKey(id)) return true;
        else return false;
    }

    public void Serialize()
    {
        Debug.Log("Preparing map data for serialization...");
        FileStream fileStream = new FileStream("start.dat", FileMode.Create);
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        
        foreach (KeyValuePair<string, Hex> hex in map)
        {
            hex.Value.prepSerialize();
            data.map[hex.Key] = hex.Value.serialData;
        }

        binaryFormatter.Serialize(fileStream, data);
        fileStream.Close();
        Debug.Log("Map has been serialized");
    }
    public void SetMap(MapSerializedContent data_)
    {
        // This gets run only once, when entering playmode
        Transform parent = transform.Find("Hexes");
        foreach (KeyValuePair<string, HexSerializedContent> deserializedHex in data_.map)
        {
            GameObject hexIsBack = Instantiate(GameControl.hexPrefab, parent);
            map[deserializedHex.Key] = hexIsBack.GetComponent<Hex>();
            hexIsBack.GetComponent<Hex>().SetHex(deserializedHex.Value);
            Destroy(hexIsBack.GetComponent<HexVertexDisplacer>());
        }
        StartCoroutine(WaitAndConstructGraph());
    }

    IEnumerator WaitAndConstructGraph()
    // We have to wait for all the triggers on the hexes to avoid inserting unnavigable (and untargetable) hexes
    // into the node graph.
    {
        yield return new WaitForSeconds(0.5f);
        GameControl.graph = new Graph(this);
    }

    public Hex GetHex(string id)
    {
        if (HexExists(id))
        {
            return map[id];
        }
        else throw new Exception("A hex with the corresponding ID was not found in the map");
    }

    public void UnHighLightEverything()
    {
        foreach (Hex hex in GetAllHexes())
        {
            GameControl.player.highlightedNeighbours.Clear();
            hex.Unhighlight();
        }
    }

    public void RemoveFromMap(Hex hex)
    {
        if (HexExists(hex.id))
        {
            map.Remove(hex.id);
        }
    }

    public List<Hex> GetRandomHexes(int count)
    {
        if (count <= 0) return null;
        List<Hex> randomHexes = new List<Hex>();
        List<Hex> allHexesCopy = new List<Hex>(GetAllHexes());
        while(count-- > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, allHexesCopy.Count - 1);
            Hex randHex = allHexesCopy[randomIndex];
            allHexesCopy.RemoveAt(randomIndex);
            while (randHex.blocked)
            {
                randomIndex = UnityEngine.Random.Range(0, allHexesCopy.Count - 1);
                randHex = allHexesCopy[randomIndex];
                allHexesCopy.RemoveAt(randomIndex);
            }
            randomHexes.Add(randHex);
        }
        return randomHexes;
    }
}

[Serializable]
public class MapSerializedContent
{
    public Dictionary<string, HexSerializedContent> map = new Dictionary<string, HexSerializedContent>();
}