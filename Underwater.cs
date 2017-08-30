using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Underwater : MonoBehaviour {

	public Terrain terrain;
	public Camera cam;

	bool fog;
	Color fogColor;
	float fogDensity;
	Material skybox;
	Material noSkybox;

	// Use this for initialization
	void Start () {
		fog = RenderSettings.fog;
		fogColor = RenderSettings.fogColor;
		fogDensity = RenderSettings.fogDensity;
		skybox = RenderSettings.skybox;
		cam.backgroundColor = new Color (0, 0.4f, 0.7f, 1);
	}
	
	// Update is called once per frame
	void Update () {
		// adding an effect to the camera, a fog, to give the impression of being underwater, with a different background, ie water.
		if (main.mode && transform.position.y < -terrain.transform.position.y) {
			RenderSettings.fog = true;
			RenderSettings.fogColor = new Color (0, 0.4f, 0.7f, 0.6f);
			RenderSettings.fogDensity = 0.04f;
			RenderSettings.skybox = noSkybox;
		} else {
			RenderSettings.fog = fog;
			RenderSettings.fogColor = fogColor;
			RenderSettings.fogDensity = fogDensity;
			RenderSettings.skybox = skybox;
		}
	}
}
