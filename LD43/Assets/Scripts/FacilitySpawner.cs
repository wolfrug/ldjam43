using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class FacilityRoom { // Determine the kinds of rooms there are in the facility and their number
    public string roomName_ = "Room";
    public RoomData room;
    public int amount = 1;
}

public class FacilitySpawner : MonoBehaviour {

	public FacilityData data_;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
