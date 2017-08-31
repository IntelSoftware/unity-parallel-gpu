using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Change the texture showing underwater light effect (caustics) 
// at the desired frequency (here 30Hz)
// The textures are taken from an array inside Unity editor
public class AnimatedProjector : MonoBehaviour {

	public float fps = 30.0f;
	public Texture2D[] frames;

	int frameIndex;
	Projector projector;

	// Use this for initialization
	void Start () {
		projector = GetComponent<Projector> ();
		NextFrame ();	
		InvokeRepeating ("NextFrame", 1 / fps, 1 / fps);
	}			
		
	void NextFrame() {
		projector.material.SetTexture ("_ShadowTex", frames [frameIndex]);
		frameIndex = (frameIndex + 1) % frames.Length;
	}
}
