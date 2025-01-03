using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class RoomDictionary : MonoBehaviour
{
    public enum NodeType
    {
        Room,
        Interaction
    }

    [System.Serializable]
    public class NodeData
    {
        public string nodeName;
        public NodeType type;
        public string unlockKey;
        [HideInInspector]
        public Vector2Int centerCell;
        [HideInInspector]
        public List<Vector2Int> nodeCells;
        [HideInInspector]
        public PathWeb.WebNode webNode;
    }

    [System.Serializable]
    public class NodeTilemap
    {
        public NodeType type;
        public Tilemap tilemap;
        public bool showName;
    }

    public PathWeb pathWeb;
    public TileBase lightTile;
    public TileBase darkTile;
    public List<NodeTilemap> tilemaps = new List<NodeTilemap>();
    public List<NodeData> nodes = new List<NodeData>();

#if UNITY_EDITOR
    [InspectorButton("Generate Node List")]
    private void GenerateNodeList()
    {
        foreach (PathWeb.WebNode webNode in pathWeb.nodes)
        {
            bool exists = false;
            foreach (NodeData nodeData in nodes)
            {
                if (nodeData.nodeName.Equals(webNode.name))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                nodes.Add(new NodeData() { nodeName = webNode.name, webNode = webNode });
            }
        }
        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    [InspectorButton("Calculate Node Tiles")]
    private void CalculateNodes()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            PathWeb.WebNode node = pathWeb.GetWebNode(nodes[i].nodeName);
            nodes[i].webNode = node;
            if (node != null)
            {
                Tilemap tilemap = null;
                foreach (NodeTilemap ntm in tilemaps)
                {
                    if (ntm.type == nodes[i].type)
                    {
                        tilemap = ntm.tilemap;
                    }
                }

                if (tilemap)
                {
                    nodes[i].centerCell = (Vector2Int)tilemap.WorldToCell(node.position);
                    nodes[i].nodeCells = new List<Vector2Int>(GetNeighbors(tilemap, nodes[i].centerCell, new HashSet<Vector2Int>()));
                }
            }
            else
            {
                Debug.LogError("Couldn't find node " + nodes[i].nodeName);
            }
        }

        foreach (NodeTilemap ntm in tilemaps)
        {
            Tilemap tilemap = ntm.tilemap;

            if (tilemap && ntm.showName)
            {
                DestroyImmediate(gameObject.transform.Find("Canvas_" + tilemap.name)?.gameObject);

                GameObject canvasGo = new GameObject("Canvas_" + tilemap.name, typeof(Canvas));
                canvasGo.transform.SetParent(gameObject.transform);
                Canvas canvas = canvasGo.GetComponent<Canvas>();
                canvas.sortingOrder = 2;

                Vector3 min = tilemap.GetCellCenterWorld(tilemap.cellBounds.min - new Vector3Int(1, 1, 0));
                Vector3 max = tilemap.GetCellCenterWorld(tilemap.cellBounds.max);
                RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
                canvasRect.sizeDelta = max - min;
                canvasRect.position = tilemap.CellToWorld((tilemap.cellBounds.min + tilemap.cellBounds.max) / 2);


                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].type == ntm.type)
                    {
                        GameObject textGo = new GameObject(nodes[i].nodeName + "_Text", typeof(TextMeshProUGUI));
                        textGo.transform.SetParent(canvasGo.transform);
                        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
                        text.text = nodes[i].nodeName;
                        text.enableAutoSizing = true;
                        text.fontSizeMin = 0;

                        RectTransform textRect = textGo.GetComponent<RectTransform>();
                        textRect.sizeDelta = tilemap.cellSize * 1.5f;
                        textRect.position = tilemap.GetCellCenterWorld((Vector3Int)nodes[i].centerCell);
                    }
                }
            }
        }
        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    HashSet<Vector2Int> GetNeighbors(Tilemap tilemap, Vector2Int gridPos, HashSet<Vector2Int> neighbors)
    {
        if (!neighbors.Contains(gridPos) && tilemap.GetSprite((Vector3Int)gridPos) != null)
        {
            neighbors.Add(gridPos);
            neighbors.UnionWith(GetNeighbors(tilemap, gridPos + Vector2Int.up, neighbors));
            neighbors.UnionWith(GetNeighbors(tilemap, gridPos + Vector2Int.right, neighbors));
            neighbors.UnionWith(GetNeighbors(tilemap, gridPos + Vector2Int.down, neighbors));
            neighbors.UnionWith(GetNeighbors(tilemap, gridPos + Vector2Int.left, neighbors));
        }

        return neighbors;
    }
