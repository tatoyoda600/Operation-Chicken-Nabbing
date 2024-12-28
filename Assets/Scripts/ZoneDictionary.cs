using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ZoneDictionary : MonoBehaviour
{
    [System.Serializable]
    public struct ZoneData
    {
        public Sprite zoneSprite;
        public string zoneName;
    }

    [System.Serializable]
    public struct Zone
    {
        public string zoneName;
        public List<Vector3Int> gridPositions;
    }

    [SerializeField]
    List<ZoneData> zones;
    [SerializeField]
    Tilemap tilemap;

    public string GetZoneName(Vector3Int gridPos)
    {
        Sprite sprite = tilemap.GetSprite(gridPos);

        if (sprite)
        {
            foreach (ZoneData data in zones)
            {
                if (data.zoneSprite.Equals(sprite))
                {
                    return data.zoneName;
                }
            }
        }

        return null;
    }

    public Zone? GetZone(Vector3Int gridPos)
    {
        Sprite sprite = tilemap.GetSprite(gridPos);

        if (sprite)
        {
            foreach (ZoneData data in zones)
            {
                if (data.zoneSprite.Equals(sprite))
                {
                    Zone output = new Zone();
                    output.zoneName = data.zoneName;
                    output.gridPositions = new List<Vector3Int>(GetNeighbors(data.zoneName, gridPos, new HashSet<Vector3Int>()));
                    return output;
                }
            }
        }

        return null;
    }

    HashSet<Vector3Int> GetNeighbors(string zoneName, Vector3Int gridPos, HashSet<Vector3Int> neighbors)
    {
        if (!neighbors.Contains(gridPos) && GetZoneName(gridPos)?.Equals(zoneName) == true)
        {
            neighbors.Add(gridPos);
            neighbors.UnionWith(GetNeighbors(zoneName, gridPos + Vector3Int.up, neighbors));
            neighbors.UnionWith(GetNeighbors(zoneName, gridPos + Vector3Int.right, neighbors));
            neighbors.UnionWith(GetNeighbors(zoneName, gridPos + Vector3Int.down, neighbors));
            neighbors.UnionWith(GetNeighbors(zoneName, gridPos + Vector3Int.left, neighbors));
        }

        return neighbors;
    }
}
