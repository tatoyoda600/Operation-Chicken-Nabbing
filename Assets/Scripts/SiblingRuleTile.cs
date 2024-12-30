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
            if (other is RuleOverrideTile)
                other = (other as RuleOverrideTile).m_InstanceTile;

            switch (neighbor)
            {
                case TilingRule.Neighbor.This:
                    {
                        return other is SiblingRuleTile
                            && (other as SiblingRuleTile).siblingGroup == this.siblingGroup;
                    }
                case TilingRule.Neighbor.NotThis:
                    {
                        return !(other is SiblingRuleTile
                            && (other as SiblingRuleTile).siblingGroup == this.siblingGroup);
                    }
            }
        }

        return base.RuleMatch(neighbor, other);
    }

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
}