using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Depending of the position of the camera
// Changing some rendering parameters
// Like enabling a fog, along with a background color
// To give a sort of underwater effect

public class Underwater : MonoBehaviour {

	public Camera cam;

	bool fog;
	Color fogColor;
	float fogDensity;
	Material skybox;
	Material noSkybox;

	public static float limit;
	public static bool mode = false;
	void Start () {
		fog = RenderSettings.fog;
		fogColor = RenderSettings.fogColor;
		fogDensity = RenderSettings.fogDensity;
		skybox = RenderSettings.skybox;
		cam.backgroundColor = new Color (0, 0.4f, 0.7f, 1);
	}
	
	// Update is called once per frame
	void Update () {
		if (mode && transform.position.y < limit) {	// if camera under lvl of water -> underwater render settings
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
