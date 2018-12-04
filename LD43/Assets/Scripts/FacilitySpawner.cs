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
    private List<Room> allRooms_ = new List<Room> { };

    [SerializeField]
    private int roomsSpawned = 0;
    [SerializeField]
    private int facilityRooms = 0;
    [SerializeField]
    private int roomsUntilFacilityRoom = 0;
    private bool nextRoomIsFacilityRoom = false;

    public Dictionary<Room, Vector2> coordinates_ = new Dictionary<Room, Vector2> { };
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
        allRooms_.Clear();
        facilityRooms = 0;
        roomsSpawned = 0;
        roomsUntilFacilityRoom = 0;
        nextRoomIsFacilityRoom = false;
        StopAllCoroutines();
        // Create lists, and set up rooms left to spawn
        foreach (FacilityRoom room in data_.requiredrooms_)
        {
            if (room.amount > 0)
            {
                // Have to add them as copies, otherwise it changes the facilityrooms in the scriptableobject lol
                facilityRooms_.Add(new FacilityRoom { roomName_ = room.roomName_, room = room.room, amount = room.amount });
                facilityRooms += room.amount;
            };
        }
        StartCoroutine(SpawnFacility());

    }

    void DespawnFacility(bool startAgain = false)
    {
        StopAllCoroutines();
        foreach (GameObject roomObj in facilityObjects_)
        {
            Destroy(roomObj);
        }
        if (startAgain)
        {
            Start();
        }
    }

    IEnumerator SpawnFacility()
    { // Spawns the facility, with one random facility room as the first and last placed
        Debug.LogWarning("SPAWNFACILITY COROUTINE RUNNING: THIS WARNING SHOULD ONLY HAPPEN ONCE");
        FacilityRoom firstRoom = GetRandomFacilityRoom(true);
        Room FRRoom = SpawnRoom(firstRoom.room);
        FRRoom.coordinate_ = new Vector2(0, 0);
        facilityObjects_.Add(FRRoom.gameObject);
        coordinates_.Add(FRRoom, new Vector2(0, 0));
        roomsUntilFacilityRoom = data_.connectiveRoomAmount_;
        facilityRooms -= 1;
        roomsSpawned += 1;

        // This is where the next room will be spawned
        Room previousRoom = FRRoom;
        Exits nextExit = Exits.NONE;
        RoomData nextRoom = null;

        while (facilityRooms > 0)
        {
            yield return new WaitForSeconds(0.1f);
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
                    if (facilityRooms <= 0)
                    {
                        Debug.LogWarning("Couldn't find any more free exits. Oh well");
                        break;
                    }
                    else
                    {// Fucked up - restart whole facility generation!
                        DespawnFacility(true);
                    }
                }
            }
            if (roomsUntilFacilityRoom <= 0)
            {
                FacilityRoom randomFC = GetRandomFacilityRoom();
                if (randomFC != null)
                {
                    if (randomFC.room.ContainsExit(GetOppositeExit(nextExit)))
                    {
                        nextRoom = randomFC.room;
                        nextRoomIsFacilityRoom = true;
                    }
                    else
                    {
                        nextRoom = GetRandomConnectiveRoom(GetOppositeExit(randomFC.room.GetRandomExit()));
                    }
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
                        /*if (previousRoom.AttemptConnectRoom(nextRoomRoom, nextExit, true))
                        {*/
                        coordinates_.Add(nextRoomRoom, GetNewCoordinates(coordinates_[previousRoom], nextExit));
                        previousRoom = nextRoomRoom;
                        facilityObjects_.Add(nextRoomRoom.gameObject);
                        roomsSpawned += 1;
                        if (nextRoomIsFacilityRoom)
                        { // Try to count it down, might not work because of...reasons. So check that first.
                            if (CountDownFacilityList(nextRoomRoom, 1))
                            {
                                facilityRooms -= 1;
                                nextRoomIsFacilityRoom = false;
                                if (facilityRooms > 0)
                                {
                                    roomsUntilFacilityRoom = data_.connectiveRoomAmount_;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        roomsUntilFacilityRoom -= 1;
                        /*}
                        else
                        {
                            //Destroy(nextRoomRoom.gameObject);
                        }*/
                    }
                };
            }

        }
        Debug.Log("FINISHED SPAWNING FACILITY!");
        allRooms_.Clear();
        foreach (KeyValuePair<Room, Vector2> pair in coordinates_)
        {
            allRooms_.Add(pair.Key);
        }
        // Attempt to finalize all connections between rooms
        foreach (Room room in allRooms_)
        {
            room.AttemptConnectAll(false);
            //yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("FINISHED CONNECTING ALL");
        // Attempts to add a blocker (if found) to each remaining empty exit

        foreach (Room room in allRooms_)
        {
            foreach (Exits exit in room.GetAllFreeExits())
            {
                //yield return new WaitForSeconds(0.1f);
                bool isTileEmpty = !coordinates_.ContainsValue(GetNewCoordinates(coordinates_[room], exit));
                RoomData randomBlockerData = GetRandomBlockerRoom(GetOppositeExit(exit), isTileEmpty);
                if (randomBlockerData != null)
                {
                    Room blockerRoom = SpawnRoom(randomBlockerData, room, GetOppositeExit(exit));
                    if (room.AttemptConnectRoom(blockerRoom, exit, true))
                    {
                        coordinates_.Add(blockerRoom, GetNewCoordinates(coordinates_[room], nextExit));
                        facilityObjects_.Add(blockerRoom.gameObject);
                        roomsSpawned += 1;
                    }
                    /*else
                    {
                        Destroy(blockerRoom.gameObject);
                    }*/
                }
            }
        }
        Debug.Log("FINISHED ADDING BLOCKERS");


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
        // Set room type, and room's new coordinates
        returnRoom.roomType_ = nextRoomData;
        if (previousRoom != null)
        {
            returnRoom.coordinate_ = GetNewCoordinates(coordinates_[previousRoom], GetOppositeExit(exit));
            returnRoom.gameObject.name = returnRoom.roomType_.name_ + "(" + returnRoom.coordinate_ + ")";
        }
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
    bool TryCoordinates(Room targetRoom, Exits exit)
    {// Sees if something is already occupying the proposed coordinates
        Vector2 newCoordinates;

        if (coordinates_.ContainsKey(targetRoom))
        {
            newCoordinates = GetNewCoordinates(coordinates_[targetRoom], exit);
        }
        else { return false; };
        if (coordinates_.ContainsValue(newCoordinates))
        {
            return false;
        }
        else
        {
            return true;
        }
    }
    public Room ReturnRoomAtCoordinates(Vector2 coordinates)
    {// return the room at the coordinates (or null if nothing is found)

        foreach (KeyValuePair<Room, Vector2> pair in coordinates_)
        {
            if (pair.Value == coordinates)
            {
                return pair.Key;
            }
        }
        return null;
    }
    public static bool TryCoordinatesStatic(Room targetRoom, Exits exit)
    {
        return instance_.TryCoordinates(targetRoom, exit);
    }

    public bool TryMaxSize(Vector2 testCoords)
    {// Checks if the coords fit within the max size as set in the data - if not, return false
        if (testCoords.x > data_.maxSizeNorthSouth_.x)
        {
            return false;
        }
        if (testCoords.x < data_.maxSizeNorthSouth_.y)
        {
            return false;
        }
        if (testCoords.y > data_.maxSizeEastWest_.x)
        {
            return false;
        }
        if (testCoords.y < data_.maxSizeEastWest_.y)
        {
            return false;
        }
        return true;
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
        Debug.LogWarning("Removed " + amount + " of Facility Room " + target.roomName_ + " Remaining: " + target.amount);
        if (target.amount < 1)
        {
            facilityRooms_.Remove(target);
            facilityRooms_.RemoveAll(item => item == null);
            Debug.LogWarning("Last room of type " + target.roomName_ + " placed!");
        }
    }
    bool CountDownFacilityList(Room target, int amount)
    {
        foreach (FacilityRoom room in facilityRooms_)
        {
            if (room.room == target.roomType_)
            {
                CountDownFacilityList(room, amount);
                return true;
            }
        }
        Debug.LogWarning("Tried to remove room " + target.roomType_ + " from Facility list, but target was not found");
        return false;
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
    RoomData GetRandomBlockerRoom(Exits requiredExit, bool tileEmpty = true)
    {
        // Gets a random blocked/end room from available ones with all of the required exits

        List<RoomData> matchingRooms = new List<RoomData> { };
        RoomData returnRoom = null;

        foreach (RoomData data in data_.endingRooms_)
        {
            if (data.ContainsExit(requiredExit))
            {
                // If tile is empty, we add any - otherwise only blockers that don't take up the whole space
                if (tileEmpty)
                {
                    matchingRooms.Add(data);
                }
                else if (!tileEmpty && !data.occupiesWholeTile)
                {
                    matchingRooms.Add(data);
                }
            };
        }
        if (matchingRooms.Count > 0)
        {
            returnRoom = matchingRooms[Random.Range(0, matchingRooms.Count)];
        }
        Debug.Log("Returning ENDING room " + returnRoom + "with the exit " + requiredExit + " to fit " + GetOppositeExit(requiredExit));
        return returnRoom;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
