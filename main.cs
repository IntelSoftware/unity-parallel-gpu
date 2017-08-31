using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class main : MonoBehaviour {
	//	Links to the items we want to display
		// Fish
	public Mesh mesh;
	public Material material;
		// Aquarium
	public GameObject tank;
	public GameObject ground;
		// Water
	public Terrain terrain;
	public GameObject water;
	public GameObject projector;

	// Parameters of the application
	public static float tankHeight = 5f;	// height of the tank
	public static bool mode = true;			// false: aquarium - 	true: water
	bool multithreading = false;			// using CPU multithreading or not
	float distNeighbor = 1.1f;				// maximal distance for two fishes to create a flock together
	int numFishes = 300;					// number of fishes
	int instanceLimit = 1023;				// max number of instances which could be drawn at the same time

	// Movement and scene properties
	float deltaTime;
	const float max_speed = 3.5f, rotationSpeed = 2.2f * 7, outOfBoundsSpeed = 7;
	const float avoid_velocity = 0.3f, direction_velocity = 8;
	const float length = 4.0f, depth = 2.75f;
	Vector3 scale = Vector3.one;
	float borderX, borderY, borderZ;

	// data
	Matrix4x4[][] fishesArray;		// contains properties needed to draw the fishes
	fishState[][] states;			// physical properties of the fish to calculate the flocks
	int nbmat, left, idx = 0;

	// swap list for read/write
	int read = 0, write = 1, tmp;

	// Use this for initialization
	void Start () {
		Init ();						// Inititialize the data to match the parameters of the application
		for (int i = 0; i < numFishes; i++) {
			if (i != 0 && i % instanceLimit == 0) 
				idx++;
			SetFish (i);				// Creation of a fish
		}
		Debug.Log (string.Format ("Total fish: {0} \t Instance limit: {1} \t Nb matrices: {2} \t Fish within last matrix: {3} \t tankHeight: {4}", numFishes, instanceLimit, nbmat, left, tankHeight));
	}
			
	// Update is called once per frame
	void Update () {
		deltaTime = Time.deltaTime;
		idx = 0;
		for (int i = 0; i < nbmat; i++) {	// To draw the fishes, several calls have to be done because of the max number of instances per draw (1023)
			if (i == (nbmat - 1)) {
				if (multithreading) 		// if multithreading if enabled, use parallel for
					Parallel.For (0, left, delegate (int id) {Calc (id + i*instanceLimit);});
				else 						// if not, singlethread for loop
					for (int j = 0; j < left; j++) {Calc (j + i*instanceLimit);}
			} else {
				if (multithreading) 
					Parallel.For (0, instanceLimit, delegate (int id) {Calc (id + i*instanceLimit);});
			 	else 
					for (int j = 0; j < instanceLimit; j++) {Calc (j + i*instanceLimit);}
			}
			Graphics.DrawMeshInstanced (mesh, 0, material, fishesArray[i]);
			idx++;
		}
		tmp = read;			// swap between read and write buffers
		read = write;		
		write = tmp;
	}
										
	//	Update the properties of each fish
	void Calc(int index){
		fishState other, current = states [read] [index];
		Vector3 center = Vector3.zero, forward = Vector3.zero, avoid = Vector3.zero, position = current.position, curfwd = current.forward;
		Quaternion rotation = current.rotation;
		float speed = 0.0f, curspeed = current.speed;
		int numNeighbors = 0;
		System.Random random = new System.Random ();

		// Each fish has to look at all the other fishes, if one is close enough to create a flock, the flocking behavior is set.
		for (int j = 0; j < numFishes; j++) {
			if (index != j) {
				other = states [read] [j];
				Vector3 othpos = other.position, othfwd = other.forward;		
				if(Call(position.x, othpos.x, distNeighbor)){			// Check if the x-position of two fishes is close enough to create a flock
					float dist = Vector3.Distance (othpos, position);	// To avoid always calculating the distance, which is quite heavy		
					if (Neighbor (othfwd, curfwd, dist, distNeighbor)) {		
						numNeighbors++;										// UPDATE FLOCKING BEHAVIOR
						center += othpos;				// COHESION
						speed += other.speed;
						forward += othfwd;				// ALIGNMENT
						if (dist <= 0.30f)				// SEPARATION 
							avoid += (position - othpos).normalized / dist;
					}
				}
			}
		}

		if (numNeighbors == 0) {	// if the fish has no one to swim with
			speed = curspeed;
			forward = curfwd;
		} else {					// if not alone, updating its properties considering its neighbors <-> flocking rules
			forward = (direction_velocity * forward / numNeighbors) + (avoid_velocity * avoid) + (center / numNeighbors) + curfwd - position;
			speed = curspeed + (((speed / numNeighbors) - curspeed) * 0.50f);	// linearly converges to the average speed of the flock
		}

		if (OutofBounds (position)) {		// if a fish reaches the limit of the tank, change its direction and speed
			Vector3 rand = new Vector3 (borderX * Mathf.Cos (360 * (float)random.NextDouble ()),
										borderY * Mathf.Cos (360 * (float)random.NextDouble ()),
										borderZ * Mathf.Cos (360 * (float)random.NextDouble ()));
			forward = forward.normalized + (deltaTime * outOfBoundsSpeed * (rand - position));	
			speed = (float)random.NextDouble () * max_speed;
		}
			
		position += (forward.normalized * deltaTime * speed);	// Translate the fish

		if (forward != Vector3.zero) 
			rotation = Quaternion.Slerp (rotation, Quaternion.LookRotation (forward), rotationSpeed * deltaTime); // Rotate the fish towards its forward direction
		
		states[write][index].Set(speed, position, forward.normalized, rotation);		// update the write state buffer
		fishesArray [idx] [index % instanceLimit].SetTRS (position, rotation, scale);	// update the matrix to draw the fishes
	}

	// If two fishes are close enough to maybe be neighbors
	// Considering their x-position
	bool Call(float xA, float xB, float limit){
		float abs = (xA - xB);
		abs *= abs;
		if (abs < (limit * limit))
			return true;
		return false;
	}
				
	// If two fishes could be considered as neighbors or not. 
	// Depending of their distance but also their forward direction, using dot product of vectors.
	bool Neighbor(Vector3 selected, Vector3 focus, float dist, float neighbor){
		if (dist > neighbor) {
			return false;
		}
		float scal = Vector3.Dot (selected, focus);
		float a, b, c = (0.5f * neighbor);
		if(scal<0){
			a = -0.2f * neighbor;
			b = a + c;				
		}							
		else{
			a = -0.24f * neighbor;			// if they are facing almost the same direction (dot product > 0)
			b = -a + 0.5f * neighbor;		// they're likely more suitable to be in the same flock
		}
		float test = ((scal * scal * a) + (b * scal) + c);	// maximal distance to get them as neighbors regarding their properties - quadratic function
		return dist <= test;
	}
				
	bool OutofBounds(Vector3 position){
		if (Mathf.Abs(position.x) >= borderX)
			return true;
		if (Mathf.Abs(position.z) >= borderZ)
			return true;
		if (Mathf.Abs(position.y) >= borderY)
			return true;
		return false;
	}
		
	void SetFish(int i){	// Creation of a fish with random parameters
		float speed = Random.Range (0.5f, max_speed);
		Vector3 pos = new Vector3 (Random.Range(-borderX, borderX), Random.Range(-borderY, borderY), Random.Range(-borderZ, borderZ));
		Vector3 goal = new Vector3 (Random.Range(-borderX, borderX), Random.Range(-borderY, borderY), Random.Range(-borderZ, borderZ));
		while (pos == goal) 
			goal = new Vector3 (Random.Range(-borderX, borderX), Random.Range(-borderY, borderY), Random.Range(-borderZ, borderZ));
		Vector3 forward = (goal - pos).normalized;
		// rotate to make it face its randomly set direction
		Quaternion rotation = Quaternion.Slerp(Quaternion.identity,Quaternion.LookRotation (forward), rotationSpeed * Time.deltaTime);
		// initialize the read/write buffer with the state of the fish
		states [0][i] = new fishState (speed, pos, forward, rotation);
		states [1][i] = new fishState (states [0][i]);
	}

	void Init(){
		getInput ();
		SetScene ();

		states = new fishState[2][];
		states [0] = new fishState[numFishes];
		states [1] = new fishState[numFishes];

		nbmat = Mathf.CeilToInt (numFishes / (float)instanceLimit);		// Number of matrices which need to be created to draw all the instances
		left = numFishes - ((nbmat - 1) * instanceLimit);				// Number of instances in the last matrix, as it could be less than 1023
		fishesArray = new Matrix4x4[nbmat][];							// Creation of the matrices
		for (int i = 0; i < nbmat; i++) {
			if (i != nbmat - 1)
				fishesArray [i] = new Matrix4x4[instanceLimit];
			else
				fishesArray [i] = new Matrix4x4[left];
		}
	}

	void SetScene(){
		// Initializing the scene along with the borders which are different considering the axes
		// Also considering the selected mode, "water"-like or tank
		borderX = tankHeight * length;
		borderY = tankHeight/2;
		borderZ = tankHeight * depth;

		if (!mode) {
			water.SetActive (false);
			projector.SetActive (false);
			terrain.enabled = false;
			tank.transform.position = Vector3.zero;
			tank.transform.localScale = new Vector3 (2.2f* borderX, 2.2f * borderY, 2.2f* borderZ);
			ground.transform.position = new Vector3 (0, - 1.1f * borderY, 0);
			ground.transform.localScale = new Vector3 (2.2f* borderX, 0.0001f, 2.2f* borderZ);

		} else {
			terrain.transform.position = new Vector3 (-200, -borderY * 1.1f, -200);	
			water.transform.position = new Vector3 (0, borderY * 1.1f, 0);
			Underwater.limit = -terrain.transform.position.y;
			Underwater.mode = true;
			ground.SetActive (false);
			tank.SetActive (false);
		}
	}

	void getInput(){	// Getting parameters of the application through command line arguments
		string[] args = System.Environment.GetCommandLineArgs ();
		string input;
		for(int i=0; i<args.Length; i++){
			if(args[i] == "-f"){
				input = args[i+1];
				numFishes = System.Convert.ToInt16(input);
			}
			if(args[i] == "-t"){
				input = args[i+1];
				tankHeight = float.Parse(input);
			}
			if(args[i] == "-n"){
				input = args[i+1];
				distNeighbor = float.Parse(input);
			}
			if (args [i] == "-m")
				multithreading = true;
			if (args [i] == "-s")
				mode = false;
		}
	}
}