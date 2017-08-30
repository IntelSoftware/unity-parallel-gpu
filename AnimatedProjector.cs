using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatedProjector : MonoBehaviour {

	public float fps = 30.0f;
	public Texture2D[] caustics;

	int iFrame;
	Projector projector;

	// Use this for initialization
	void Start () {
		projector = GetComponent<Projector> ();
		NextFrame ();	
		InvokeRepeating ("NextFrame", 1 / fps, 1 / fps);
	}			// allows to change the texture showing the light on the ground at the desired frequency ( here 30fps)

	void NextFrame() {
		projector.material.SetTexture ("_ShadowTex", caustics [iFrame]);
		iFrame = (iFrame + 1) % caustics.Length;
	}
}
