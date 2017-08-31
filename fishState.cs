using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fishState {

	public float speed;
	public Vector3 position, forward;
	public Quaternion rotation;	

	public fishState(fishState state){
		Set (state.speed, state.position, state.forward, state.rotation);
	}

	public fishState(float s, Vector3 pos, Vector3 fwd, Quaternion rot){
		Set (s, pos, fwd, rot);
	}
		
	public void Set(float s, Vector3 pos, Vector3 fwd, Quaternion rot){
		speed = s;
		position = pos;
		forward = fwd;
		rotation = rot;
	}
}
