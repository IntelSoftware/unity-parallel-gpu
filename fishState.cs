using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fishState {

	public float speed;
	public Vector3 position, forward, direction;	// direction: goal of the fish if alone
	public Quaternion rotation, lerp;	

	public fishState(){
	}

	public fishState(fishState state){
		speed = state.speed;
		position = state.position;
		forward = state.forward;
		direction = state.direction;
		rotation = state.rotation;
		lerp = state.lerp;
	}
	/*
	public fishState(float s, Vector3 pos, Vector3 fwd, Vector3 dir, Quaternion rot){
		speed = s;
		position = pos;
		forward = fwd;
		direction = dir;
		rotation = rot;
		lerp = rot;
	}
	*/

	public fishState(float s, Vector3 pos, Vector3 dir, Vector3 fwd, Quaternion rot, Quaternion lrp){
		speed = s;
		position = pos;
		forward = fwd;
		direction = dir;
		rotation = rot;
		lerp = rot;
	}


	public void Set(float s, Vector3 pos, Vector3 dir, Vector3 fwd, Quaternion rot, Quaternion lrp){
		speed = s;
		position = pos;
		forward = fwd;
		direction = dir;
		rotation = rot;
		lerp = lrp;
	}
}
