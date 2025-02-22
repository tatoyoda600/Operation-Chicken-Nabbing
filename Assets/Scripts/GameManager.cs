using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
#if UNITY_EDITOR
    static GameManager _instance;
    public static GameManager instance
    {
        get
        {
            _instance = _instance ?? FindObjectOfType<GameManager>();
            return _instance;
        }
        set
        {
            _instance = value;
        }
    }
#else
    public static GameManager instance;
#endif

    public Tilemap groundTilemap;
    public Tilemap collisionTilemap;
    public Tilemap interactionTilemap;
    public Tilemap keyTilemap;
    public Tilemap scanTilemap;

    public PathWeb pathWeb;
    public RoomDictionary roomDictionary;
    public GameObject pauseMenu;
    public TestMovement player;
    public TileBase lockTile;

    readonly List<GuardScript> guards = new List<GuardScript>();
    public bool paused
    {
        get;
        private set;
    } = false;

    private void Awake()
    {
#if !UNITY_EDITOR
        if (instance)
        {
            Debug.LogError("Duplicate GameManagers");
            DestroyImmediate(this);
        }
        else
        {
            instance = this;
        }
#endif
    }

    private void OnDestroy()
    {
        instance = null;
    }

    public void RegisterGuard(GuardScript guardObj)
    {
        guards.Add(guardObj);
    }

    public List<GuardScript> GetGuards(string roomName)
    {
        List<GuardScript> output = new List<GuardScript>();
        foreach (GuardScript guard in guards)
        {
            if (roomName.Equals(guard.path[guard.curNodeIndex]))
            {
                output.Add(guard);
            }
        }

        return output;
    }

    public void ShowGuardMarker(PathWeb.WebNode roomNode, GuardScript.MarkerFading fadeStage)
    {
        roomNode.scanState = fadeStage;
        foreach (GuardScript guard in guards)
        {
            if (roomNode.name.Equals(guard.path[guard.curNodeIndex]))
            {
                guard.ShowMarker(fadeStage);
            }
        }
    }

    public void TriggerGuardsInRoom(string roomName)
    {
        foreach (GuardScript guard in guards)
        {
            if (roomName.Equals(guard.path[guard.curNodeIndex]))
            {
                // Guard is in the room
                guard.CatchPlayer();
            }
        }
    }

    public void PauseGame(bool pause)
    {
        paused = pause;
        TimeManager.instance.FreezeTimer(pause);
        pauseMenu.SetActive(pause);
    }

    public PlayerInput GetInputSystem()
    {
        return player.playerInput;
    }

    public PathWeb.WebNode GetLuredNode(PathWeb.WebNode node)
    {
        PathWeb.WebNode luredNode = null;
        HashSet<int> processedIds = new HashSet<int>(node.connections);
        List<int> connectionIds = new List<int>(processedIds);
        while (connectionIds.Count > 0)
        {
            PathWeb.WebNode conNode = pathWeb.GetWebNode(connectionIds[0]);
            connectionIds.RemoveAt(0);

            if (!conNode.locked && !conNode.playerLocked)
            {
                // Only room nodes can be lured, a non-lured node is either a non-lured room or a node that could connect to a lured room
                if (conNode.lured)
                {
                    Vector3Int cellPos = groundTilemap.WorldToCell(conNode.position);
                    Vector3Int luredCellPos = cellPos + Vector3Int.right;
                    if (luredNode != null)
                    {
                        luredCellPos = groundTilemap.WorldToCell(conNode.position);
                    }
                    // Prioritize lured nodes on the left, using lower down as a tiebreaker
                    if (cellPos.x < luredCellPos.x || (cellPos.x == luredCellPos.x && cellPos.y < luredCellPos.y))
                    {
                        luredNode = conNode;
                    }
                }
                else if (roomDictionary.GetNode(conNode.name).type != RoomDictionary.NodeType.Room)
                {
                    // Node isn't a room, so its connections need to be processed in case any are lured rooms
                    foreach (int conId in conNode.connections)
                    {
                        if (processedIds.Add(conId))
                        {
                            connectionIds.Add(conId);
                        }
                    }
                }
            }
        }

        return luredNode;
    }
}
