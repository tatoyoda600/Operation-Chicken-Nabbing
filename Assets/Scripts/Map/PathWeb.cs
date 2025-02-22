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
        public string name;
        public Vector3 position;
        [HideInInspector]
        public List<int> connections = new List<int>();

        [HideInInspector]
        public bool playerLocked = false;
        [HideInInspector]
        public bool locked = false;
        [HideInInspector]
        public bool lured = false;
        [HideInInspector]
        public GuardScript.MarkerFading scanState = GuardScript.MarkerFading.Faded;

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
            /*Locking/Unlocking is already handled when darkening/brightening the rooms
            LockNode(currentWebNode, false, true);
            LockNode(node, true, true);
            LockVisuals(currentWebNode, false);
            LockVisuals(node, true);
            */
            currentWebNode = node;
        }
    }

    public void LockNode(int id, bool value, bool isPlayer = false) { LockNode(GetWebNode(id), value, isPlayer); }
    public void LockNode(string name, bool value, bool isPlayer = false) { LockNode(GetWebNode(name), value, isPlayer); }
    public void LockNode(WebNode node, bool value, bool isPlayer = false)
    {
        if (node != null)
        {
            if (value)
            {
                lockedNodes.Add(node);
            }
            else if ((isPlayer && !node.locked) || (!isPlayer && !node.playerLocked))
            {
                lockedNodes.Remove(node);
            }

            if (value)
            {
                node.locked = !isPlayer || node.locked;
                node.playerLocked = isPlayer || node.playerLocked;

                // Free to lock all connected nodes
                foreach (int connectionId in node.connections)
                {
                    WebNode connection = GetWebNode(connectionId);
                    if (connection != null)
                    {
                        connection.locked = !isPlayer || connection.locked;
                        connection.playerLocked = isPlayer || connection.playerLocked;
                    }
                }
            }
            else
            {
                node.locked = isPlayer && node.locked;
                node.playerLocked = !isPlayer && node.playerLocked;

                // Must ensure connected nodes aren't locked by other things before unlocking them
                foreach (int connectionId in node.connections)
                {
                    bool shared = false;
                    foreach (WebNode lockedNode in lockedNodes)
                    {
                        bool isSameLock = (!isPlayer && lockedNode.locked) || (isPlayer && lockedNode.playerLocked);
                        if (isSameLock && lockedNode.connections.Contains(connectionId))
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
                            connection.locked = isPlayer && connection.locked;
                            connection.playerLocked = !isPlayer && connection.playerLocked;
                        }
                    }
                }
            }
        }
    }

    public void LockVisuals(WebNode node, bool show)
    {
        // Prevent hiding locks when room is still locked
        if (show || (!node.locked && !node.playerLocked))
        {
            ZoneDictionary zoneDictionary = GameManager.instance.interactionTilemap.GetComponent<ZoneDictionary>();
            foreach (int conId in node.connections)
            {
                WebNode conWebNode = GameManager.instance.pathWeb.GetWebNode(conId);
                if (show || (!conWebNode.locked && !conWebNode.playerLocked))
                {
                    Vector3Int conPos = GameManager.instance.interactionTilemap.WorldToCell(conWebNode.position);
                    if (zoneDictionary.GetZone(conPos)?.zoneName == "Doors")
                    {
                        GameManager.instance.keyTilemap.SetTile(conPos, show ? GameManager.instance.lockTile : null);
                    }
                }
            }
        }
    }
}
