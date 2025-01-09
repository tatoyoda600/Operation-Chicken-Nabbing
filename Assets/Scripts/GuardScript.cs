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
    Color markerColor { get { return new Color(210f / 255f, 45f / 255f, 45f / 255f, 1f); } }
    Color fadingColor { get { return new Color(210f / 255f, 45f / 255f, 45f / 255f, 0.6f); } }
    Color fadedColor { get { return new Color(210f / 255f, 90f / 255f, 45f / 255f, 0.3f); } }
    const int fadeLifetime = 2;
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

        // If the current node isn't playerLocked (Visible or scanned)
        if (curNode?.playerLocked == false)
        {
            delayCounter++;
            if (delayCounter >= turnCooldown)
            {
                delayCounter = 0;
                AdvanceGuard();
            }
        }
    }

    bool AdvanceGuard()
    {
        // If looping in reverse, continue doing so
        reversing = (loop && reversing)
            // If not looping then go in reverse if either already reversing and haven't reached the start, or aren't reversing but have reached the end of the path
            || (!loop && ((reversing && curNodeIndex != 0) || (!reversing && curNodeIndex == path.Count - 1)));

        // Get the next node (If reversing go back 1, otherwise forward 1, and loop around if past the end or start)
        int nextNodeIndex = (curNodeIndex + (reversing ? -1 : 1)) % path.Count;
        if (nextNodeIndex < 0)
        {
            nextNodeIndex = path.Count - 1;
        }

        // If the next node is playerLocked (Visible or scanned)
        PathWeb.WebNode nextNode = GameManager.instance.pathWeb.GetWebNode(path[nextNodeIndex]);
        if (nextNode.playerLocked)
        {
            // Reverse away
            reversing = !reversing;
            return false;
        }
        else
        {
            // If the next node is an interaction, skip over it
            RoomDictionary.NodeData nextNodeData = GameManager.instance.roomDictionary.GetNode(nextNode.name);
            if (nextNodeData != null && nextNodeData.type == RoomDictionary.NodeType.Interaction)
            {
                int prevNodeIndex = curNodeIndex;
                curNodeIndex = nextNodeIndex;

                // If failed to skip over interaction node (Room on other side is active somehow)
                if (!AdvanceGuard())
                {
                    // Undo modification of node index
                    curNodeIndex = prevNodeIndex;
                    return false;
                }
            }
            else
            {
                // If the next node is free, go there
                curNodeIndex = nextNodeIndex;
                MoveToPosition(nextNode.position);
            }
        }
        return true;
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
