using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoomOpener))]
public class RoomOpenerEditor : Editor {
    public override void OnInspectorGUI() {

        DrawDefaultInspector();

        RoomOpener updateScript = (RoomOpener)target;
        if (GUILayout.Button("Update")) {
            updateScript.Start();
        }

    }
}

[System.Serializable]
public class Door {
    public GameObject doorObj;
    public Exits direction = Exits.NONE;
}

[RequireComponent(typeof(Room))]
public class RoomOpener : MonoBehaviour {

    public Door[] doors;
    private RoomData data;

	// Use this for initialization
	public void Start () {
		
        data = GetComponent<Room>().roomType_;
        if (data == null) {
            Debug.LogWarning("No data set. Set data.");
            return;
        }

        foreach (Door door in doors) {
            if (data.ContainsExit(door.direction)) {
                door.doorObj.SetActive(false);
            }
            else {
                door.doorObj.SetActive(true);
            }
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
