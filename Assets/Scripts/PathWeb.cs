using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathWeb : MonoBehaviour
{
    [System.Serializable]
    public class WebNode
    {
        [HideInInspector]
        public int id = -1;
        public bool active = true;
        public string name;
        public Vector3 position;
        [HideInInspector]
        public List<int> connections = new List<int>();

        public static void ConnectNodes(ref WebNode node1, ref WebNode node2, bool twoWay)
        {
            if (node1 == null || node2 == null || node1 == node2)
            {
                return;
            }

            if (!node1.connections.Contains(node2.id))
            {
                node1.connections.Add(node2.id);
            }
            if (twoWay && !node2.connections.Contains(node1.id))
            {
                node2.connections.Add(node1.id);
            }
        }

        public static void DisconnectNodes(ref WebNode node1, ref WebNode node2, bool twoWay)
        {
            node1.connections.Remove(node2.id);
            if (twoWay)
            {
                node2.connections.Remove(node1.id);
            }
        }
    }

    [SerializeField]
    public List<WebNode> nodes = new List<WebNode>();
    public bool bidirectional = false;

    static int nextId = -1;

    [InspectorButton("New Node")]
    public void CreateNewNode()
    {
        if (nextId < 0)
        {
            foreach (WebNode node in nodes)
            {
                nextId = Mathf.Max(nextId, node.id);
            }
            nextId++;
        }

        WebNode newNode = new WebNode();
        newNode.id = nextId++;
        newNode.position = gameObject.transform.position + Vector3.up + Vector3.right;
        nodes.Add(newNode);
    }

    public WebNode GetWebNode(int id)
    {
        foreach (WebNode node in nodes)
        {
            if (node.id == id)
            {
                return node;
            }
        }

        return null;
    }

    public WebNode GetWebNode(string name)
    {
        foreach (WebNode node in nodes)
        {
            if (node.name.Equals(name))
            {
                return node;
            }
        }

        return null;
    }
}

