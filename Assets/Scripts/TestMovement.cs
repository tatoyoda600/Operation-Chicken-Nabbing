using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class TestMovement : MonoBehaviour
{
    public PlayerInput playerInput;

    public string startingRoom = "A1";
    public float speed;
    
    public string endScene;
    public AudioClip doorSound;

    public Tilemap groundTilemap;
    public Tilemap collisionTilemap;
    public Tilemap interactionTilemap;
    public Tilemap keyTilemap;

    public PathWeb pathWeb;
    public RoomDictionary roomDictionary;

    string currentRoom;
    Vector2 movementDirection = Vector2.zero;
    Vector2 destination = Vector2.zero;
    bool noInput = true;
    List<Vector2Int> movementSequence = new List<Vector2Int>();
    Action onSequenceEnd = null;
    HashSet<string> keys = new HashSet<string>();
    Animator anim;
    SpriteRenderer sprite;

    private void Awake()
    {
        playerInput = new PlayerInput();
        anim = gameObject.GetComponent<Animator>();
        sprite = gameObject.GetComponent<SpriteRenderer>();
        sprite.flipX = true;

        RoomDictionary.NodeData startingNode = roomDictionary.GetNode(startingRoom);
        transform.position = groundTilemap.GetCellCenterWorld((Vector3Int)startingNode.centerCell);
        destination = transform.position;
        currentRoom = startingRoom;
        roomDictionary.BrightenRoom(startingRoom);

        playerInput.Interaction.Click.performed += (_) =>
        {
            Vector3 pos = Camera.main.ScreenToWorldPoint(playerInput.Interaction.MousePosition.ReadValue<Vector2>());
            Vector3Int gridPos = interactionTilemap.WorldToCell(pos);
            if (!noInput && interactionTilemap.HasTile(gridPos))
            {
                ZoneDictionary.Zone? interactionZone = interactionTilemap.GetComponent<ZoneDictionary>().GetZone(gridPos);
                RoomDictionary.NodeData interactionNode = new RoomDictionary.NodeData() { nodeName = null };
                foreach (RoomDictionary.NodeData node in roomDictionary.nodes)
                {
                    if (interactionZone.Value.gridPositions.Contains(node.centerCell))
                    {
                        interactionNode = node;
                        break;
                    }
                }

                if (interactionNode.nodeName != null && pathWeb.GetWebNode(interactionNode.nodeName)?.active == true)
                {
                    destination = interactionTilemap.GetCellCenterWorld(gridPos);
                }
            }
        };

        noInput = false;
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
        Vector2 movement = (destination - (Vector2)transform.position);
        if (movement.magnitude < 0.5f * groundTilemap.cellSize.x)
        {
            if (movementSequence.Count > 0)
            {
                destination = groundTilemap.GetCellCenterWorld((Vector3Int)movementSequence[0]);
                movement = (destination - (Vector2)transform.position);
                if (movement.magnitude < 0.5f * groundTilemap.cellSize.x)
                {
                    movementSequence.RemoveAt(0);
                    movement = Vector2.zero;
                    movementDirection = Vector2.zero;
                    destination = transform.position;

                    if (movementSequence.Count <= 0)
                    {
                        noInput = false;
                        if (onSequenceEnd != null)
                        {
                            onSequenceEnd();
                        }
                    }
                }
            }
            else
            {
                movement = Vector2.zero;
                movementDirection = Vector2.zero;
                destination = transform.position;
            }
        }
        movementDirection += movement.normalized * speed * Time.deltaTime;
        OnMove(movementDirection);
    }

    private void OnMove(Vector2 direction, bool allowSplit = true)
    {
        Vector2Int intDirection = Vector2Int.RoundToInt(direction);
        if (intDirection != Vector2Int.zero)
        {
            anim.SetInteger("MoveX", Math.Sign(direction.x));
            anim.SetInteger("MoveY", Math.Sign(direction.y));
            sprite.flipX = direction.x >= 0;

            movementDirection -= (Vector2)intDirection;
            Vector3Int gridPos = groundTilemap.WorldToCell(transform.position) + (Vector3Int)intDirection;

            ZoneDictionary.Zone? interactionZone = interactionTilemap.GetComponent<ZoneDictionary>().GetZone(gridPos);
            bool interacting = interactionZone.HasValue;

            if (!HasCollision(gridPos))
            {
                transform.position = groundTilemap.GetCellCenterWorld(gridPos);
                interacting = !noInput && interacting;
            }
            else if (!interacting && allowSplit)
            {
                // If there is a collision and no interaction, separate the movement into X and Y
                if (direction.y != 0)
                {
                    OnMove(direction * Vector2.up, false);
                }
                if (direction.x != 0)
                {
                    OnMove(direction * Vector2.right, false);
                }
            }

            // If attempting to interact with an interaction cell
            if (interacting)
            {
                movementDirection = Vector2.zero;
                destination = transform.position;

                // Find the RoomDictionary node for the cell
                RoomDictionary.NodeData interactionNode = new RoomDictionary.NodeData() { nodeName = null };
                foreach (RoomDictionary.NodeData node in roomDictionary.nodes)
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
                    // If it's a door
                    switch (interactionZone?.zoneName)
                    {
                        case "Keys":
                            // Add the node name to the list of keys
                            keys.Add(interactionNode.nodeName);
                            keyTilemap.SetTile((Vector3Int)interactionNode.centerCell, null);
                            GoToRoomCenter();
                            break;
                        case "Doors":
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
            }
        }
        else
        {
            anim.SetInteger("MoveX", 0);
            anim.SetInteger("MoveY", 0);
        }
    }

    void GoToRoomCenter()
    {
        RoomDictionary.NodeData node = roomDictionary.GetNode(currentRoom);
        if (node != null)
        {
            destination = groundTilemap.GetCellCenterWorld((Vector3Int)node.centerCell);
        }
    }

    private bool HasCollision(Vector3Int gridPos)
    {
        // When noInput, no collisions are checked. Make sure to move correctly
        return !noInput && (!groundTilemap.HasTile(gridPos) || (collisionTilemap.HasTile(gridPos) && collisionTilemap.GetSprite(gridPos) != null));
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
            AlternateRuleTile.SetState(collisionTilemap, (Vector3Int)zonePos, (int)AlternateRuleTile.DoorStates.Open);
        }

        if (centerNeightborCells.Count > 0)
        {
            // Separate the close and far ends of the door
            Vector2Int curCellPos = (Vector2Int)interactionTilemap.WorldToCell(transform.position);
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
                PathWeb.WebNode connection = pathWeb.GetWebNode(connectionId);
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
                roomDictionary.BrightenRoom(nextRoom.name);
                TimeManager.instance.FreezeTimer(true);

                // Queue up movement through the door
                movementSequence.Add(closestCell);
                movementSequence.Add(interactionNode.centerCell);
                // 1 further than the bounds of the door
                movementSequence.Add(furthestCell + (furthestCell - interactionNode.centerCell));

                onSequenceEnd = () => {
                    // Once queued movement ends, darken previous room and start moving towards room center
                    currentRoom = nextRoom.name;
                    roomDictionary.DarkenRoom(prevRoom.name);
                    destination = groundTilemap.GetCellCenterWorld(groundTilemap.WorldToCell(nextRoom.position));
                    pathWeb.MoveToConnection(currentRoom);
                    TimeManager.instance.FreezeTimer(false);

                    // Close door
                    foreach (Vector2Int zonePos in interactionZone.gridPositions)
                    {
                        AlternateRuleTile.SetState(collisionTilemap, (Vector3Int)zonePos, (int)AlternateRuleTile.DoorStates.Closed);
                    }
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
                AlternateRuleTile.SetState(collisionTilemap, (Vector3Int)zonePos, (int)SiblingRuleTile.DoorStates.Closed);
            }
        }
    }
}
