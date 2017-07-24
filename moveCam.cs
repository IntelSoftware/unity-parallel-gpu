using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveCam : MonoBehaviour {

	float speed = 0.1f;
	float sensitivity = 0.3f;
	float moveUD, moveLR, rotX, rotY;

	// Use this for initialization
	void Start () {
		// Initializing the position of the camera at the beginning to make it look to the main scene
		transform.position = new Vector3(0, main.tankRadius/2, -main.tankRadius*10/3);
		transform.rotation = Quaternion.Euler(new Vector3(10,2,0));
	}
	
	// Update is called once per frame
	void Update () {
		// Depending on movements with the keyboard/mouse
		moveUD = Input.GetAxis ("Vertical") * speed;
		moveLR = Input.GetAxis ("Horizontal") * speed;
		rotX = Input.GetAxis ("Mouse X") * sensitivity;
		rotY = Input.GetAxis ("Mouse Y") * sensitivity;
		Vector3 movement = new Vector3 (moveLR, 0, moveUD);
		// Rotate & translate the camera
		transform.Rotate (-rotY, rotX, 0);
		transform.position += movement;
	}
}
