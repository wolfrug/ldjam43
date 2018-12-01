using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "FacilityData", order = 1)]
public class FacilityData : ScriptableObjectBase {

    // Rooms that have to exist
    public FacilityRoom[] requiredrooms_;
    // Allowed connective rooms
    public RoomData[] connectiveRooms_;
    // Max size of facility (minimum = nr of required rooms)
    public int facilitySize_;
}