#endif

    public NodeData GetNode(string nodeName)
    {
        foreach (NodeData node in nodes)
        {
            if (node.nodeName.Equals(nodeName))
            {
                return node;
            }
        }

        return null;
    }

    public string InNode(Vector3Int gridPos)
    {
        foreach (NodeData node in nodes)
        {
            if (node.nodeCells.Contains((Vector2Int)gridPos))
            {
                return node.nodeName;
            }
        }

        return null;
    }

    public Vector3Int? FurthestPointInNode(string nodeName, Vector2 dir)
    {
        NodeData node = GetNode(nodeName);

        if (node != null)
        {
            Vector3Int furthestPoint = (Vector3Int)node.centerCell;

            if (dir != Vector2.zero)
            {
                Vector2 normalizedDir = dir.normalized;
                float furthestDist = 0;
                foreach (Vector2Int cell in node.nodeCells)
                {
                    float dist = Vector2.Dot(normalizedDir, cell - node.centerCell);
                    if (dist > furthestDist)
                    {
                        furthestDist = dist;
                        furthestPoint = (Vector3Int)cell;
                    }
                }
            }

            return furthestPoint;
        }

        return null;
    }

    public void DarkenRoom(string roomName)
    {
        NodeData node = GetNode(roomName);
        if (node != null)
        {
            pathWeb.ChangeNodeState(roomName, false);
            ChangeRoomLight(node, false);
        }
    }

    public void BrightenRoom(string roomName)
    {
        NodeData node = GetNode(roomName);
        if (node != null)
        {
            pathWeb.ChangeNodeState(roomName, true);
            ChangeRoomLight(node, true);
            TimeManager.instance.TriggerGuardsInRoom(roomName);
        }
    }

    void ChangeRoomLight(NodeData node, bool light)
    {
        foreach (NodeTilemap ntm in tilemaps)
        {
            if (ntm.type == NodeType.Room)
            {
                Tilemap tilemap = ntm.tilemap;
                GridInformation gridInfo = tilemap.GetComponent<GridInformation>();
                if (gridInfo)
                {
                    foreach (Vector3Int cell in node.nodeCells)
                    {
                        gridInfo.SetPositionProperty(cell, AlternateRuleTile.gridInfoKey, light ? (int)AlternateRuleTile.LightStates.Light : (int)AlternateRuleTile.LightStates.Dark);
                    }
                }
                else
                {
                    Debug.LogWarning("Setting Tiles manually");
                    foreach (Vector3Int cell in node.nodeCells)
                    {
                        tilemap.SetTile(cell, light ? lightTile : darkTile);
                    }
                }
                StartCoroutine(RefreshTilesAsync(tilemap, node.nodeCells));
            }
        }
    }

    public static IEnumerator RefreshTilesAsync(Tilemap tilemap, List<Vector2Int> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            Vector3Int cell = (Vector3Int)cells[i];
            tilemap.RefreshTile(cell);

            if (i % 5 == 4)
            {
                yield return null;
            }
        }
    }

    public void ChangeRoomTextColor(string roomName, Color color)
    {
        TextMeshProUGUI text = gameObject.transform.Find("Canvas_RoomMask")?.Find(roomName + "_Text")?.GetComponent<TextMeshProUGUI>();
        if (text)
        {
            text.color = color;
        }
    }
}
