using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum Exits {

    NORTH = 1000,
    SOUTH = 2000,
    EAST = 3000,
    WEST = 4000,
    NONE = 9999
}

[CreateAssetMenu(fileName = "Data", menuName = "RoomData", order = 1)]
public class RoomData : ScriptableObjectBase {

    public Exits[] exits_ = { Exits.NONE };
    public GameObject prefab_;
    public bool occupiesWholeTile = true;

    public bool ContainsExit(Exits query) {
        foreach (Exits exit in exits_) {
            if (query == exit) {
                return true;
            }
        }
        return false;
    }
    public Exits GetRandomExit(){
        return exits_[Random.Range(0, exits_.Length)];
    }

}
