using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FacilitySpawner))]
public class FacilitySpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {

        DrawDefaultInspector();

        FacilitySpawner updateScript = (FacilitySpawner)target;
        if (GUILayout.Button("Spawn"))
        {
            updateScript.Start();
        }
    }
}


[System.Serializable]
public class FacilityRoom
{ // Determine the kinds of rooms there are in the facility and their number
    public string roomName_ = "Room";
    public RoomData room;
    public int amount = 1;
}

public class FacilitySpawner : MonoBehaviour
{

    public static FacilitySpawner instance_;
    public FacilityData data_;
    public float roomSize_ = 5f;
    private List<FacilityRoom> facilityRooms_ = new List<FacilityRoom> { };
    private List<GameObject> facilityObjects_ = new List<GameObject> { };

    [SerializeField]
    private int roomsLeftToSpawn = 0;
    [SerializeField]
    private int facilityRooms = 0;
    [SerializeField]
    private int roomsUntilFacilityRoom = 0;

    public List<Vector2> coordinates_ = new List<Vector2> { };
    // Use this for initialization
    void Awake()
    {
        if (instance_ == null)
        {
            instance_ = this;
        }
    }
    public void Start()
    {
        // Null a few things
        facilityRooms_.Clear();
        facilityObjects_.Clear();
        coordinates_.Clear();
        StopAllCoroutines();
        roomsLeftToSpawn = data_.connectiveRoomAmount_;
        // Create lists, and set up rooms left to spawn
        foreach (FacilityRoom room in data_.requiredrooms_)
        {
            if (room.amount > 0)
            {
                // Have to add them as copies, otherwise it changes the facilityrooms in the scriptableobject lol
                facilityRooms_.Add(new FacilityRoom { roomName_ = room.roomName_, room = room.room, amount = room.amount });
                facilityRooms += room.amount;
            };
            roomsLeftToSpawn += facilityRooms;
        }
        StartCoroutine(SpawnFacility());

    }

