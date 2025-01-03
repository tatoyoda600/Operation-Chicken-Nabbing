using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "2D/Tiles/Alternate Rule Tile")]
public class AlternateRuleTile : RuleTile
{
    public enum DoorStates
    {
        Closed = 0,
        Open = 1,
        Locked = 2
    }
    public enum LightStates
    {
        Dark = 0,
        Light = 1
    }

    public const string gridInfoKey = "AlternateRuleTile_State";
    readonly static Dictionary<ITilemap, GridInformation> gridInfoMap = new Dictionary<ITilemap, GridInformation>();
    readonly static Dictionary<string, AlternateRuleValue> altRuleValueMap = new Dictionary<string, AlternateRuleValue>();

    public static void ClearMaps()
    {
        gridInfoMap.Clear();
        altRuleValueMap.Clear();
    }

    internal static GridInformation GetGridInfo(ITilemap tilemap)
    {
        GridInformation gridInfo;
        if (!gridInfoMap.TryGetValue(tilemap, out gridInfo) || gridInfo == null)
        {
            gridInfo = tilemap.GetComponent<GridInformation>();
            gridInfoMap[tilemap] = gridInfo;
        }
        return gridInfo;
    }

    internal static AlternateRuleValue GetAltRuleValue(GameObject obj)
    {
        AlternateRuleValue altRuleValue;
        if (!altRuleValueMap.TryGetValue(obj.name, out altRuleValue))
        {
            altRuleValue = obj.GetComponent<AlternateRuleValue>();
            altRuleValueMap[obj.name] = altRuleValue;
        }
        return altRuleValue;
    }

    public override bool RuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform)
    {
        GridInformation gridInfo = tilemap.GetComponent<GridInformation>();

        if (gridInfo && rule.m_GameObject)
        {
            int stateValue = gridInfo.GetPositionProperty(position, gridInfoKey, int.MinValue);

            if (stateValue > int.MinValue)
            {
                AlternateRuleValue altRuleValue = GetAltRuleValue(rule.m_GameObject);

                if (altRuleValue && altRuleValue.value != stateValue)
                {
                    return false;
                }
            }
        }

        return BaseRuleMatches(rule, position, tilemap, ref transform);
    }

    /*
    public override bool RuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform)
    {
        GridInformation gridInfo = tilemap.GetComponent<GridInformation>();

        if (gridInfo)
        {
            int stateValue = gridInfo.GetPositionProperty(position, gridInfoKey, int.MinValue);

            if (stateValue > int.MinValue)
            {
                AlternateRuleValue altRuleValue = rule.m_GameObject?.GetComponent<AlternateRuleValue>();

                if (altRuleValue && altRuleValue.value != stateValue)
                {
                    return false;
                }
            }
        }

        return BaseRuleMatches(rule, position, tilemap, ref transform);
    }
    */

    public bool BaseRuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform)
    {
        return base.RuleMatches(rule, position, tilemap, ref transform);
    }

    public static int GetState(Tilemap tilemap, Vector3Int gridPos)
    {
        return GetGridInfo(tilemap)?.GetPositionProperty(gridPos, gridInfoKey, int.MinValue) ?? int.MinValue;
    }

    public static void SetState(Tilemap tilemap, Vector3Int gridPos, int stateValue)
    {
        GetGridInfo(tilemap)?.SetPositionProperty(gridPos, gridInfoKey, stateValue);
        tilemap.RefreshTile(gridPos);
    }

    public static void SetStateNoRefresh(Tilemap tilemap, Vector3Int gridPos, int stateValue)
    {
        GetGridInfo(tilemap)?.SetPositionProperty(gridPos, gridInfoKey, stateValue);
    }
}