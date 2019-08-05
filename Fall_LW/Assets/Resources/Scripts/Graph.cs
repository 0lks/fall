using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Graph : Map
// Container for pathfinding algorithms
{
    Dictionary<Hex, Node> nodeDict;
    public Graph(Map map)
    {
        nodeDict = new Dictionary<Hex, Node>();
        List<Hex> allHexes = map.GetAllHexes();
        foreach (Hex hex in allHexes)
        {
            if (hex.blocked) continue;
            nodeDict.Add(hex, new Node(hex));
        }

        foreach (Node node in nodeDict.Values)
        {
            node.Process(nodeDict);
        }
    }

    private HashSet<Node> changedNodes;
    public Queue<Hex> Path(Hex startHex, Hex endHex)
    // Find the shortest path using A*
    {
        if (endHex.blocked) return new Queue<Hex>();
        Node startNode = nodeDict[startHex];
        Node endNode = nodeDict[endHex];
        HashSet<Node> visitedNodes = new HashSet<Node>();

        startNode.tentativeDistance = 0;
        //startNode.pathToHere.Enqueue(startNode);
        startNode.CalcDistToDest(endHex);
        Queue<Node> q = new Queue<Node>();
        q.Enqueue(startNode);

        var relevantLayers = (1 << 14 | 1 << 18);
        while (q.Count > 0)
        {
            q = new Queue<Node>(q.OrderBy(t => t.tentativeDistance + t.distanceToDest));
            Node currentNode = q.Dequeue();
            visitedNodes.Add(currentNode);

            if (currentNode == endNode)
            {
                changedNodes = new HashSet<Node>(q);
                changedNodes.UnionWith(visitedNodes);
                return NodeQToHexQ(currentNode.pathToHere);
            }

            if (Hex.Occupied(currentNode.hex))
            {
                if (currentNode != startNode) continue;
            }

            currentNode.CalcDistToDest(endHex);

            foreach (Node neighbour in currentNode.neighbours.Keys)
            {
                neighbour.CalcDistToDest(endHex);
                if (!visitedNodes.Contains(neighbour))
                {
                    if (!q.Contains(neighbour))
                    {
                        q.Enqueue(neighbour);
                    }

                    if ((currentNode.tentativeDistance + currentNode.neighbours[neighbour] + currentNode.distanceToDest)
                        < (neighbour.tentativeDistance + neighbour.distanceToDest)
                        &&
                        !Physics.CapsuleCast(
                        currentNode.hex.GetPositionOnGround() + Vector3.up * 7f,
                        currentNode.hex.GetPositionOnGround() + Vector3.up * 5f,
                        1f,
                        (neighbour.hex.GetPositionOnGround() + Vector3.up * 6f)
                        - (currentNode.hex.GetPositionOnGround() + Vector3.up * 6f),
                        Vector3.Distance(currentNode.hex.GetPositionOnGround()
                        + Vector3.up * 6f,
                        neighbour.hex.GetPositionOnGround()
                        + Vector3.up * 6f),
                        relevantLayers))
                    {
                        //Debug.DrawRay(currentNode.hex.GetPositionOnGround() + Vector3.up * 6f, (neighbour.hex.GetPositionOnGround() + Vector3.up * 6f) - (currentNode.hex.GetPositionOnGround() + Vector3.up * 6f), Color.blue, 10);
                        neighbour.tentativeDistance = currentNode.tentativeDistance + currentNode.neighbours[neighbour];
                        Queue<Node> p = new Queue<Node>(currentNode.pathToHere);
                        p.Enqueue(neighbour);
                        neighbour.pathToHere = p;
                    }
                }
            }
        }
        throw new System.Exception("No path found");
    }

    private Queue<Hex> NodeQToHexQ(Queue<Node> q)
    {
        Queue<Hex> hexQ = new Queue<Hex>();
        while (q.Count > 0) hexQ.Enqueue(q.Dequeue().hex);
        ResetNodes();
        return hexQ;
    }

    public void ResetNodes()
    // RESET AFTER EVERY MOVE
    {
        foreach (Node node in changedNodes) node.ResetNode();
    }
}

class Node
{
    public Dictionary<Node, int> neighbours; // Neighbour + cost to reach (edge)
    public Hex hex;
    public int tentativeDistance = int.MaxValue - 5000; // Cost to reach this node. DO NOT USE BITWISE MAX.
    public int distanceToDest = -1;
    public Queue<Node> pathToHere;

    public Node(Hex hex)
    {
        this.hex = hex;
    }

    public void Process(Dictionary<Hex, Node> dict)
    {
        List<Hex> neighboursAsHexes = hex.GetImmediateNeighboursNoDir();
        neighbours = new Dictionary<Node, int>();
        pathToHere = new Queue<Node>();
        foreach (Hex hex in neighboursAsHexes)
        {
            if (!hex.blocked) neighbours.Add(dict[hex], 1);
        }
    }
    
    public void CalcDistToDest(Hex destination)
    {
        if (distanceToDest == -1)
        {
            distanceToDest = hex.DistanceTo(destination);
        }
    }

    public void ResetNode()
    {
        tentativeDistance = int.MaxValue - 5000;
        distanceToDest = -1;
        pathToHere = new Queue<Node>();
    }
}
