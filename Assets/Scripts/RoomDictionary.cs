using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;

[RequireComponent(typeof(Tilemap)), RequireComponent(typeof(PathWeb))]
public class RoomDictionary : MonoBehaviour
{
    [System.Serializable]
    public class RoomData
    {
        public string roomName;
        [HideInInspector]
        public Vector2Int centerCell;
        [HideInInspector]
        public List<Vector2Int> roomCells;
    }

    public List<RoomData> rooms = new List<RoomData>();

#if UNITY_EDITOR
    [InspectorButton("Calculate Room Tiles")]
    private void CalculateRooms()
    {
        Tilemap tilemap = gameObject.GetComponent<Tilemap>();
        PathWeb pathWeb = gameObject.GetComponent<PathWeb>();

        for (int i = 0; i < rooms.Count; i++)
        {
            PathWeb.WebNode node = pathWeb.GetWebNode(rooms[i].roomName);
            if (node != null)
            {
                rooms[i].centerCell = (Vector2Int)tilemap.WorldToCell(node.position);
                rooms[i].roomCells = new List<Vector2Int>(GetNeighbors(tilemap, rooms[i].centerCell, new HashSet<Vector2Int>()));
            }
            else
            {
                Debug.LogError("Couldn't find node " + rooms[i].roomName);
            }
        }

        DestroyImmediate(gameObject.transform.Find("Canvas")?.gameObject);

        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas));
        canvasGo.transform.SetParent(gameObject.transform);
        Canvas canvas = canvasGo.GetComponent<Canvas>();

        Vector3 min = tilemap.GetCellCenterWorld(tilemap.cellBounds.min - new Vector3Int(1, 1, 0));
        Vector3 max = tilemap.GetCellCenterWorld(tilemap.cellBounds.max);
        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = max - min;
        canvasRect.position = tilemap.CellToWorld((tilemap.cellBounds.min + tilemap.cellBounds.max) / 2);


        for (int i = 0; i < rooms.Count; i++)
        {
            GameObject textGo = new GameObject(rooms[i].roomName + "_Text", typeof(TextMeshProUGUI));
            textGo.transform.SetParent(canvasGo.transform);
            TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = rooms[i].roomName;
            text.enableAutoSizing = true;
            text.fontSizeMin = 0;

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.sizeDelta = tilemap.cellSize * 1.5f;
            textRect.position = tilemap.GetCellCenterWorld((Vector3Int)rooms[i].centerCell);
        }
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

    public RoomData GetRoom(string roomName)
    {
        foreach (RoomData room in rooms)
        {
            if (room.roomName.Equals(roomName))
            {
                return room;
            }
        }

        return null;
    }

    public string InRoom(Vector3Int gridPos)
    {
        foreach (RoomData room in rooms)
        {
            if (room.roomCells.Contains((Vector2Int)gridPos))
            {
                return room.roomName;
            }
        }

        return null;
    }

    public Vector3Int? FurthestPointInRoom(string roomName, Vector2 dir)
    {
        RoomData room = GetRoom(roomName);

        if (room != null)
        {
            Vector3Int furthestPoint = (Vector3Int)room.centerCell;

            if (dir != Vector2.zero)
            {
                Vector2 normalizedDir = dir.normalized;
                float furthestDist = 0;
                foreach (Vector2Int cell in room.roomCells)
                {
                    float dist = Vector2.Dot(normalizedDir, cell - room.centerCell);
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
}
