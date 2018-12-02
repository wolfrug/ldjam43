using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{


    public RoomData roomType_;
    public Dictionary<Exits, Room> connectedRooms_ = new Dictionary<Exits, Room> { };
    public List<GameObject> connectedRoomsDebug_ = new List<GameObject> { };
    public List<Exits> connectedRoomsDebugDirs_ = new List<Exits> { };
    // Use this for initialization
    void Start()
    {

    }

    public bool AttemptConnectRoom(Room room, Exits exit, bool connectReverse = false)
    { //This should not happen, as we should check for exits first
        if (!roomType_.ContainsExit(exit))
        {
            Debug.LogWarning("Cannot connect " + room + " to exit " + exit + ", " + roomType_.name_ + " does not have it");
            return false;
        };
        if (connectedRooms_.ContainsKey(exit))
        { // Already contains the exit
            Debug.Log("Cannot connect to exit " + exit + ", " + this + " already has a connection. (connecting reverse: " + connectReverse + ")");
            return false;
        }
        else
        {
            connectedRooms_.Add(exit, room);
            if (connectReverse)
            {
                room.AttemptConnectRoom(this, FacilitySpawner.GetOppositeExit(exit));
            }
            connectedRoomsDebug_.Add(room.gameObject);
            connectedRoomsDebugDirs_.Add(exit);
            Debug.Log("Connected the exit " + exit + "of this room (" + this + ") to " + room + "(connectReverse: " + connectReverse + ")");
            return true;
        }
    }
    public Exits GetRandomFreeConnection()
    {
        List<Exits> freeExits = new List<Exits> { };
        foreach (Exits exit in roomType_.exits_)
        {
            if (!connectedRooms_.ContainsKey(exit))
            {
                // Check if the proposed new location already has something
                if (FacilitySpawner.TryCoordinatesStatic(exit))
                {
                    freeExits.Add(exit);
                };
            }
        }
        if (freeExits.Count > 0)
        {
            Exits randomExit = freeExits[Random.Range(0, freeExits.Count)];
            Debug.Log("Found free exit " + randomExit + " from room " + this);
            return randomExit;
        }
        else
        {
            Debug.Log("Found no free exits in room " + this);
            return Exits.NONE;
        }
    }

}
