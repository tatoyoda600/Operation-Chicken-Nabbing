using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static PathWeb;

public class Codec : MonoBehaviour
{
    public abstract class RoomAction
    {
        public enum ActionType
        {
            Scan = 0,
            Lock = 1,
            Lure = 2,
            Cancel = 3
        }
        /// <summary> Contains the class Types associated with the values of ActionType </summary>
        public static readonly System.Type[] actionTypes = { typeof(ScanAction), typeof(LockAction), typeof(LureAction), typeof(CancelAction) };

        /// <summary> Type of action </summary>
        public readonly ActionType actionType;
        /// <summary> Turns action lasts for </summary>
        public readonly int turnDuration;
        /// <summary> Turns until action starts </summary>
        public readonly int turnDelay;
        /// <summary> Turns until action fades away </summary>
        public readonly int fadeTurns;

        public WebNode roomNode;
        public int turnCount = 0;

        public RoomAction(WebNode roomNode, ActionType actionType, int turnDuration = 0, int turnDelay = 0, int fadeTurns = 0)
        {
            this.actionType = actionType;
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
        public readonly List<GameObject> fadedObjects = new List<GameObject>();

        public ScanAction(Codec context, WebNode roomNode, List<GameObject> prevFadedObjects = null) : base(roomNode, ActionType.Scan, turnDuration: 3, turnDelay: 1, fadeTurns: 1)
        {
            // Start revealing the scan tiles
            ScanVisuals(context, true);

            if (prevFadedObjects != null)
            {
                fadedObjects = prevFadedObjects;
            }
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
                return false;
            }
        }

