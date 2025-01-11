using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static PathWeb;

public class Codec : MonoBehaviour
{
    public abstract class RoomAction
    {
        /// <summary> Turns action lasts for </summary>
        public readonly int turnDuration;
        /// <summary> Turns until action starts </summary>
        public readonly int turnDelay;
        /// <summary> Turns until action fades away </summary>
        public readonly int fadeTurns;

        public WebNode roomNode;
        public int turnCount = 0;

        public RoomAction(WebNode roomNode, int turnDuration = 0, int turnDelay = 0, int fadeTurns = 0)
        {
            this.turnDuration = turnDuration;
            this.turnDelay = turnDelay;
            this.fadeTurns = fadeTurns;
            this.roomNode = roomNode;
            this.turnCount = turnDelay + turnDuration;
        }

        public abstract bool TurnUpdate(Codec context);
        public abstract bool RepeatAction();
    }

    public class ScanAction : RoomAction
    {
        List<GameObject> fadedObjects = new List<GameObject>();

        public ScanAction(Codec context, WebNode roomNode) : base(roomNode, turnDuration: 3, turnDelay: 1, fadeTurns: 1)
        {
            GameManager.instance.pathWeb.LockNode(roomNode, true);
            // Start revealing the scan tiles
            ScanVisuals(context, true);
        }

        public override bool TurnUpdate(Codec context)
        {
            turnCount--;
            if (turnCount == turnDuration)
            {
                context.source.clip = context.scanSound;
                context.source.Play();
                // Finish revealing the scan tiles
                GameManager.instance.ShowGuardMarker(roomNode.name, GuardScript.MarkerFading.Normal);
            }
            else if (turnCount == 1)
            {
                // Start fading
                GameManager.instance.ShowGuardMarker(roomNode.name, GuardScript.MarkerFading.Fading);
            }
            else if (turnCount == 0)
            {
                // Fade the scan tiles
                GameManager.instance.pathWeb.LockNode(roomNode, false);
                ScanVisuals(context, false);
                GameManager.instance.ShowGuardMarker(roomNode.name, GuardScript.MarkerFading.Faded);
                foreach (GuardScript guard in GameManager.instance.GetGuards(roomNode.name))
                {
                    fadedObjects.Add(guard.CreateFaded());
                }
            }
            else if (fadeTurns > 0 && turnCount <= -1 * fadeTurns)
            {
                // Fully faded
                foreach (GameObject obj in fadedObjects)
                {
                    Destroy(obj);
                }
                fadedObjects.Clear();
                return false;
            }

            return true;
        }

        public override bool RepeatAction()
        {
            if (turnCount > 0)
            {
                // Refresh turn count
                turnCount = Mathf.Max(turnCount, turnDuration);
                return true;
            }
            else
            {
                // Delete existing room action
                foreach (GameObject obj in fadedObjects)
                {
                    Destroy(obj);
                }
                fadedObjects.Clear();
                return false;
            }
        }

        void ScanVisuals(Codec context, bool show)
        {
            GridInformation gridInfo = GameManager.instance.scanTilemap.GetComponent<GridInformation>();
            if (gridInfo)
            {
                RoomDictionary.NodeData node = GameManager.instance.roomDictionary.GetNode(roomNode.name);
                foreach (Vector3Int cell in node.nodeCells)
                {
                    gridInfo.SetPositionProperty(cell, AlternateRuleTile.gridInfoKey, show ? (int)AlternateRuleTile.ScanStates.Scan : (int)AlternateRuleTile.ScanStates.NoScan);
                }
                context.StartCoroutine(RoomDictionary.RefreshTilesAsync(GameManager.instance.scanTilemap, node.nodeCells, show ? turnDelay : fadeTurns));
            }
        }
    }

    public GameObject buttons;
    public Sprite regularCodec;
    public Sprite disabledCodec;
    public GameObject neuro;
    public AudioClip scanSound;
    public Sprite markerSprite;

    string curLetter = null;
    string curNumber = null;
    int disabledTurns = 0;
    const int turnCooldown = 1;

    AudioSource source;
    Image image;
    Animator neuroAnim;
    readonly List<RoomAction> roomActions = new List<RoomAction>();
    bool inTurn = false;
    System.Action afterTurnAction;

    private void Awake()
    {
        source = gameObject.GetComponent<AudioSource>();
        image = gameObject.GetComponent<Image>();
        neuroAnim = neuro.GetComponent<Animator>();
        TimeManager.instance.OnTurnStart += TurnUpdate;
    }

    void TurnUpdate()
    {
        inTurn = true;
        CodecCooldown();

        for (int i = 0; i < roomActions.Count; i++)
        {
            RoomAction roomAction = roomActions[i];
            
            if (!roomAction.TurnUpdate(this))
            {
                roomActions.RemoveAt(i);
                i--;
            }
        }
        afterTurnAction?.Invoke();
        inTurn = false;
    }

    public void InputLetter(string letter)
    {
        if (disabledTurns <= 0)
        {
            curLetter = letter;
            CallNeuro();
        }
    }

    public void InputNumber(string number)
    {
        if (disabledTurns <= 0)
        {
            curNumber = number;
            CallNeuro();
        }
    }

    void CallNeuro()
    {
        if (curLetter != null && curNumber != null)
        {
            string roomName = curLetter + curNumber;
            curLetter = null;
            curNumber = null;

            WebNode node = GameManager.instance.pathWeb.GetWebNode(roomName);
            if (node != null)
            {
                if (inTurn)
                {
                    // If currently in the middle of TurnUpdate, delay the execution until the end of it, to prevent race conditions
                    afterTurnAction = () => {
                        ExecuteRoomAction<ScanAction>(node);
                        disabledTurns = turnCooldown;
                        afterTurnAction = null;
                    };
                }
                else
                {
                    ExecuteRoomAction<ScanAction>(node);
                }

                // Disable codec screen
                for (int i = 0; i < buttons.transform.childCount; i++)
                {
                    buttons.transform.GetChild(i).gameObject.SetActive(false);
                }
                image.sprite = disabledCodec;
                neuroAnim.gameObject.SetActive(true);
                neuroAnim.SetInteger("Face", Random.Range(0, 6));
                disabledTurns = turnCooldown;
            }
        }
    }

    void CodecCooldown()
    {
        if (disabledTurns > 0)
        {
            disabledTurns--;
            if (disabledTurns == 0)
            {
                for (int i = 0; i < buttons.transform.childCount; i++)
                {
                    buttons.transform.GetChild(i).gameObject.SetActive(true);
                }
                image.sprite = regularCodec;
                neuroAnim.gameObject.SetActive(false);
            }
        }
    }

    void ExecuteRoomAction<T>(WebNode node) where T : RoomAction
    {
        if (node != null)
        {
            for (int i = 0; i < roomActions.Count; i++)
            {
                RoomAction roomAction = roomActions[i];
                if (roomAction.GetType() == typeof(T) && roomAction.roomNode.id == node.id)
                {
                    if (!roomAction.RepeatAction())
                    {
                        // The existing action was cleared out
                        roomActions.RemoveAt(i);
                    }
                    else
                    {
                        // The existing action was updated
                        return;
                    }
                    break;
                }
            }

            if (typeof(T) == typeof(ScanAction))
            {
                roomActions.Add(new ScanAction(this, node));
            }
        }
    }
}
