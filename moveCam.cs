using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveCam : MonoBehaviour {

	float speed = 0.1f;
	float sensitivity = 0.4f;
	float moveUD, moveLR, rotX, rotY;

	// Use this for initialization
	void Start () {
		transform.position = new Vector3 (0, main.tankRadius/2 , -main.tankRadius * 10 / 3);
		transform.rotation = Quaternion.Euler (new Vector3(10, 2, 0));
	}

	// Update is called once per frame
	void Update () {
		moveUD = Input.GetAxis ("Vertical") * speed;
		moveLR = Input.GetAxis ("Horizontal") * speed;
		rotX = Input.GetAxis ("Mouse X") * sensitivity;
		rotY = Input.GetAxis ("Mouse Y") * sensitivity;

		Vector3 movement = new Vector3 (moveLR, Mathf.Sin(rotY) * sensitivity, moveUD);
		transform.Rotate (0,rotX,0);
		transform.position += movement;
	}

}
