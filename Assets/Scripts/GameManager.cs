using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public Tilemap groundTilemap;
    public Tilemap collisionTilemap;
    public Tilemap interactionTilemap;
    public Tilemap keyTilemap;
    public Tilemap scanTilemap;

    public PathWeb pathWeb;
    public RoomDictionary roomDictionary;

    readonly List<GuardScript> guards = new List<GuardScript>();

    private void Awake()
    {
        if (instance)
        {
            Debug.LogError("Duplicate GameManagers");
            DestroyImmediate(this);
        }
        else
        {
            instance = this;
        }
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
}
