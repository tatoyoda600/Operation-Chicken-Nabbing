using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PathWeb : MonoBehaviour
{
    [System.Serializable]
    public class WebNode
    {
        [HideInInspector]
        public int id = -1;
        [HideInInspector]
        public bool playerLocked = false;
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

    [HideInInspector]
    public WebNode currentWebNode;

    static readonly List<WebNode> lockedNodes = new List<WebNode>();
    static int nextId = -1;


#if UNITY_EDITOR
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
        newNode.position = (Vector2)SceneView.lastActiveSceneView.camera.transform.position;
        nodes.Add(newNode);
    }
#endif

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

    public void MoveToConnection(int id) { MoveToConnection(GetWebNode(id)); }
    public void MoveToConnection(string name) { MoveToConnection(GetWebNode(name)); }
    public void MoveToConnection(WebNode node)
    {
        if (node != null)
        {
            LockNode(currentWebNode, false);
            LockNode(node, true);
            currentWebNode = node;
        }
    }

    public void LockNode(int id, bool value) { LockNode(GetWebNode(id), value); }
    public void LockNode(string name, bool value) { LockNode(GetWebNode(name), value); }
    public void LockNode(WebNode node, bool value)
    {
        if (node != null)
        {
            if (value)
            {
                lockedNodes.Add(node);
            }
            else
            {
                lockedNodes.Remove(node);
            }

            node.playerLocked = value;
            if (value)
            {
                // Free to lock all connected nodes
                foreach (int connectionId in node.connections)
                {
                    WebNode connection = GetWebNode(connectionId);
                    if (connection != null)
                    {
                        connection.playerLocked = true;
                    }
                }
            }
            else
            {
                // Must ensure connected nodes aren't locked by other things before unlocking them
                foreach (int connectionId in node.connections)
                {
                    bool shared = false;
                    foreach (WebNode lockedNode in lockedNodes)
                    {
                        if (lockedNode.connections.Contains(connectionId))
                        {
                            shared = true;
                            break;
                        }
                    }

                    if (!shared)
                    {
                        WebNode connection = GetWebNode(connectionId);
                        if (connection != null)
                        {
                            connection.playerLocked = false;
                        }
                    }
                }
            }
        }
    }
}