    IEnumerator SpawnFacility()
    { // Spawns the facility, with one random facility room as the first and last placed

        FacilityRoom firstRoom = GetRandomFacilityRoom(true);
        Room FRRoom = SpawnRoom(firstRoom.room);
        facilityObjects_.Add(FRRoom.gameObject);
        coordinates_.Add(new Vector2(0, 0));
        roomsUntilFacilityRoom = roomsLeftToSpawn / facilityRooms;
        facilityRooms -= 1;
        roomsLeftToSpawn -= 1;

        // This is where the next room will be spawned
        Room previousRoom = FRRoom;
        Exits nextExit = Exits.NONE;
        RoomData nextRoom = null;

        while (facilityRooms > 0)
        {
            // Get the next random connective room that fits the previous exit
            nextExit = previousRoom.GetRandomFreeConnection();
            if (nextExit == Exits.NONE)
            {
                Debug.LogWarning("No more free exits, looking for other exits further back...");
                for (int i = 0; i < facilityObjects_.Count; i++)
                {
                    nextExit = facilityObjects_[i].GetComponent<Room>().GetRandomFreeConnection();
                    if (nextExit != Exits.NONE)
                    {
                        previousRoom = facilityObjects_[i].GetComponent<Room>();
                        break;
                    }
                }
                if (nextExit == Exits.NONE)
                {
                    Debug.LogWarning("Couldn't find any more free exits. Oh well");
                    break;
                }
            }
            if (roomsUntilFacilityRoom <= 0)
            {
                FacilityRoom randomFC = GetRandomFacilityRoom();
                if (randomFC.room.ContainsExit(GetOppositeExit(nextExit)))
                {
                    nextRoom = randomFC.room;
                    facilityRooms -= 1;
					CountDownFacilityList(randomFC, 1);
                    roomsUntilFacilityRoom = roomsLeftToSpawn / facilityRooms;
                }
				else {
					nextRoom = GetRandomConnectiveRoom(GetOppositeExit(randomFC.room.GetRandomExit()));
				}
            }
            else
            {
                nextRoom = GetRandomConnectiveRoom(GetOppositeExit(nextExit));
            }
            if (nextRoom != null)
            {
                // Spawn it, and connect it to the previous room
                Room nextRoomRoom = SpawnRoom(nextRoom, previousRoom, GetOppositeExit(nextExit));
                if (nextRoomRoom != null)
                {
                    //Debug.Log(returnRoomGO);
                    if (previousRoom != null)
                    {
                        if (previousRoom.AttemptConnectRoom(nextRoomRoom, nextExit, true))
                        {
                            previousRoom = nextRoomRoom;
                            facilityObjects_.Add(nextRoomRoom.gameObject);
                            roomsLeftToSpawn -= 1;
                            roomsUntilFacilityRoom -= 1;
                            coordinates_.Add(GetNewCoordinates(coordinates_[coordinates_.Count - 1], nextExit));
                        }
                        else
                        {
                            Destroy(nextRoomRoom.gameObject);
                        }
                    }
                };
            }
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("FINISHED SPAWNING FACILITY!");
    }

    Room SpawnRoom(RoomData nextRoomData, Room previousRoom = null, Exits exit = Exits.NONE)
    { // Spawns a room and aligns it with the exit in question
        if (exit != Exits.NONE)
        { // Checks to make sure the exit works
            if (!nextRoomData.ContainsExit(exit))
            {
                Debug.LogWarning("Tried to connect room (" + nextRoomData + ") that does not contain exit " + GetOppositeExit(exit));
                return null;
            }
        };
        // This allows for the first room to spawn
        Vector3 oldPosition = transform.position;
        if (previousRoom != null)
        {
            oldPosition = previousRoom.transform.position;
        }
        GameObject returnRoomGO = GameObject.Instantiate(nextRoomData.prefab_, GetNewPosition(oldPosition, exit), Quaternion.identity);
        Room returnRoom = returnRoomGO.GetComponent<Room>();
        returnRoom.roomType_ = nextRoomData;
        return returnRoom;
    }

    Vector3 GetNewPosition(Vector3 oldPosition, Exits exit)
    {// Returns the necessary offset to move to match the exit

        switch (exit)
        {
            case Exits.NORTH:
                {
                    return oldPosition + new Vector3(roomSize_, 0f, 0f);
                }
            case Exits.SOUTH:
                {
                    return oldPosition - new Vector3(roomSize_, 0f, 0f);
                }
            case Exits.EAST:
                {
                    return oldPosition - new Vector3(0f, 0f, roomSize_);
                }
            case Exits.WEST:
                {
                    return oldPosition + new Vector3(0f, 0f, roomSize_);
                }
            default:
                {
                    return oldPosition;
                }
        }
    }
    public static Vector2 GetNewCoordinates(Vector2 oldCoordinates, Exits exit)
    {
        switch (exit)
        {
            case Exits.NORTH:
                {
                    return oldCoordinates + new Vector2(1, 0);
                }
            case Exits.SOUTH:
                {
                    return oldCoordinates - new Vector2(1, 0);
                }
            case Exits.EAST:
                {
                    return oldCoordinates - new Vector2(0, 1);
                }
            case Exits.WEST:
                {
                    return oldCoordinates + new Vector2(0, 1);
                }
            default:
                {
                    return oldCoordinates;
                }
        }
    }
    bool TryCoordinates(Exits exit)
    {// Sees if something is already occupying the proposed coordinates
        Vector2 newCoordinates = GetNewCoordinates(coordinates_[coordinates_.Count - 1], exit);
        if (coordinates_.Contains(newCoordinates))
        {
            return false;
        }
        else
        {
            return true;
        }
    }
    public static bool TryCoordinatesStatic(Exits exit)
    {
        return instance_.TryCoordinates(exit);
    }
    public static Exits GetOppositeExit(Exits exit)
    {// Gets the exit opposite of the one inputed.
        switch (exit)
        {
            case Exits.NORTH:
                {
                    return Exits.SOUTH;
                }
            case Exits.SOUTH:
                {
                    return Exits.NORTH;
                }
            case Exits.EAST:
                {
                    return Exits.WEST;
                }
            case Exits.WEST:
                {
                    return Exits.EAST;
                }
            default:
                {
                    return Exits.NONE;
                }
        }
    }

    FacilityRoom GetRandomFacilityRoom(bool countDownAndRemove = false)
    {// Gets a random facility room from all available ones
        if (facilityRooms_.Count > 0)
        {
            FacilityRoom randomFR = facilityRooms_[Random.Range(0, facilityRooms_.Count)];
            if (countDownAndRemove)
            { // Count down the amount and remove the room if there's now less than 1
				CountDownFacilityList(randomFR, 1);
            }
            return randomFR;
        }
        else
        {
            return null;
        }
    }

    void CountDownFacilityList(FacilityRoom target, int amount)
    {
        target.amount -= amount;
        if (target.amount < 1)
        {
            facilityRooms_.Remove(target);
            facilityRooms_.RemoveAll(item => item == null);
        }
    }
    RoomData GetRandomConnectiveRoom(Exits requiredExit)
    {// Gets a random connective room from available ones with all of the required exits

        List<RoomData> matchingRooms = new List<RoomData> { };
        RoomData returnRoom = null;

        foreach (RoomData data in data_.connectiveRooms_)
        {
            if (data.ContainsExit(requiredExit))
            {
                matchingRooms.Add(data);
            };
        }
        if (matchingRooms.Count > 0)
        {
            returnRoom = matchingRooms[Random.Range(0, matchingRooms.Count)];
        }
        Debug.Log("Returning room " + returnRoom + "with the exit " + requiredExit + " to fit " + GetOppositeExit(requiredExit));
        return returnRoom;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
