using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveCam : MonoBehaviour {
	float speed = 0.1f;
	float sensitivity = 0.4f;
	float moveUD, moveLR, rotX, rotY;

	Matrix4x4 Perp;

	// Use this for initialization
	void Start () {
		Perp = Matrix4x4.zero;
		Perp.m11 = 1;
		Perp.m00 = Mathf.Cos(90*Mathf.PI/180);
		Perp.m22 = Mathf.Cos(90*Mathf.PI/180);
		Perp.m02 = Mathf.Sin (90 * Mathf.PI / 180);
		Perp.m20 = -Mathf.Sin (90 * Mathf.PI / 180);
		transform.position = new Vector3 (0, main.tankHeight/2 , -main.tankHeight* 8 / 3);
		transform.rotation = Quaternion.Euler (new Vector3(10, 2, 0));
	}

	// Update is called once per frame
	void Update () {
		moveUD = Input.GetAxis ("Vertical") * speed;
		moveLR = Input.GetAxis ("Horizontal") * speed;
		rotX = Input.GetAxis ("Mouse X") * sensitivity;
		rotY = Input.GetAxis ("Mouse Y") * sensitivity;

		transform.Rotate (0,rotX,0);
		Vector3 ortho = Perp * transform.forward;
		Vector3 movement = new Vector3(moveLR * ortho.x + moveUD*transform.forward.x, Mathf.Sin (rotY) * sensitivity, moveLR * ortho.z + moveUD*transform.forward.z);
		transform.position += movement;
	}
}
