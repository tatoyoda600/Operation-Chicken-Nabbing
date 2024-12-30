using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TimeManager : MonoBehaviour
{
    public static TimeManager instance;
    public float actionTime = 2;
    public float delayCount = 2;
    public PathWeb pathWeb;
    public RoomDictionary roomDictionary;
    public Tilemap groundTilemap;
    public AudioClip scanSound;

    float timeElapsed = 0;
    bool frozen = false;
    Animator anim;

    class Guard
    {
        public int curNodeIndex = 0;
        public bool loop = false;
        public bool reversing = false;
        public GuardScript obj;
        public int delayCounter = 0;
    }
    public class RoomScan
    {
        public const int delayTime = 1;
        public const int startScanTime = 3;
        public const int fadingTime = 1;
        public PathWeb.WebNode roomNode;
        public int scanTime = 0;
    }
    class DestroyObj
    {
        public GameObject obj;
        public int lifetime = 0;
    }

    List<Guard> guards = new List<Guard>();
    List<RoomScan> roomScans = new List<RoomScan>();
    List<DestroyObj> destroyObjects = new List<DestroyObj>();

    private void Awake()
    {
        if (instance)
        {
            DestroyImmediate(this);
        }
        else
        {
            instance = this;
            anim = gameObject.GetComponent<Animator>();
            FreezeTimer(false);
        }
    }

    public void RegisterGuard(GuardScript guardObj)
    {
        if (guardObj.path.Count > 0)
        {
            bool loop = guardObj.path[0].Equals(guardObj.path[guardObj.path.Count - 1]);
            Guard guard = new Guard() { loop = loop, obj = guardObj };
            guards.Add(guard);

            PathWeb.WebNode curNode = pathWeb.GetWebNode(guardObj.path[0]);
            MoveToPosition(guard, curNode.position);
        }
    }

    public void RegisterRoomScan(PathWeb.WebNode roomNode)
    {
        RoomScan roomScan = new RoomScan() { roomNode = roomNode, scanTime = RoomScan.startScanTime + RoomScan.delayTime };
        roomScans.Add(roomScan);
        roomDictionary.ChangeRoomTextColor(roomNode.name, new Color(101f / 255f, 119f / 255f, 181f / 255f));
    }

    public void RegisterDestroyObject(GameObject obj, int lifetime)
    {
        destroyObjects.Add(new DestroyObj() { obj = obj, lifetime = lifetime });
    }

    public void FreezeTimer(bool freeze)
    {
        anim.speed = freeze ? 0 : 1.0f / actionTime;
        frozen = freeze;
    }

    private void Update()
    {
        if (!frozen)
        {
            timeElapsed += Time.deltaTime;

            if (timeElapsed > actionTime)
            {
                DoActions();
                timeElapsed -= actionTime;
            }
        }
    }

    void DoActions()
    {
        anim.Play("Animation", -1, 0);

        for (int i = 0; i < destroyObjects.Count; i++)
        {
            DestroyObj destroyObject = destroyObjects[i];
            destroyObject.lifetime--;

            if (destroyObject.lifetime <= 0)
            {
                Destroy(destroyObject.obj);
                destroyObjects.RemoveAt(i);
                i--;
            }
        }

        for (int i = 0; i < roomScans.Count; i++)
        {
            RoomScan roomScan = roomScans[i];
            roomScan.scanTime--;

            if (roomScan.scanTime == RoomScan.startScanTime)
            {
                AudioSource source = gameObject.GetComponent<AudioSource>();
                source.clip = scanSound;
                source.Play();
                pathWeb.ChangeNodeState(roomScan.roomNode, true);
                roomDictionary.ChangeRoomTextColor(roomScan.roomNode.name, Color.green);
            }
            else if (roomScan.scanTime <= 0)
            {
                pathWeb.ChangeNodeState(roomScan.roomNode, false);
                roomScans.RemoveAt(i);
                roomDictionary.ChangeRoomTextColor(roomScan.roomNode.name, Color.white);
                i--;
            }
        }

        for (int i = 0; i < roomScans.Count; i++)
        {
            RoomScan roomScan = roomScans[i];
            pathWeb.ChangeNodeState(roomScan.roomNode, true);
        }
        pathWeb.ChangeNodeState(pathWeb.currentWebNode, true);

        for (int i = 0; i < guards.Count; i++)
        {
            Guard guard = guards[i];
            PathWeb.WebNode curNode = pathWeb.GetWebNode(guard.obj.path[guard.curNodeIndex]);
            bool showMarker = false;
            bool advance = false;
            GuardScript.MarkerFading fadeStage = GuardScript.MarkerFading.Faded;

            // If the current node isn't active (Visible or scanned)
            if (curNode != null)
            {
                if (!curNode.active)
                {
                    guard.delayCounter++;
                    if (guard.delayCounter == delayCount)
                    {
                        guard.delayCounter = 0;
                        advance = true;
                    }
                }
                else
                {
                    showMarker = true;
                    fadeStage = GuardScript.MarkerFading.Normal;
                    for (int j = 0; j < roomScans.Count; j++)
                    {
                        RoomScan roomScan = roomScans[j];
                        if (roomScan.roomNode.name.Equals(curNode.name))
                        {
                            if (roomScan.scanTime <= RoomScan.fadingTime)
                            {
                                fadeStage = GuardScript.MarkerFading.Fading;
                            }
                            break;
                        }
                    }
                }
            }

            guard.obj.ShowMarker(showMarker, fadeStage);
            if (advance)
            {
                AdvanceGuard(guard);
            }
        }
    }

    bool AdvanceGuard(Guard guard)
    {
        // If looping in reverse, continue doing so
        guard.reversing = (guard.loop && guard.reversing)
            // If not looping then go in reverse if either already reversing and haven't reached the start, or aren't reversing but have reached the end of the path
            || (!guard.loop && ((guard.reversing && guard.curNodeIndex != 0) || (!guard.reversing && guard.curNodeIndex == guard.obj.path.Count - 1)));

        // Get the next node (If reversing go back 1, otherwise forward 1, and loop around if past the end or start)
        int nextNodeIndex = (guard.curNodeIndex + (guard.reversing ? -1 : 1)) % guard.obj.path.Count;
        if (nextNodeIndex < 0)
        {
            nextNodeIndex = guard.obj.path.Count - 1;
        }

        // If the next node is active (Visible or scanned)
        PathWeb.WebNode nextNode = pathWeb.GetWebNode(guard.obj.path[nextNodeIndex]);
        if (nextNode.active)
        {
            // Reverse away
            guard.reversing = !guard.reversing;
            return false;
        }
        else
        {
            // If the next node is an interaction, skip over it
            RoomDictionary.NodeData nextNodeData = roomDictionary.GetNode(nextNode.name);
            if (nextNodeData != null && nextNodeData.type == RoomDictionary.NodeType.Interaction)
            {
                int prevNodeIndex = guard.curNodeIndex;
                guard.curNodeIndex = nextNodeIndex;

                // If failed to skip over interaction node (Room on other side is active somehow)
                if (!AdvanceGuard(guard))
                {
                    // Undo modification of node index
                    guard.curNodeIndex = prevNodeIndex;
                    return false;
                }
            }
            else
            {
                // If the next node is free, go there
                guard.curNodeIndex = nextNodeIndex;
                MoveToPosition(guard, nextNode.position);
            }
        }
        return true;
    }

    void MoveToPosition(Guard guard, Vector3 position)
    {
        Vector3 center = groundTilemap.GetCellCenterWorld(groundTilemap.WorldToCell(position));
        Vector3 shifted = groundTilemap.GetCellCenterWorld(groundTilemap.WorldToCell(position) + new Vector3Int(Random.Range(-1, 2), Random.Range(-1, 2), 0));
        guard.obj.transform.position = (center + shifted) * 0.5f;
    }

    public void TriggerGuardsInRoom(string roomName)
    {
        for (int i = 0; i < guards.Count; i++)
        {
            Guard guard = guards[i];

            if (roomName.Equals(guard.obj.path[guard.curNodeIndex]))
            {
                // Guard is in the room
                guard.obj.CatchPlayer();
            }
        }
    }
}
