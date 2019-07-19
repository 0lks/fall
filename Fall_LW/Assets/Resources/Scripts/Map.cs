using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;

public class Map : MonoBehaviour {
    //public Dictionary<string, Hex> map = new Dictionary<string, Hex>();
    public Dictionary<int, Dictionary<int, Dictionary<int, Hex>>> map = new Dictionary<int, Dictionary<int, Dictionary<int, Hex>>>();
    public MapSerializedContent data = new MapSerializedContent();
    public HexVertexDisplacer vertexDisplacer;

    private void Awake()
    {
        GameControl.map = this;
        vertexDisplacer = GetComponentInChildren<HexVertexDisplacer>();
    }

    public List<Hex> GetAllHexes()
    {
        List<Hex> _hexes = new List<Hex>();
        foreach (int x in map.Keys)
        {
            foreach (int y in map[x].Keys)
            {
                foreach (int z in map[x][y].Keys)
                {
                    _hexes.Add(map[x][y][z]);
                }
            }
        }
        return _hexes;
    }

    public void AddHexToMap(Hex hex)
    {
        if (map.ContainsKey(hex.x))
        {
            if (map[hex.x].ContainsKey(hex.y))
            {
                map[hex.x][hex.y][hex.z] = hex;
            }
            else
            {
                map[hex.x][hex.y] = new Dictionary<int, Hex>();
                map[hex.x][hex.y][hex.z] = hex;
            }
        }
        else
        {
            map[hex.x] = new Dictionary<int, Dictionary<int, Hex>>();
            map[hex.x][hex.y] = new Dictionary<int, Hex>();
            map[hex.x][hex.y][hex.z] = hex;
        }
    }

    public bool HexExists (int id_x, int id_y, int id_z)
    {
        if (map.ContainsKey(id_x))
        {
            if (map[id_x].ContainsKey(id_y))
            {
                if (map[id_x][id_y].ContainsKey(id_z))
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    public void Serialize()
    {
        Debug.Log("Preparing map data for serialization...");
        FileStream fileStream = new FileStream("start.dat", FileMode.Create);
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        
        /*
        foreach (KeyValuePair<string, Hex> hex in map)
        {
            hex.Value.prepSerialize();
            data.map[hex.Key] = hex.Value.serialData;
        }
        */

        foreach (Hex hex in GetAllHexes())
        {
            hex.prepSerialize();
            //data.newmap[hex.x][hex.y][hex.z] = hex.serialData;
            if (data.newmap.ContainsKey(hex.x))
            {
                if (data.newmap[hex.x].ContainsKey(hex.y))
                {
                    data.newmap[hex.x][hex.y][hex.z] = hex.serialData;
                }
                else
                {
                    data.newmap[hex.x][hex.y] = new Dictionary<int, HexSerializedContent>();
                    data.newmap[hex.x][hex.y][hex.z] = hex.serialData;
                }
            }
            else
            {
                data.newmap[hex.x] = new Dictionary<int, Dictionary<int, HexSerializedContent>>();
                data.newmap[hex.x][hex.y] = new Dictionary<int, HexSerializedContent>();
                data.newmap[hex.x][hex.y][hex.z] = hex.serialData;
            }
        }

        binaryFormatter.Serialize(fileStream, data);
        fileStream.Close();
        Debug.Log("Map has been serialized");
    }

    /*
    public void SetMap(MapSerializedContent data_)
    {
        // This gets run only once, when entering playmode
        Transform parent = transform.Find("Hexes");
        foreach (KeyValuePair<string, HexSerializedContent> deserializedHex in data_.map)
        {
            GameObject hexIsBack_ = Instantiate(GameControl.hexPrefab, parent);
            //hexIsBack.GetComponentInChildren<MeshRenderer>().enabled = false;

            //map[deserializedHex.Key] = hexIsBack.GetComponent<Hex>();
            Hex hexIsBack = hexIsBack_.GetComponent<Hex>();
            //map[hexIsBack.x][hexIsBack.y][hexIsBack.z] = hexIsBack.GetComponent<Hex>();
            hexIsBack.GetComponent<Hex>().SetHex(deserializedHex.Value);
            AddHexToMap(hexIsBack);
            Destroy(hexIsBack.GetComponent<HexVertexDisplacer>());
            //hexIsBack.gameObject.SetActive(false);
        }

        StartCoroutine(WaitAndConstructGraph());
    }
    */
    
    public void SetMap(MapSerializedContent data_)
    {
        // This gets run only once, when entering playmode
        Transform parent = transform.Find("Hexes");
        foreach (int x in data_.newmap.Keys)
        {
            foreach (int y in data_.newmap[x].Keys)
            {
                foreach (int z in data_.newmap[x][y].Keys)
                {
                    GameObject hexIsBack_ = Instantiate(GameControl.hexPrefab, parent);
                    Hex hexIsBack = hexIsBack_.GetComponent<Hex>();
                    //hexIsBack.GetComponentInChildren<MeshRenderer>().enabled = false;
                    hexIsBack.SetHex(data_.newmap[x][y][z]);
                    AddHexToMap(hexIsBack);
                    Destroy(hexIsBack.GetComponent<HexVertexDisplacer>());
                    //hexIsBack.gameObject.SetActive(false);
                }
            }
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

    public Hex GetHex(int x, int y, int z)
    {
        if (HexExists(x, y, z))
        {
            return map[x][y][z];
        }
        else throw new Exception("A requested hex was not found in the map");
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
        if (HexExists(hex.x, hex.y, hex.z))
        {
            map[hex.x][hex.y].Remove(hex.z);
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
    //public Dictionary<string, HexSerializedContent> map = new Dictionary<string, HexSerializedContent>();
    public Dictionary<int, Dictionary<int, Dictionary<int, HexSerializedContent>>> newmap = new Dictionary<int, Dictionary<int, Dictionary<int, HexSerializedContent>>>();
}