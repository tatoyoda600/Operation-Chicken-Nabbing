using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GuardScript : MonoBehaviour
{
    public enum MarkerFading
    {
        Normal = 0,
        Fading = 1,
        Faded = 2
    }
    MarkerFading curFadeStage = MarkerFading.Faded;
    readonly Color markerColor = new Color(210f / 255f, 45f / 255f, 45f / 255f, 1f);
    readonly Color fadingColor = new Color(210f / 255f, 45f / 255f, 45f / 255f, 0.6f);
    readonly Color fadedColor = new Color(210f / 255f, 90f / 255f, 45f / 255f, 0.3f);
    const int turnCooldown = 2;

    static Coroutine coroutine = null;

    public List<string> path;

    Animator anim;
    SpriteRenderer sprite;
    [HideInInspector]
    public int curNodeIndex = 0;
    bool loop = false;
    bool reversing = false;
    int delayCounter = 0;
    int lureCounter = 0;
    int lureNodeId = -1;

    private void Awake()
    {
        if (path.Count <= 0)
        {
            gameObject.SetActive(false);
            return;
        }

        anim = gameObject.GetComponent<Animator>();
        sprite = gameObject.GetComponent<SpriteRenderer>();
        loop = path[0].Equals(path[path.Count - 1]);

        PathWeb.WebNode curNode = GameManager.instance.pathWeb.GetWebNode(path[curNodeIndex]);
        MoveToPosition(curNode.position);

        GameManager.instance.RegisterGuard(this);
        ShowMarker(MarkerFading.Faded);
        TimeManager.instance.AfterTurnStart += GuardTurn;
    }

    void MoveToPosition(Vector3 position)
    {
        UnityEngine.Tilemaps.Tilemap groundTilemap = GameManager.instance.groundTilemap;
        Vector3 center = groundTilemap.GetCellCenterWorld(groundTilemap.WorldToCell(position));
        Vector3 shifted = groundTilemap.GetCellCenterWorld(groundTilemap.WorldToCell(position) + new Vector3Int(Random.Range(-1, 2), Random.Range(-1, 2), 0));
        gameObject.transform.position = (center + shifted) * 0.5f;
    }

    public void ShowMarker(MarkerFading fadeStage)
    {
        if (coroutine == null)
        {
            switch (fadeStage)
            {
                case MarkerFading.Normal:
                    sprite.enabled = true;
                    sprite.color = markerColor;
                    break;
                case MarkerFading.Fading:
                    sprite.enabled = true;
                    sprite.color = fadingColor;
                    break;
                case MarkerFading.Faded:
                    sprite.enabled = false;
                    break;
            }
            curFadeStage = fadeStage;
        }
    }

    public void CatchPlayer()
    {
        sprite.color = Color.white;
        sprite.enabled = true;
        anim.SetInteger("Face", Random.Range(0, 6));
        if (coroutine == null)
        {
            gameObject.GetComponent<AudioSource>().Play();
            coroutine = StartCoroutine(ResetScene());
        }
    }

    IEnumerator ResetScene()
    {
        yield return new WaitForSecondsRealtime(2);
        coroutine = null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void GuardTurn()
    {
        PathWeb.WebNode curNode = GameManager.instance.pathWeb.GetWebNode(path[curNodeIndex]);

        if (curNode != null)
        {
            // If the current node isn't locked/playerLocked (Visible or scanned)
            if (!curNode.locked && !curNode.playerLocked)
            {
                delayCounter++;
                if (delayCounter >= turnCooldown)
                {
                    delayCounter = 0;

                    int nextNodeIndex = curNodeIndex;
                    bool nextReversing = reversing;
                    bool success = false;
                    PathWeb.WebNode nextNode;

                    if (lureNodeId != -1)
                    {
                        // If currently on a lured node outside the path
                        nextNode = GameManager.instance.pathWeb.GetWebNode(path[nextNodeIndex]);
                        if (!nextNode.locked && !nextNode.playerLocked)
                        {
                            // If able to move back onto the path, do so, then tick down and reset lureNodeId
                            success = true;
                            lureCounter--;
                            lureNodeId = -1;
                        }
                    }
                    else
                    {
                        // If currently on the path
                        success = AdvanceGuard(ref nextNodeIndex, ref nextReversing);
                        nextNode = GameManager.instance.pathWeb.GetWebNode(path[nextNodeIndex]);

                        if (lureCounter <= 0)
                        {
                            success = HandleLuring(ref nextNode, ref nextNodeIndex, ref nextReversing) || success;
                        }
                        else if (success)
                        {
                            // If on lure cooldown and moved, tick down and reset lureNodeId
                            lureCounter--;
                        }
                    }

                    // Update the state of the current path node
                    curNodeIndex = nextNodeIndex;
                    reversing = nextReversing;

                    // If able to move, update the marker's display and position to the new node
                    if (success)
                    {
                        ShowMarker(nextNode.scanState);
                        MoveToPosition(nextNode.position);
                    }
                }
            }
        }
    }

    /// <summary> The function takes in references to the variables instead of updating anything in the object, so that it can be used to predict the next movement </summary>
    bool AdvanceGuard(ref int _curNodeIndex, ref bool _reversing)
    {
        // If looping in reverse, continue doing so
        _reversing = (loop && _reversing)
            // If not looping then go in reverse if either already reversing and haven't reached the start, or aren't reversing but have reached the end of the path
            || (!loop && ((_reversing && _curNodeIndex != 0) || (!_reversing && _curNodeIndex == path.Count - 1)));

        // Get the next node (If reversing go back 1, otherwise forward 1, and loop around if past the end or start)
        int nextNodeIndex = (_curNodeIndex + (_reversing ? -1 : 1)) % path.Count;
        if (nextNodeIndex < 0)
        {
            nextNodeIndex = path.Count - 1;
        }

        // If the next node is playerLocked (Visible or scanned)
        PathWeb.WebNode nextNode = GameManager.instance.pathWeb.GetWebNode(path[nextNodeIndex]);
        if (nextNode.locked || nextNode.playerLocked)
        {
            // Reverse away
            _reversing = !_reversing;
            return false;
        }
        else
        {
            // If the next node is an interaction, skip over it
            RoomDictionary.NodeData nextNodeData = GameManager.instance.roomDictionary.GetNode(nextNode.name);
            if (nextNodeData != null && nextNodeData.type == RoomDictionary.NodeType.Interaction)
            {
                int prevNodeIndex = _curNodeIndex;
                _curNodeIndex = nextNodeIndex;

                if (!AdvanceGuard(ref _curNodeIndex, ref _reversing))
                {
                    // If failed to skip over interaction node (Room on other side is active somehow)
                    // Undo modification of node index
                    _curNodeIndex = prevNodeIndex;
                    return false;
                }
                else
                {
                    // Skipped over the interaction node in the nested AdvanceGuard()
                    return true;
                }
            }
            else
            {
                // If the next node is free, go there
                _curNodeIndex = nextNodeIndex;
                return true;
            }
        }
    }

    /// <summary> This function is just a section of the GuardTurn function separated for cleanliness, so refs are used to modify the local variables in that function's scope </summary>
    bool HandleLuring(ref PathWeb.WebNode nextNode, ref int _curNodeIndex, ref bool _reversing)
    {
        bool success = false;
        if (nextNode.lured)
        {
            // If the next node is lured, continue as normal but start the lure cooldown
            lureCounter = Codec.LureAction.COOLDOWN_TURNS;
        }
        else
        {
            // Look around to check if any neighboring nodes are lured (and unlocked)
            PathWeb.WebNode lureNode = GameManager.instance.GetLuredNode(GameManager.instance.pathWeb.GetWebNode(path[curNodeIndex]));
            if (lureNode != null)
            {
                // If a lure node is found, start the cooldown, set it as the next node
                lureCounter = Codec.LureAction.COOLDOWN_TURNS;
                nextNode = lureNode;
                success = true;

                if (path.Contains(nextNode.name))
                {
                    // If the node is on the path, find the closest instance of it from the current node, and jump to that point on the path
                    int minDist = path.Count;
                    int index = -1;
                    for (int i = 0; i < path.Count; i++)
                    {
                        if (path[i].Equals(nextNode.name))
                        {
                            int tempDist;
                            if (loop)
                            {
                                // Gets the closest distance between 2 points on a loop
                                tempDist = Mathf.Min((i - curNodeIndex + path.Count) % path.Count, (curNodeIndex - i + path.Count) % path.Count);
                            }
                            else
                            {
                                tempDist = Mathf.Abs(i - curNodeIndex);
                            }

                            if (tempDist < minDist)
                            {
                                minDist = tempDist;
                                index = i;
                            }
                        }
                    }
                    _curNodeIndex = index;
                    _reversing = reversing;
                }
                else
                {
                    // If the node isn't on the path, keep the last path node state stored to return to
                    lureNodeId = nextNode.id;
                    _curNodeIndex = curNodeIndex;
                    _reversing = reversing;
                }
            }
        }

        return success;
    }

    public GameObject CreateFaded()
    {
        GameObject fadedGo = new GameObject(gameObject.name + "_Faded", typeof(SpriteRenderer));
        fadedGo.transform.SetParent(gameObject.transform.parent);
        fadedGo.transform.position = gameObject.transform.position;
        SpriteRenderer fadedSprite = fadedGo.GetComponent<SpriteRenderer>();
        fadedSprite.sprite = sprite.sprite;
        fadedSprite.color = fadedColor;
        fadedSprite.sortingOrder = sprite.sortingOrder;
        sprite.color = Color.white;
        return fadedGo;
    }
}