        public override bool TurnUpdate(Codec context)
        {
            turnCount--;
            if (turnCount == turnDuration)
            {
                context.source.clip = context.scanSound;
                context.source.Play();

                // Delete previous faded objects
                foreach (GameObject obj in fadedObjects)
                {
                    Destroy(obj);
                }
                fadedObjects.Clear();

                // Finish revealing the scan tiles
                GameManager.instance.ShowGuardMarker(roomNode, GuardScript.MarkerFading.Normal);
            }
            else if (turnCount == 1)
            {
                // Start fading
                GameManager.instance.ShowGuardMarker(roomNode, GuardScript.MarkerFading.Fading);
            }
            else if (turnCount == 0)
            {
                // Fade the scan tiles
                ScanVisuals(context, false);
                GameManager.instance.ShowGuardMarker(roomNode, GuardScript.MarkerFading.Faded);
                foreach (GuardScript guard in GameManager.instance.GetGuards(roomNode.name))
                {
                    fadedObjects.Add(guard.CreateFaded());
                }
            }
            
            // Has to be separate from the if else chain in case fade is 0, in which case turnCount == 0
            //  will disable the effect and this will clean up the fade that shouldn't exist
            if (turnCount <= -1 * fadeTurns)
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
                context.StartCoroutine(RoomDictionary.RefreshTilesAsync(GameManager.instance.scanTilemap, node.nodeCells, show ? turnDelay : 0.5f));
            }
        }
    }

    public class LockAction : RoomAction
    {
        public LockAction(Codec context, WebNode roomNode) : base(roomNode, ActionType.Lock, turnDuration: 3, turnDelay: 1, fadeTurns: 0)
        {
            LockVisuals(context, true);
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
                return false;
            }
        }

        public override bool TurnUpdate(Codec context)
        {
            turnCount--;
            if (turnCount == turnDuration)
            {
                // TODO: What sound to play?
                //  - Hydraulic door sound (Among Us door close?)
                //context.source.clip = context.scanSound;
                //context.source.Play();

                // Finish revealing the lock tiles
                GameManager.instance.pathWeb.LockNode(roomNode, true);
            }
            else if (turnCount == 0)
            {
                // Hide the scan tiles
                GameManager.instance.pathWeb.LockNode(roomNode, false);
                LockVisuals(context, false);
            }
            
            if (turnCount <= -1 * fadeTurns)
            {
                // Fully faded
                return false;
            }

            return true;
        }

        void LockVisuals(Codec context, bool show)
        {
            GameManager.instance.pathWeb.LockVisuals(roomNode, show);
        }
    }

    public class LureAction : RoomAction
    {
        public const int COOLDOWN_TURNS = 2;

        public LureAction(Codec context, WebNode roomNode) : base(roomNode, ActionType.Lure, turnDuration: 3, turnDelay: 1, fadeTurns: 0)
        {
            LureVisuals(context, true);
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
                return false;
            }
        }

        public override bool TurnUpdate(Codec context)
        {
            turnCount--;
            if (turnCount == turnDuration)
            {
                // TODO: What sound to play?
                //  - Metal pipes (Low volume and equalize the levels a bit)
                //context.source.clip = context.scanSound;
                //context.source.Play();

                // Finish revealing the lure tiles
                roomNode.lured = true;
            }
            else if (turnCount == 0)
            {
                // Hide the lure tiles
                roomNode.lured = false;
                LureVisuals(context, false);
            }

            if (turnCount <= -1 * fadeTurns)
            {
                // Fully faded
                return false;
            }

            return true;
        }

        void LureVisuals(Codec context, bool show)
        {
            // TODO:
        }
    }

    public class CancelAction : RoomAction
    {
        public CancelAction(Codec context, WebNode roomNode) : base(roomNode, ActionType.Cancel, turnDuration: 0, turnDelay: 0, fadeTurns: 0)
        {
            // TODO: What sound to play?
            foreach (RoomAction action in context.roomActions)
            {
                if (action.roomNode.id == roomNode.id && action.turnCount >= 1)
                {
                    action.turnCount = 1;
                    action.TurnUpdate(context);
                }
            }
        }

        public override bool RepeatAction()
        {
            // Delete existing room action
            return false;
        }

        public override bool TurnUpdate(Codec context)
        {
            // Delete
            return false;
        }
    }

    [System.Serializable]
    public struct CodecButton
    {
        public InputActionReference inputAction;
        public Button obj;
    }

    public bool doBonusScan = false;
    public GameObject buttons;
    public Sprite regularCodec;
    public Sprite disabledCodec;
    public GameObject neuro;
    public AudioClip scanSound;
    public Sprite markerSprite;
    public List<CodecButton> codecButtons;

    string curLetter = null;
    string curNumber = null;
    Button curLetterBtn = null;
    Button curNumberBtn = null;
    RoomAction.ActionType? curAction = null;
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

    public void InputSetup(PlayerInput playerInput)
    {
        EventSystem eventSystem = EventSystem.current;
        foreach (CodecButton codecBtn in codecButtons)
        {
            GameObject obj = codecBtn.obj.gameObject;
            playerInput.FindAction(codecBtn.inputAction.action.name).started += (_) =>
            {
                ExecuteEvents.Execute(obj, new PointerEventData(eventSystem), ExecuteEvents.pointerDownHandler);
            };
            playerInput.FindAction(codecBtn.inputAction.action.name).canceled += (_) =>
            {
                PointerEventData data = new PointerEventData(eventSystem);
                ExecuteEvents.Execute(obj, data, ExecuteEvents.pointerClickHandler);
                ExecuteEvents.Execute(obj, data, ExecuteEvents.pointerUpHandler);
            };
        }
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

    public void ClickLetter(Button button)
    {
        if (curLetterBtn != null)
        {
            ColorBlock colors = curLetterBtn.colors;
            Color normalColor = colors.normalColor;
            colors.normalColor = colors.selectedColor;
            colors.selectedColor = normalColor;
            curLetterBtn.colors = colors;
        }
        curLetterBtn = button;
        if (curLetterBtn != null)
        {
            ColorBlock colors = curLetterBtn.colors;
            Color normalColor = colors.normalColor;
            colors.normalColor = colors.selectedColor;
            colors.selectedColor = normalColor;
            curLetterBtn.colors = colors;
        }
    }

    public void InputLetter(string letter)
    {
        if (disabledTurns <= 0)
        {
            curLetter = letter;
            CallNeuro();
        }
    }

    public void ClickNumber(Button button)
    {
        if (curNumberBtn != null)
        {
            ColorBlock colors = curNumberBtn.colors;
            Color normalColor = colors.normalColor;
            colors.normalColor = colors.selectedColor;
            colors.selectedColor = normalColor;
            curNumberBtn.colors = colors;
        }
        curNumberBtn = button;
        if (curNumberBtn != null)
        {
            ColorBlock colors = curNumberBtn.colors;
            Color normalColor = colors.normalColor;
            colors.normalColor = colors.selectedColor;
            colors.selectedColor = normalColor;
            curNumberBtn.colors = colors;
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

    public void InputAction(string actionType)
    {
        if (disabledTurns <= 0 && System.Enum.TryParse(actionType, true, out RoomAction.ActionType temp))
        {
            curAction = temp;
            CallNeuro();
        }
    }

    void CallNeuro()
    {
        if (curLetter != null && curNumber != null && curAction != null)
        {
            string roomName = curLetter + curNumber;
            System.Type actionType = RoomAction.actionTypes[((int)curAction.Value)];
            ClickLetter(null);
            ClickNumber(null);
            curLetter = null;
            curNumber = null;
            curAction = null;

            WebNode node = GameManager.instance.pathWeb.GetWebNode(roomName);
            if (node != null)
            {
                // Since C# doesn't support using variables for generic types, reflection has to be used to get ExecuteRoomAction<actionType>
                System.Reflection.MethodInfo roomAction = this.GetType().GetMethod(nameof(ExecuteRoomAction), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).MakeGenericMethod(new[] { actionType });

                if (inTurn)
                {
                    // If currently in the middle of TurnUpdate, delay the execution until the end of it, to prevent race conditions
                    afterTurnAction = () => {
                        // Equivalent to 'ExecuteRoomAction<actionType>(node)'
                        roomAction.Invoke(this, new object[] { node });
                        disabledTurns = turnCooldown;
                        afterTurnAction = null;
                    };
                }
                else
                {
                    // Equivalent to 'ExecuteRoomAction<actionType>(node)'
                    roomAction.Invoke(this, new object[] { node });
                }

                if (actionType == typeof(CancelAction))
                {
                    // Don't disable the codec screen, just pop up Neuro
                    disabledTurns = -1;
                }
                else
                {
                    // Disable codec screen
                    for (int i = 0; i < buttons.transform.childCount; i++)
                    {
                        buttons.transform.GetChild(i).gameObject.SetActive(false);
                    }
                    image.sprite = disabledCodec;
                    disabledTurns = turnCooldown;
                }
                neuroAnim.gameObject.SetActive(true);
                neuroAnim.SetInteger("Face", Random.Range(0, 6));
            }
        }
    }

    void CodecCooldown()
    {
        if (disabledTurns < 0 || disabledTurns == 1)
        {
            ReenableCodec();
        }
        else if (disabledTurns > 1)
        {
            disabledTurns--;
        }
    }

    void ReenableCodec()
    {
        for (int i = 0; i < buttons.transform.childCount; i++)
        {
            buttons.transform.GetChild(i).gameObject.SetActive(true);
        }
        image.sprite = regularCodec;
        neuroAnim.gameObject.SetActive(false);
        disabledTurns = 0;
    }

    void ExecuteRoomAction<T>(WebNode node) where T : RoomAction
    {
        if (node != null)
        {
            if (doBonusScan && (typeof(T) == typeof(LockAction) || typeof(T) == typeof(LureAction)))
            {
                // TODO: Need to make sure the scan's sound doesn't interfere with the sound of the real action
                ExecuteRoomAction<ScanAction>(node);
            }

            int prevRoomActionIdx = -1;
            for (int i = 0; i < roomActions.Count; i++)
            {
                RoomAction roomAction = roomActions[i];
                if (roomAction.GetType() == typeof(T) && roomAction.roomNode.id == node.id)
                {
                    if (roomAction.RepeatAction())
                    {
                        // The existing action was updated
                        return;
                    }

                    // The existing action was cleared out
                    prevRoomActionIdx = i;
                    break;
                }
            }

            if (typeof(T) == typeof(ScanAction))
            {
                List<GameObject> prevFadedObjects = prevRoomActionIdx >= 0 ? ((ScanAction)roomActions[prevRoomActionIdx]).fadedObjects : null;
                roomActions.Add(new ScanAction(this, node, prevFadedObjects));
            }
            else if (typeof(T) == typeof(LockAction))
            {
                // TODO: Need to make sure the sounds of these don't interfere with each other
                roomActions.Add(new LockAction(this, node));
            }
            else if (typeof(T) == typeof(LureAction))
            {
                // TODO: Need to make sure the sounds of these don't interfere with each other
                roomActions.Add(new LureAction(this, node));
            }
            else if (typeof(T) == typeof(CancelAction))
            {
                roomActions.Add(new CancelAction(this, node));
            }

            if (prevRoomActionIdx >= 0)
            {
                roomActions.RemoveAt(prevRoomActionIdx);
            }
        }
    }
}
