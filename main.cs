using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class main : MonoBehaviour {
	struct fishState {

		public float speed;
		public Vector3 position, forward;
		public  Quaternion rotation;	

		public fishState(fishState state){
			speed = state.speed;
			position = state.position;
			forward = state.forward;
			rotation = state.rotation;
		}

		public fishState(float s, Vector3 pos, Vector3 fwd, Quaternion rot){
			speed = s;
			position = pos;
			forward = fwd;
			rotation = rot;
		}

		public void Set(float s, Vector3 pos, Vector3 fwd, Quaternion rot){
			speed = s;
			position = pos;
			forward = fwd;
			rotation = rot;
		}
	};
	//	Links to the items we want to display
	public ComputeShader shader;
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
	// Obstacles
	public GameObject rock;
	GameObject[] rocks;
	struct s_Rock{
		public Vector3 position;
		public float radius;
	}
	s_Rock[] obstacles;

	// Parameters of the application
	public static float tankHeight = 5f;	// height of the tank
	public static bool mode = true;			// false: aquarium - 	true: water
	int appMode = 02;						// 0: Single thread		1: Multi thread		2:	GPU
	float distNeighbor = 1.1f;				// maximal distance for two fish to create a flock together
	int numFishes = 1000;					// number of fish
	int numRocks = 50;						// number of rocks into the scene
	int instanceLimit = 1023;				// max number of instances which could be drawn at the same time

	// Movement and scene properties	-- constants
	float deltaTime; // time to complete the last frame

	//Speeds : Maximum speed for a fish, rotation speed
		//speed to add when near to the limits of the area (i.e edge reactivity)
	const float max_speed = 3.5f, rotationSpeed = 2.2f * 7, outOfBoundsSpeed = 7;
	// Flocking : weight/velocity of each flocking rule. 
		//cohesion velocity is not provided, should be left with a unit weight
	const float avoid_velocity = 0.3f, direction_velocity = 8;
	// Dimension : How long/deep the area will be
		// related to the height. how many times longer/deeper than higher
	const float length = 4.0f, depth = 2.75f;
	// Borders of the area following each direction
	float borderX, borderY, borderZ;
	Vector3 scale = Vector3.one;	// scale of the mesh to draw, keeping this the same for all fishes. 


	public delegate void Updater();
	Updater updater;
	public delegate void UpdaterMode(int i, int max);
	UpdaterMode updaterMode;

	// data
	Matrix4x4[][] fishArray;		// contains properties needed to draw the fish
	fishState[][] states;			// physical properties of the fish to calculate the flocks
	int nbmat, left;				// number of matrices to store all the instances of fish, and number of fish left in the last matrix
	int nbGroups;					// GPU : number of group of threads which are needed to compute the calculation, 1 thread per fish
	int idx = 0;
	int kernel;						// GPU : index of the function (kernel) inside the compute shader, which needs to be run for the flocking algorithm

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
		updater ();
	}		

	void UpdateStates(int i){		// called for each fish after the shader has done all calculations		
		if (states [write] [i].forward != Vector3.zero) 
			states[write][i].rotation = Quaternion.Slerp(states[read][i].rotation, 
					Quaternion.LookRotation(states[write][i].forward), rotationSpeed * deltaTime);
		// setting the TRS matrix to draw the fish
		fishArray [idx] [i % instanceLimit].SetTRS(states [write] [i].position, states [write] [i].rotation, scale);	
	}

	void RunShader(){			// Setting and filling the buffers about the states of the fishes
		ComputeBuffer rState = new ComputeBuffer (numFishes, System.Runtime.InteropServices.Marshal.SizeOf (states[0][0]));
		ComputeBuffer wState = new ComputeBuffer (numFishes, System.Runtime.InteropServices.Marshal.SizeOf (states[0][0]));
		shader.SetBuffer (kernel, "readState", rState);	
		shader.SetBuffer(kernel, "writeState", wState);
		rState.SetData (states[read]);
		wState.SetData (states [write]);
		// run the shader : flocking algorithm
		shader.Dispatch (kernel, nbGroups, 1, 1);
		// Once the work is done, get back the written data and save it
		wState.GetData (states[write]);
	}	
										

	void Calc(int index){		//	Update the properties of each fish
		fishState other, current = states [read] [index];
		Vector3 center = Vector3.zero, forward = Vector3.zero, avoid = Vector3.zero, position = current.position, curfwd = current.forward;
		Quaternion rotation = current.rotation;
		float speed = 0.0f, curspeed = current.speed;
		int numNeighbors = 0;
		System.Random random = new System.Random ();

		// Each fish has to look at all the other fish, if one is close enough to create a flock, the flocking behavior is set.
		for (int j = 0; j < numFishes; j++) {
			if (index != j) {
				other = states [read] [j];
				Vector3 othpos = other.position, othfwd = other.forward;		
				if(Call(position.x, othpos.x, distNeighbor)){			// Check if the x-position of two fish is close enough to create a flock
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
			
		forward += Rocks (position, forward, speed, max_speed);	// Check if the fish is close to a rock - if so, add an avoidance force
		position += (forward.normalized * deltaTime * speed);	// Translate the fish

		if (forward != Vector3.zero) 
			rotation = Quaternion.Slerp (rotation, Quaternion.LookRotation (forward), rotationSpeed * deltaTime); // Rotate the fish towards its forward direction
		
		states[write][index].Set(speed, position, forward.normalized, rotation);		// update the write state buffer
		fishArray [idx] [index % instanceLimit].SetTRS (position, rotation, scale);	// update the matrix to draw the fish
	}

	// If two fish are close enough to maybe be neighbors
	// Considering their x-position
	bool Call(float xA, float xB, float limit){
		float abs = (xA - xB);
		abs *= abs;
		if (abs < (limit * limit))
			return true;
		return false;
	}

	// Considering a fish
	// Will check if this fish is about to collide into any rocks of the scene
	Vector3 Rocks(Vector3 position, Vector3 fwd, float speed, float max){
		Vector3 avoid;
		fwd = fwd.normalized * deltaTime * speed;
		for (int i = 0; i < obstacles.Length; i++) {
			avoid = Rock (position, fwd, obstacles[i].position, obstacles[i].radius);
			if (avoid != Vector3.zero) 
				return avoid;
		}
		return Vector3.zero;
	}

	// Considering a fish
	// Will check if this fish is about to collide into a rock of the scene
	Vector3 Rock(Vector3 pos, Vector3 fwd, Vector3 rocpos, float scale){
		Vector3 avoid = Vector3.zero;	
		Vector3 ahead = pos + fwd;			
		Vector3 ahead2 = pos + fwd/2;
		if(Vector3.Distance(ahead,rocpos) < scale){
			avoid = (ahead - rocpos).normalized * max_speed;
			return avoid;
		}
		if(Vector3.Distance(ahead2,rocpos) < scale){
			avoid = (ahead2 - rocpos).normalized * max_speed;
			return avoid;
		}
		return avoid;
	}
				
	// If two fish could be considered neighbors or not. 
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
		InitShader ();
		SetDelegates ();

		int threadsPgroup = 1024/4;					// Shader parameter, number of threads per group, to get the number of groups
		nbGroups = Mathf.CeilToInt (numFishes / (float)threadsPgroup);

		states = new fishState[2][];
		states [0] = new fishState[numFishes];
		states [1] = new fishState[numFishes];

		nbmat = Mathf.CeilToInt (numFishes / (float)instanceLimit);		// Number of matrices which need to be created to draw all the instances
		left = numFishes - ((nbmat - 1) * instanceLimit);				// Number of instances in the last matrix, as it could be less than 1023
		fishArray = new Matrix4x4[nbmat][];							// Creation of the matrices
		for (int i = 0; i < nbmat; i++) {
			if (i != nbmat - 1)
				fishArray [i] = new Matrix4x4[instanceLimit];
			else
				fishArray [i] = new Matrix4x4[left];
		}
	}

	// Initialization of the data which needs to be sent to the compute shader
	// For GPU Application
	void InitShader(){
		kernel = shader.FindKernel ("Calc");
		shader.SetVector ("settings", new Vector3 (numFishes, numRocks, distNeighbor));
		shader.SetVector ("borders", new Vector3 (borderX, borderY, borderZ));
		shader.SetVector("velocities", new Vector2(avoid_velocity,direction_velocity));
		shader.SetVector ("speeds", new Vector3 (max_speed,rotationSpeed,outOfBoundsSpeed));
		if (numRocks > 0) {	 // if we want rocks, setting and filling the rock buffer at start-up, it won't ever change
			ComputeBuffer bRocks = new ComputeBuffer (numRocks, System.Runtime.InteropServices.Marshal.SizeOf (obstacles[0]));
			shader.SetBuffer(kernel, "Rocks", bRocks);
			bRocks.SetData (obstacles);
		}
	}

	// Considering the desired mode : CPU MT/ST or GPU
	// Set the delegates to execute the right function 
	// During each frame update
	void SetDelegates(){
		if (appMode == 2) {
			updaterMode = new UpdaterMode (GPU);
			updater = new Updater (Update_GPU);
		} else {
			updater = new Updater (Update_CPU);
			if (appMode==1)
				updaterMode = new UpdaterMode (MT);
			else
				updaterMode = new UpdaterMode (ST);
		}
	}

	// Selected mode : CPU - Multithread
	// Application of the flocking algorithm 
	void MT(int i, int max){
		if(i==max)
			Parallel.For (0, left, delegate (int id) {Calc (id + i * instanceLimit);});
		else
			Parallel.For (0, instanceLimit, delegate (int id) {Calc (id + i * instanceLimit);});
	}

	// Selected mode : GPU
	// Update the fish state after the flocking algorithm being executed in the GPU
	void GPU(int i, int max){
		if(i==max)
			Parallel.For (0, left, delegate (int id) {UpdateStates (id + i*instanceLimit);});
		else
			Parallel.For(0,instanceLimit,delegate (int id){UpdateStates(id + i*instanceLimit);});	
	}

	// Selected mode : CPU - Singlethread
	// Application of the flocking algorithm 
	void ST(int i, int max){
		if(i==max)
			for (int j = 0; j < left; j++) {Calc (j + i*instanceLimit);}
		else
			for (int j = 0; j < instanceLimit; j++) {Calc (j + i*instanceLimit);}
	}
		
	void Update_GPU(){			// Update of the scene if the selected mode is GPU
		deltaTime = Time.deltaTime;
		idx = 0;

		shader.SetFloat ("deltaTime", deltaTime);
		RunShader ();
		int max = nbmat - 1;
		for (int j = 0; j < nbmat; j++) {	// To draw the fish, several calls have to be done because of the max number of instances per draw (1023)
			updaterMode (j, max);
			Graphics.DrawMeshInstanced (mesh, 0, material, fishArray[j]);
			idx++;
		}
		tmp = read;			// swap between read and write buffers
		read = write;		
		write = tmp;

	}
	// Update of the scene if the selected mode is CPU
	// Select then between single thread or multi thread
	void Update_CPU(){
		deltaTime = Time.deltaTime;
		idx = 0;
		int max = nbmat - 1;
		for (int j = 0; j < nbmat; j++) {	// To draw the fish, several calls have to be done because of the max number of instances per draw (1023)
			updaterMode (j, max);
			Graphics.DrawMeshInstanced (mesh, 0, material, fishArray[j]);
			idx++;
		}
		tmp = read;			// swap between read and write buffers
		read = write;		
		write = tmp;
	}

	// Initializing the scene along with the borders which are different considering the axes
	// Also considering the selected mode, "water"-like or tank
	void SetScene(){
		borderX = tankHeight * length;
		borderY = tankHeight/2;
		borderZ = tankHeight * depth;

		Transform rockT = rock.transform;
		obstacles = new s_Rock[numRocks];
		rocks = new GameObject[numRocks];
		for (int i = 0; i < numRocks; i++) {
			float scale = Random.Range (0.15f, 1.00f) * tankHeight;
			Vector3 position = new Vector3 (Random.Range (-borderX, borderX), -borderY + scale*Random.Range(0,0.2f), Random.Range (-borderZ, borderZ)); 
			obstacles[i].position = position;
			obstacles[i].radius = scale / 2;
			rock.transform.localScale = new Vector3 (scale, scale, scale);
			rocks [i] = (GameObject)Instantiate (rock, position, Quaternion.identity);
		}
			
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
			water.transform.position = new Vector3 (0, borderY * 1.15f, 0);
			Underwater.limit = -terrain.transform.position.y;
			Underwater.mode = true;
			ground.SetActive (false);
			tank.SetActive (false);
		}
	}

	// Getting parameters of the application through command line arguments
	// if some parameters are less than 0, set them to 0
	// in the same case, other will stick to their default value
	// shown under parameters of the application (l54)
	void getInput(){	
		string[] args = System.Environment.GetCommandLineArgs ();
		string input;
		for(int i=0; i<args.Length; i++){
			if(args[i] == "-f"){
				input = args[i+1];
				if (System.Convert.ToInt16 (input) < 0)
					numFishes = 0;
				else
					numFishes = System.Convert.ToInt16(input);
			}
			if(args[i] == "-t"){
				input = args[i+1];
				if(float.Parse(input) > 0)
					tankHeight = float.Parse(input);
			}
			if(args[i] == "-n"){
				input = args[i+1];
				if (float.Parse (input) < 0)
					distNeighbor = 0;
				else
					distNeighbor = float.Parse(input);
			}
			if (args [i] == "-m") {
				input = args[i+1];
				if (System.Convert.ToInt16 (input) == 0)
					appMode = 0;
				else if (System.Convert.ToInt16 (input) == 1)
					appMode = 1;
			}
			if (args [i] == "-r") {
				input = args[i+1];
				if (System.Convert.ToInt16 (input) < 1)
					numRocks = 0;
				else
					numRocks = System.Convert.ToInt16(input);
			}
			if (args [i] == "-s")
				mode = false;
		}
	}
}
