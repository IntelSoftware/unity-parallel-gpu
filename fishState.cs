using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fishState {

	public float speed;
	public Vector3 position, forward;	// direction: goal of the fish if alone
	public Quaternion rotation, lerp;	

	public fishState(fishState state){
		Set (state.speed, state.position, state.forward, state.rotation, state.lerp);
	}

	public fishState(float s, Vector3 pos, Vector3 fwd, Quaternion rot, Quaternion lrp){
		Set (s, pos, fwd, rot, lrp);
	}


	public void Set(float s, Vector3 pos, Vector3 fwd, Quaternion rot, Quaternion lrp){
		speed = s;
		position = pos;
		forward = fwd;
		rotation = rot;
		lerp = lrp;
	}
}
