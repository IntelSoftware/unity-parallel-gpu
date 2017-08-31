using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Depending of the position of the camera
// Changing some rendering parameters
// Like enabling a fog, along with a background color
// To give a sort of underwater effect

public class Underwater : MonoBehaviour {

	public Camera cam;

	bool defaultFog;
	Color defaultFogColor;
	float defaultFogDensity;
	Material defaultSkyBox;
	Material noSkybox;

	public static float limit;
	public static bool mode;
	void Start () {
		defaultFog = RenderSettings.fog;
		defaultFogColor = RenderSettings.fogColor;
		defaultFogDensity = RenderSettings.fogDensity;
		defaultSkyBox = RenderSettings.skybox;
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
			RenderSettings.fog = defaultFog;
			RenderSettings.fogColor = defaultFogColor;
			RenderSettings.fogDensity = defaultFogDensity;
			RenderSettings.skybox = defaultSkyBox;
		}
	}
}
