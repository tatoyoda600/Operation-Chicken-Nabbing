using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "2D/Tiles/Sibling Rule Tile")]
public class SiblingRuleTile : AlternateRuleTile
{
    public enum SibingGroup
    {
        Wall
    }
    public SibingGroup siblingGroup;
    public bool ignoreSiblings = false;

    public override bool RuleMatch(int neighbor, TileBase other)
    {
        if (!ignoreSiblings)
        {
            other = (other as RuleOverrideTile)?.m_InstanceTile ?? other;
            return ((other as SiblingRuleTile)?.siblingGroup == siblingGroup) == (neighbor == TilingRuleOutput.Neighbor.This);
        }

        return base.RuleMatch(neighbor, other);
    }

    public override bool RuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform)
    {
        GridInformation gridInfo = GetGridInfo(tilemap);

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
}