using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class main : MonoBehaviour {
	//	Links to the items we want to display
	public Mesh mesh;
	public Material material;
	public GameObject tank;
	public GameObject ground;

	// Parameters of the application
	public static float tankRadius = 5f;		// size of the tank
	public static float distNeighbor = 1.1f;	// maximal distance for two fishes to create a flock together
	public static int 	numFishes = 500;		// number of fishes
	public static bool multithreading = false;	// using CPU multithreading or not

	int instanceLimit = 1023;					// max number of instances which could be drawn at the same time

	// data
	Matrix4x4[][] fishesArray;					// contains properties needed to draw the fishes
	public static fishState[,] states;			// physical properties of the fish to calculate the flocks
	int nbmat, rest, idx;
	Vector3 scale = Vector3.one * 100;
	float borderX, borderY, borderZ;

	// swap list for read/write
	public static int read = 0;
	public static int write = 1;
	int tmp;

	// Movement properties
	float deltaTime;
	float rotationSpeed = 2.2f;
	Quaternion rotX;
	float avoid_velocity = 0.3f, direction_velocity = 8;

	// Use this for initialization
	void Start () {
		Init ();						// Inititialize the data to match the parameters of the application
		for (int i = 0; i < numFishes; i++) {
			if (i != 0 && i % instanceLimit == 0) 
				idx++;
			SetFish (i);				// Creation of a fish
		}
		Debug.Log (string.Format ("Total fish: {0} \t Instance limit: {1} \t Nb matrices: {2} \t Fish dans derniere: {3} \t tankRadius: {4}", numFishes, instanceLimit, nbmat, rest, tankRadius));
	}
			
	// Update is called once per frame
	void Update () {
		deltaTime = Time.deltaTime;
		idx = 0;
		for (int i = 0; i < nbmat; i++) {	// To draw the fishes, several calls have to be done because of the max number of instances per draw (1023)
			if (i == (nbmat - 1)) {
				if (multithreading) 		// if multithreading if enabled, use parallel for
					Parallel.For (0, rest, delegate (int id) {Calc (id + i*instanceLimit);});
				else 						// if not, singlethread for loop
					for (int j = 0; j < rest; j++) {Calc (j + i*instanceLimit);}
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
		System.Random random = new System.Random ();
		fishState other, current = states [read, index];
		float gen_speed = 0.0f, curspeed = current.speed;
		Vector3 direction = current.direction, center = Vector3.zero, forward = Vector3.zero, curfwd = current.forward, avoid = Vector3.zero;
		Vector3 position = current.position;
		int size = 0;
		Quaternion rotation = current.rotation, lerp = current.lerp;
		//center = current.position;
		// Each fish has to look at all the other fishes, if one is close enough to create a flock, the flocking behavior is set.
		for (int j = 0; j < numFishes; j++) {
			if (index != j) {
				other = states [read, j];
				Vector3 othpos = other.position, othfwd = other.forward;		
				if (Call(position.x,othpos.x,distNeighbor)) {			// if along the x axis the fishes are further than distance Neighbor
					float dist = Vector3.Distance (othpos, position);			// skip testing the distance which is taking time
					if (Neighbor (othfwd, curfwd, dist, distNeighbor)) {		
						size++;										// UPDATE FLOCKING BEHAVIOR
						center += othpos;				// COHESION
						gen_speed += other.speed;
						forward += othfwd;				// ALIGNMENT

					if (dist <= 0.30f)				// SEPARATION 
						avoid += (position - othpos).normalized / dist;
					}
				}
			}
		}

		if (size == 0) {	// if the fish has no one to swim with
			gen_speed = curspeed;
			if (goalReached (position, direction)) {	// checking if it reaches its own goal to swim towards a new random direction
				direction = new Vector3 (borderX * Mathf.Cos(360*(float)random.NextDouble()), borderY * Mathf.Cos(360*(float)random.NextDouble()), borderZ * Mathf.Cos(360*(float)random.NextDouble()));
				gen_speed = (float)random.NextDouble () * 3;
			}
			forward = direction - position;	
		}
		else {	// if not alone, updating its direction considering its neighbors
			direction = (direction_velocity * forward / size) + (avoid_velocity * avoid) + (center / size) + curfwd - position;
			gen_speed = curspeed + (((gen_speed/size)-curspeed)*0.50f);	// linearly converges to the average speed of the flock
		}
						
			
		if(OutofBounds(position)){		// if a fish reaches the limit of the tank, change its direction and speed
			Vector3 rand =  new Vector3 (borderX * Mathf.Cos(360*(float)random.NextDouble()), borderY * Mathf.Cos(360*(float)random.NextDouble()), borderZ * Mathf.Cos(360*(float)random.NextDouble()));
			direction = direction.normalized + (deltaTime * 4 * (rand - position));
			gen_speed = (float)random.NextDouble () * 3.5f;
		}

		if (size > 0) 
			forward = direction;
		
		position += (forward.normalized * deltaTime * gen_speed);	// Translate the fish
		if (forward != Vector3.zero) {
			lerp = Quaternion.Slerp (lerp, Quaternion.LookRotation (forward), rotationSpeed * deltaTime * 5);
			rotation = lerp * rotX;									// Rotate the fish towards its forward direction
		}
		states[write,index].Set(gen_speed, position, direction, forward.normalized, rotation, lerp);	// update the write state buffer
		fishesArray [idx] [index % instanceLimit].SetTRS (position, rotation, scale);					// update the matrix to draw the fishes
	}

	bool Call(float xA, float xB, float limit){
		float abs = (xA - xB);
		abs *= abs;
		if (abs < (limit * limit))
			return true;
		return false;
	}
				
	// If two fishes could be considered as neighbors or not. Depending of their distance but the direction they are facing also, using dot product of vectors.
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

	bool goalReached(Vector3 a, Vector3 b){		// if a fish has reached its goal
		float dist = Vector3.Distance (a, b);
		if (dist <= 0.2f)
			return true;
		return false;
	}

	void Init(){
		getInput ();
		// Initializing the scene along with the borders which are different considering the axes
		borderX = tankRadius * 2;
		borderY = tankRadius * 1;
		borderZ = tankRadius * 0.75f;
		tank.transform.position = Vector3.zero;
		tank.transform.localScale = new Vector3 (2.1f* borderX, 2.1f * borderY, 2.1f* borderZ);
		ground.transform.position = new Vector3 (0, - 1.05f * borderY, 0);
		ground.transform.localScale = new Vector3 (2.1f* borderX, 0.0001f, 2.1f* borderZ);

		states = new fishState[2, numFishes];
		idx = 0;
		rotX = Quaternion.AngleAxis (-90, new Vector3 (1, 0, 0));		// Create an additional quaternion to make the fish facing its direction (because of the imported mesh axis)
		nbmat = Mathf.CeilToInt (numFishes / (float)instanceLimit);		// Number of matrices which need to be created to draw all the instances
		rest = numFishes - ((nbmat - 1) * instanceLimit);
		fishesArray = new Matrix4x4[nbmat][];							// Creation of the matrices
		for (int i = 0; i < nbmat; i++) {
			if (i != nbmat - 1)
				fishesArray [i] = new Matrix4x4[instanceLimit];
			else
				fishesArray [i] = new Matrix4x4[rest];
		}
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
		Vector3 pos = new Vector3 (Random.Range(-borderX, borderX), Random.Range(-borderY, borderY), Random.Range(-borderZ, borderZ));
		Vector3 goal = new Vector3 (Random.Range(-borderX, borderX), Random.Range(-borderY, borderY), Random.Range(-borderZ, borderZ));
		while (pos == goal) 
			goal = new Vector3 (Random.Range(-borderX, borderX), Random.Range(-borderY, borderY), Random.Range(-borderZ, borderZ));
		float speed = Random.Range (0.5f, 3.0f);	 
		Vector3 forward = (goal - pos).normalized;
		// rotate to make it face its direction
		Quaternion lerp = Quaternion.Slerp(Quaternion.identity,Quaternion.LookRotation (forward), rotationSpeed * Time.deltaTime * 5);
		Quaternion rotation = lerp * rotX;
		// initialize the read/write buffer with the state of the fish
		states [0, i] = new fishState (speed, pos, goal, forward, rotation, lerp);
		states [1, i] = new fishState (states [0, i]);
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
				tankRadius = float.Parse(input);
			}
			if(args[i] == "-n"){
				input = args[i+1];
				distNeighbor = float.Parse(input);
			}
			if (args [i] == "-m")
				multithreading = true;
			
		}
	}
}