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

    public void ShowGuardMarker(string roomName, GuardScript.MarkerFading fadeStage)
    {
        foreach (GuardScript guard in guards)
        {
            if (roomName.Equals(guard.path[guard.curNodeIndex]))
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
        Debug.Log(pause ? "PAUSING" : "UNPAUSING");
        paused = pause;
        TimeManager.instance.FreezeTimer(pause);
        pauseMenu.SetActive(pause);
    }
}
