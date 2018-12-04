using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "FacilityData", order = 1)]
public class FacilityData : ScriptableObjectBase {

    // Rooms that have to exist
    public FacilityRoom[] requiredrooms_;
    // Allowed connective rooms
    public RoomData[] connectiveRooms_;
    // Ending rooms, i.e. rooms that cut off leftover empty endings (if an appropriate exit can be found)
    public RoomData[] endingRooms_;
    // Max size of facility (minimum = nr of required rooms)
    public int connectiveRoomAmount_;

    public Vector2 maxSizeEastWest_ = new Vector2(10,-10);
    public Vector2 maxSizeNorthSouth_ = new Vector2(10,-10);
}
