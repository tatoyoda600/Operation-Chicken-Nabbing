using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TestMovement : MonoBehaviour
{
    public string startingRoom = "A1";
    public float speed;
    public GameObject movementMarker;
    
    public string endScene;
    public AudioClip doorSound;
    public GameObject keyHolder;
    public Codec codec;

    const string CONSUMED_INTERACTION = "CONSUMED_INTERACTION";

    public PlayerInput playerInput { get; private set; }
    string currentRoom;
    Vector2 destination = Vector2.zero;
    bool noInput = true;
    List<Vector2Int> movementSequence = new List<Vector2Int>();
    Action onSequenceEnd = null;
    HashSet<string> keys = new HashSet<string>();
    Animator anim;
    SpriteRenderer sprite;
    Vector2 freePosition;
    float unitsPerPixel;
    // A tolerance to use when considering if a destination has been reached
    float tolerance;

    private void Awake()
    {
        playerInput = new PlayerInput();
        string keyMapping = PlayerPrefs.GetString(KeyMapper.KEY_MAPPINGS_PREF, null);
        if (!string.IsNullOrEmpty(keyMapping))
        {
            playerInput.asset.LoadBindingOverridesFromJson(keyMapping);
        }
        codec.InputSetup(playerInput);

        anim = gameObject.GetComponent<Animator>();
        sprite = gameObject.GetComponent<SpriteRenderer>();
        sprite.flipX = true;

        RoomDictionary.NodeData startingNode = GameManager.instance.roomDictionary.GetNode(startingRoom);
        transform.position = GameManager.instance.groundTilemap.GetCellCenterWorld((Vector3Int)startingNode.centerCell);
        destination = transform.position;
        currentRoom = startingRoom;
        GameManager.instance.roomDictionary.BrightenRoom(startingRoom);
        GameManager.instance.pathWeb.MoveToConnection(startingRoom);

        playerInput.Interaction.Click.performed += (_) =>
        {
            if (GameManager.instance.paused)
            {
                return;
            }

            Vector3 pos = Camera.main.ScreenToWorldPoint(playerInput.Interaction.MousePosition.ReadValue<Vector2>());
            Vector3Int gridPos = GameManager.instance.interactionTilemap.WorldToCell(pos);
            if (!noInput && GameManager.instance.interactionTilemap.HasTile(gridPos))
            {
                ZoneDictionary.Zone? interactionZone = GameManager.instance.interactionTilemap.GetComponent<ZoneDictionary>().GetZone(gridPos);
                RoomDictionary.NodeData interactionNode = new RoomDictionary.NodeData() { nodeName = null };
                foreach (RoomDictionary.NodeData node in GameManager.instance.roomDictionary.nodes)
                {
                    if (interactionZone.Value.gridPositions.Contains(node.centerCell))
                    {
                        interactionNode = node;
                        break;
                    }
                }

                if (interactionNode.nodeName != null && GameManager.instance.pathWeb.GetWebNode(interactionNode.nodeName)?.connections.Contains(GameManager.instance.pathWeb.currentWebNode.id) == true)
                {
                    destination = GameManager.instance.interactionTilemap.GetCellCenterWorld(gridPos);
                    movementMarker.SetActive(true);
                    movementMarker.transform.position = destination;
                }
            }
        };

        playerInput.Hotkeys.Pause.performed += (_) =>
        {
            GameManager.instance.PauseGame(!GameManager.instance.paused);
        };

        noInput = false;
        freePosition = transform.position;
        unitsPerPixel = 1.0f / sprite.sprite.pixelsPerUnit;
        tolerance = unitsPerPixel * 0.01f;
    }

    private void OnEnable()
    {
        playerInput.Enable();
    }

    private void OnDisable()
    {
        playerInput.Disable();
    }

    private void Update()
    {
        if (GameManager.instance.paused)
        {
            return;
        }

        OnMove((Vector2Int)GameManager.instance.groundTilemap.WorldToCell(destination));
    }

    private void OnMove(Vector2Int destinationCell)
    {
        // groundTilemap.cellSize -> unit width of cell (1.28 units)
        // sprite.sprite.pixelsPerUnit -> 25 pixels per unit
        // 1.0f / sprite.sprite.pixelsPerUnit -> 0.04 units per pixel
        // groundTilemap.cellSize * sprite.sprite.pixelsPerUnit -> 32 pixels per cell

        Vector2 destCellCenter = GameManager.instance.groundTilemap.GetCellCenterWorld((Vector3Int)destinationCell);
        Vector2 direction = (destCellCenter - freePosition).normalized;
        if (direction.magnitude > Mathf.Epsilon)
        {
            Vector2 cellSize = GameManager.instance.groundTilemap.cellSize;

            // Since we adjust the position by the grid's origin cell's center position, (0; 0) now represents that origin cell's center
            // By then dividing by the cell size, keeping the fractional, and re-multiplying by the cell size, we get a vector representing the position offset from a cell center
            Vector2 gridOrigin = GameManager.instance.groundTilemap.GetCellCenterWorld(GameManager.instance.groundTilemap.origin);
            Vector2 centerOffset = ((freePosition - gridOrigin) / cellSize);
            centerOffset = new Vector2(centerOffset.x % 1.0f, centerOffset.y % 1.0f) * cellSize;
            // This position offset can then be used along with the direction of movement to determine the necessary movement needed to line up with a cell center per axis
            Vector2 cellCenterProgress = new Vector2(
                Mathf.Abs(centerOffset.x + Mathf.Sign(direction.x) * cellSize.x) % cellSize.x,
                Mathf.Abs(centerOffset.y + Mathf.Sign(direction.y) * cellSize.y) % cellSize.y
            );
            // Due to float imprecision, cellCenterProgress can sometimes be functionally equivalent to the cellSize but not be wrapped by the modulo (%), so fix it manually
            cellCenterProgress = new Vector2(
                cellSize.x - cellCenterProgress.x < tolerance ? 0 : cellCenterProgress.x,
                cellSize.y - cellCenterProgress.y < tolerance ? 0 : cellCenterProgress.y
            );
            // How many steps need to be taken in the given direction to reach the next cell, manually setting infinity if a direction is too small in order to avoid division by 0
            Vector2 stepsToCell = new Vector2(
                Mathf.Abs(direction.x) < Mathf.Epsilon ? Mathf.Infinity : (cellSize.x - cellCenterProgress.x) / direction.x,
                Mathf.Abs(direction.y) < Mathf.Epsilon ? Mathf.Infinity : (cellSize.y - cellCenterProgress.y) / direction.y
            );

            // By taking the smallest of movement the axis, we make sure to prioritize aligning with a cell center on some axis, ensuring alignment on at least 1 axis when moving
            if (stepsToCell.x < Mathf.Infinity && Mathf.Abs(stepsToCell.x) < Mathf.Abs(stepsToCell.y))
            {
                //Debug.Log("X Movement");
                // At this point collision needs to be checked, if that nearest cell center has collision, then the other axis of movement must be used
                // If the other axis also points towards a collision, then it is necessary to return to the room center to get unstuck
                if (!DoMovement(new Vector2Int(Math.Sign(direction.x), 0)) && !DoMovement(new Vector2Int(0, Math.Sign(direction.y))))
                {
                    GoToRoomCenter();
                    // Should also make sure that the room center isn't in the same direction, and if it is then just teleport to the room center instead
                    Vector2 centerDirection = (destination - (Vector2)transform.position);
                    if (Math.Sign(centerDirection.x) == Mathf.Sign(direction.x) && Math.Sign(centerDirection.y) == Mathf.Sign(direction.y))
                    {
                        freePosition = destination;
                    }
                }
            }
            else if (stepsToCell.y < Mathf.Infinity)
            {
                //Debug.Log("Y Movement");
                // At this point collision needs to be checked, if that nearest cell center has collision, then the other axis of movement must be used
                // If the other axis also points towards a collision, then it is necessary to return to the room center to get unstuck
                if (!DoMovement(new Vector2Int(0, Math.Sign(direction.y))) && !DoMovement(new Vector2Int(Math.Sign(direction.x), 0)))
                {
                    GoToRoomCenter();
                    // Should also make sure that the room center isn't in the same direction, and if it is then just teleport to the room center instead
                    Vector2 centerDirection = (destination - (Vector2)transform.position);
                    if (Math.Sign(centerDirection.x) == Mathf.Sign(direction.x) && Math.Sign(centerDirection.y) == Mathf.Sign(direction.y))
                    {
                        freePosition = destination;
                    }
                }
            }
        }
        else
        {
            if (movementSequence.Count > 0)
            {
                destination = GameManager.instance.groundTilemap.GetCellCenterWorld((Vector3Int)movementSequence[0]);
                movementMarker.SetActive(true);
                movementMarker.transform.position = GameManager.instance.groundTilemap.GetCellCenterWorld((Vector3Int)movementSequence[movementSequence.Count - 1]);
                if (destinationCell == (Vector2Int)GameManager.instance.groundTilemap.WorldToCell(destination))
                {
                    movementSequence.RemoveAt(0);
                    freePosition = transform.position;

                    if (movementSequence.Count <= 0)
                    {
                        noInput = false;
                        destination = transform.position;
                        movementMarker.SetActive(false);
                        onSequenceEnd?.Invoke();
                    }
                    else
                    {
                        destination = GameManager.instance.groundTilemap.GetCellCenterWorld((Vector3Int)movementSequence[0]);
                    }
                }
            }
            else
            {
                //Debug.Log("Default Reached (" + (destination - freePosition).magnitude + ")");
                freePosition = transform.position;
                destination = transform.position;
                movementMarker.SetActive(false);
            }
        }

        sprite.flipX = Math.Sign(direction.x) >= 0;
        anim.SetInteger("MoveX", Math.Sign(direction.x));
        anim.SetInteger("MoveY", Math.Sign(direction.y));
        Vector2 newPosition = new Vector2(Mathf.Round(freePosition.x / unitsPerPixel), Mathf.Round(freePosition.y / unitsPerPixel)) * unitsPerPixel;
        transform.position = newPosition;
        //Debug.Log("~~ (" + destCellCenter + " - " + freePosition + ").magnitude (" + (destCellCenter - freePosition).magnitude + ")");
    }

    private bool DoMovement(Vector2Int direction)
    {
        if (direction.magnitude < 1)
        {
            //Debug.Log("No Magnitude");
            return false;
        }

        // By using slightly over half of the cell size, the resulting position will be within the same tile if the center hasn't been reached yet
        Vector2 halfCellDir = ((Vector2)GameManager.instance.groundTilemap.cellSize * 0.5f + new Vector2(tolerance, tolerance)) * direction;
        Vector3Int destinationCell = GameManager.instance.groundTilemap.WorldToCell(freePosition + halfCellDir);
        //Debug.Log(freePosition + " | " + groundTilemap.WorldToCell(freePosition) + " || " + halfCellDir + " | " + destinationCell + " | " + groundTilemap.GetCellCenterWorld(destinationCell));
        if (HasCollision(destinationCell))
        {
            //Debug.Log("Collision");
            return DoInteraction(destinationCell);
        }

        Vector3Int curCell = GameManager.instance.groundTilemap.WorldToCell(freePosition);
        if (!noInput && GameManager.instance.interactionTilemap.HasTile(curCell))
        {
            //Debug.Log("Interaction");
            DoInteraction(curCell);
        }

        //Debug.Log("Movement");
        Vector2 destCellCenter = GameManager.instance.groundTilemap.GetCellCenterWorld(destinationCell);
        if ((destCellCenter - freePosition).magnitude <= speed * Time.deltaTime + tolerance)
        {
            //Reached destination
            //Debug.Log("Reached");
            freePosition = destCellCenter;
        }
        else
        {
            //Debug.Log("(" + destCellCenter + " - " + freePosition + ").magnitude (" + (destCellCenter - freePosition).magnitude + ") > " + (speed * Time.deltaTime));
            freePosition += speed * Time.deltaTime * (Vector2)direction;
        }

        return true;
    }

    private bool DoInteraction(Vector3Int gridPos)
    {
        // If interaction found
        ZoneDictionary.Zone? interactionZone = GameManager.instance.interactionTilemap.GetComponent<ZoneDictionary>().GetZone(gridPos);
        if (interactionZone.HasValue)
        {
            // Find the RoomDictionary node for the cell
            RoomDictionary.NodeData interactionNode = new RoomDictionary.NodeData() { nodeName = null };
            foreach (RoomDictionary.NodeData node in GameManager.instance.roomDictionary.nodes)
            {
                if (interactionZone.Value.gridPositions.Contains(node.centerCell))
                {
                    interactionNode = node;
                    break;
                }
            }

            // If the node exists and the interaction isn't locked
            if (interactionNode.nodeName != null && IsUnlocked(interactionNode.unlockKey))
            {
                //DoMovement((Vector2Int)(gridPos - groundTilemap.WorldToCell(freePosition)));
                //destination = groundTilemap.GetCellCenterWorld(gridPos);
                destination = transform.position;

                switch (interactionZone?.zoneName)
                {
                    case "Keys":
                        // Add the node name to the list of keys
                        keys.Add(interactionNode.nodeName);
                        keyHolder.transform.Find(interactionNode.nodeName).gameObject.SetActive(true);
                        GameManager.instance.keyTilemap.SetTile((Vector3Int)interactionNode.centerCell, null);
                        GameManager.instance.interactionTilemap.SetTile((Vector3Int)interactionNode.centerCell, null);
                        interactionNode.unlockKey = CONSUMED_INTERACTION;
                        GoToRoomCenter();
                        break;
                    case "Doors":
                        // If it's a door, play the door sound and navigate the door
                        AudioSource source = gameObject.GetComponent<AudioSource>();
                        source.clip = doorSound;
                        source.Play();
                        NavigateDoor(interactionZone.Value, interactionNode);
                        break;
                    case "Ending":
                        SceneManager.LoadScene(endScene);
                        break;
                    default:
                        Debug.Log("No zone name " + interactionZone?.zoneName);
                        break;
                }
            }
            else
            {
                GoToRoomCenter();
            }

            return true;
        }

        return false;
    }

    void GoToRoomCenter()
    {
        RoomDictionary.NodeData node = GameManager.instance.roomDictionary.GetNode(currentRoom);
        if (node != null)
        {
            destination = GameManager.instance.groundTilemap.GetCellCenterWorld((Vector3Int)node.centerCell);
            movementMarker.SetActive(true);
            movementMarker.transform.position = destination;
        }
    }

    private bool HasCollision(Vector3Int gridPos)
    {
        // When noInput, no collisions are checked. Make sure to move correctly
        return !noInput && (!GameManager.instance.groundTilemap.HasTile(gridPos) || (GameManager.instance.collisionTilemap.HasTile(gridPos) && GameManager.instance.collisionTilemap.GetSprite(gridPos) != null));
    }

    private bool IsUnlocked(string key)
    {
        return string.IsNullOrEmpty(key) || keys.Contains(key);
    }

    void NavigateDoor(ZoneDictionary.Zone interactionZone, RoomDictionary.NodeData interactionNode)
    {
        // Stop inputs
        noInput = true;

        // Open the door (While getting tiles on either end of the door center)
        List<Vector2Int> centerNeightborCells = new List<Vector2Int>();
        foreach (Vector2Int zonePos in interactionZone.gridPositions)
        {
            if (zonePos + Vector2.up == interactionNode.centerCell
                || zonePos + Vector2.right == interactionNode.centerCell
                || zonePos + Vector2.down == interactionNode.centerCell
                || zonePos + Vector2.left == interactionNode.centerCell
            )
            {
                centerNeightborCells.Add(zonePos);
            }
            AlternateRuleTile.SetStateNoRefresh(GameManager.instance.collisionTilemap, (Vector3Int)zonePos, (int)AlternateRuleTile.DoorStates.Open);
        }

        StartCoroutine(RoomDictionary.RefreshTilesAsync(GameManager.instance.collisionTilemap, interactionZone.gridPositions));
        if (centerNeightborCells.Count > 0)
        {
            // Separate the close and far ends of the door
            Vector2Int curCellPos = (Vector2Int)GameManager.instance.interactionTilemap.WorldToCell(transform.position);
            Vector2Int closestCell = centerNeightborCells[0];
            Vector2Int furthestCell = centerNeightborCells[0];
            foreach (var cell in centerNeightborCells)
            {
                if ((closestCell - curCellPos).magnitude > (cell - curCellPos).magnitude)
                {
                    closestCell = cell;
                }
                if ((furthestCell - curCellPos).magnitude < (cell - curCellPos).magnitude)
                {
                    furthestCell = cell;
                }
            }

            // Get the rooms for each end of the door
            PathWeb.WebNode prevRoom = null;
            PathWeb.WebNode nextRoom = null;
            foreach (int connectionId in interactionNode.webNode.connections)
            {
                PathWeb.WebNode connection = GameManager.instance.pathWeb.GetWebNode(connectionId);
                if (connection.name == currentRoom)
                {
                    prevRoom = connection;
                }
                else
                {
                    nextRoom = connection;
                }
            }

            if (prevRoom != null && nextRoom != null)
            {
                // Brighten up the room on the far end of the door
                GameManager.instance.roomDictionary.BrightenRoom(nextRoom.name);
                TimeManager.instance.FreezeTimer(true);

                // Queue up movement through the door
                movementSequence.Add(closestCell);
                movementSequence.Add(interactionNode.centerCell);
                // 1 further than the bounds of the door
                movementSequence.Add(furthestCell + (furthestCell - interactionNode.centerCell));

                onSequenceEnd = () => {
                    // Once queued movement ends, darken previous room and start moving towards room center
                    currentRoom = nextRoom.name;
                    GameManager.instance.roomDictionary.DarkenRoom(prevRoom.name);
                    destination = GameManager.instance.groundTilemap.GetCellCenterWorld(GameManager.instance.groundTilemap.WorldToCell(nextRoom.position));
                    movementMarker.SetActive(true);
                    movementMarker.transform.position = destination;
                    GameManager.instance.pathWeb.MoveToConnection(currentRoom);
                    TimeManager.instance.FreezeTimer(false);

                    // Close door
                    foreach (Vector2Int zonePos in interactionZone.gridPositions)
                    {
                        AlternateRuleTile.SetStateNoRefresh(GameManager.instance.collisionTilemap, (Vector3Int)zonePos, (int)AlternateRuleTile.DoorStates.Closed);
                    }
                    StartCoroutine(RoomDictionary.RefreshTilesAsync(GameManager.instance.collisionTilemap, interactionZone.gridPositions));
                };
            }
            else
            {
                Debug.LogError("Door is missing a connection??!?");
            }
        }
        else
        {
            Debug.LogError("No neighboring cells??!?");
            noInput = false;
            foreach (Vector2Int zonePos in interactionZone.gridPositions)
            {
                AlternateRuleTile.SetStateNoRefresh(GameManager.instance.collisionTilemap, (Vector3Int)zonePos, (int)AlternateRuleTile.DoorStates.Closed);
            }
            StartCoroutine(RoomDictionary.RefreshTilesAsync(GameManager.instance.collisionTilemap, interactionZone.gridPositions));
        }
    }
}
