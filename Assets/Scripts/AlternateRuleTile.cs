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

        return base.RuleMatches(rule, position, tilemap, ref transform);
    }

    public static int GetState(Tilemap tilemap, Vector3Int gridPos)
    {
        GridInformation gridInfo = tilemap.GetComponent<GridInformation>();

        if (!gridInfo)
        {
            Debug.LogError("No GridInformation on tilemap " + tilemap.ToString());
        }

        return gridInfo.GetPositionProperty(gridPos, gridInfoKey, int.MinValue);
    }

    public static void SetState(Tilemap tilemap, Vector3Int gridPos, int stateValue)
    {
        GridInformation gridInfo = tilemap.GetComponent<GridInformation>();

        if (!gridInfo)
        {
            Debug.LogError("No GridInformation on tilemap " + tilemap.ToString());
        }

        gridInfo.SetPositionProperty(gridPos, gridInfoKey, stateValue);
        tilemap.RefreshTile(gridPos);
    }
}