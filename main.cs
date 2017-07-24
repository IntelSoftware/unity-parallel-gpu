using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class main : MonoBehaviour {
	public Mesh mesh;
	public Material material;

	// Reglages des differents parametres de l'application. taille de l'aquarium, nombre de poissons, distances, multithread
	public static float tankRadius = 3f;
	public static float distNeighbor = 2.1f;
	public static float distAvoid = 0.2f;
	public static int 	numFishes = 1023;
	public static bool multithreading = false;	// si -m : multithread

	int instanceLimit = 1023;

	// data
	private List<List<Matrix4x4>> matrices = new List<List<Matrix4x4>>();
	private List<Matrix4x4> matFish = new List<Matrix4x4>();
	private Matrix4x4 temp;
	int nbmat, rest;
	public static fishState[,] states;
	Vector3 scale = Vector3.one * 100;
	float scalar ;

	// swap list for read/write
	public static int read = 0;
	public static int write = 1;
	int tmp;

	// Movement properties
	float deltaTime;
	float rotationSpeed = 2.1f;
	Quaternion rotX;

	// Use this for initialization
	void Start () {
		getInput ();
		states = new fishState[2, numFishes];
		float speed;
		int idx = 0;
		rotX = Quaternion.AngleAxis (-90, new Vector3 (1, 0, 0));
		scalar = tankRadius * 0.90f * Mathf.Sqrt (2);
		matrices.Add (new List<Matrix4x4> ());
		for (int i = 0; i < numFishes; i++) {
			if (i != 0 && i % instanceLimit == 0) {
				matrices.Add (new List<Matrix4x4> ());
				idx++;
			}
			Vector3 pos = Random.insideUnitSphere * scalar;
			Vector3 goal = Random.insideUnitSphere * scalar;
			speed = Random.Range (0.5f, 3.0f);

			///////// Faire face a son goal directement 
			Vector3 forward = (goal - pos).normalized;
			Quaternion rotation = Quaternion.identity, lerp = Quaternion.identity;
			if (forward != Vector3.zero) {
				lerp = Quaternion.Slerp (lerp, Quaternion.LookRotation (forward), rotationSpeed * Time.deltaTime * 5);
				rotation = lerp * rotX;
			}
			states [0, i] = new fishState ();
			states [0, i].Set (speed, pos, goal, forward, rotation, lerp);
			states [1, i] = new fishState (states [0, i]);
			temp.SetTRS(pos, rotation, scale);
			matrices [idx].Add (temp);
		}
		nbmat = Mathf.CeilToInt (numFishes / (float)instanceLimit);
		rest = numFishes - ((nbmat - 1) * instanceLimit);
	}
	
	// Update is called once per frame
	void Update () {
		deltaTime = Time.deltaTime;
		for (int i = 0; i < nbmat; i++) {
			matFish = matrices [i];
			if (i == (nbmat - 1)) {	
				if (multithreading) 
					Parallel.For (0, rest, delegate (int id) {Calc (id + i*instanceLimit);});
				else 
					for (int j = 0; j < rest; j++) {Calc (j + i*instanceLimit);}
			} else {
				if (multithreading) 
					Parallel.For (0, instanceLimit, delegate (int id) {Calc (id + i*instanceLimit);});
			 	else 
					for (int j = 0; j < instanceLimit; j++) {Calc (j + i*instanceLimit);}
			}
			Graphics.DrawMeshInstanced (mesh, 0, material, matFish);
		}
		tmp = read;			
		read = write;		
		write = tmp;
	}
				
	void Calc(int index){
		System.Random random = new System.Random ();
		float gen_speed = 0.0f;
		fishState other, current = states [read, index];
		Vector3 direction = current.direction, target, center = Vector3.zero, forward = Vector3.zero, avoid = Vector3.zero;
		Vector3 position = current.position;
		int size = 0;
		Quaternion rotation = current.rotation, lerp = current.lerp;
		for (int j = 0; j < numFishes; j++) {
			if (index != j) {
				other = states [read, j];
				float dist = Vector3.Distance (other.position, current.position);
				if (Neighbor (other.forward, current.forward, dist, distNeighbor)) {
					size++;
					center += other.position;	// COHESION
					gen_speed += other.speed;
					forward += other.forward;	// ALIGNMENT

					if (dist <= distAvoid)		// SEPARATION 
						avoid += (current.position - other.position);
				}
			}
		}
		if (size == 0) {
			gen_speed = current.speed;
			if (goalReached (current.position, current.direction)) {
				direction = new Vector3 (0.95f * tankRadius * (float)random.NextDouble (), 0.95f * tankRadius * (float)random.NextDouble (), 0.95f * tankRadius * (float)random.NextDouble ());
				gen_speed = (float)random.NextDouble () * 3;
			}
			forward = direction - position;
		}
		else {
			target = (forward / size) + current.forward;
			center = (center / size) + target - current.position;
			direction = center + avoid;
			gen_speed = current.speed + (((gen_speed/size)-current.speed)*0.50f);
		}

		if (Vector3.Distance (current.position, Vector3.zero) > scalar) { 
			direction += (Vector3.zero - position) * deltaTime * 4;
			gen_speed = (float)random.NextDouble () * 3;
		}


		if (size > 0) 
			forward = direction;
		position += (forward.normalized * deltaTime * gen_speed);

		if (forward != Vector3.zero) {
			lerp = Quaternion.Slerp (lerp, Quaternion.LookRotation (forward), rotationSpeed * deltaTime * 5);
			rotation = lerp * rotX;
		}
		states[write,index].Set(gen_speed, position, direction, direction.normalized, rotation, lerp);
		temp.SetTRS (position, rotation, scale);									
		matFish [index % instanceLimit] = temp;	
	}

	bool Neighbor(Vector3 selected, Vector3 focus, float dist, float neighbor){
		float scal = Vector3.Dot (selected, focus);
		float test;	
		float a, b, c = (0.5f * neighbor);
		if(scal<0){
			a = -0.2f * neighbor;
			b = a + c;
		}
		else{
			a = -0.24f * neighbor;
			b = -a + 0.5f * neighbor;
		}
		test = ((scal * scal * a) + (b * scal) + c);
		return dist <= test;
	}

	bool goalReached(Vector3 a, Vector3 b){
		float dist = Vector3.Distance (a, b);
		if (dist <= 0.2f)
			return true;
		return false;
	}

	void getInput(){	// Recuperation des differents parametres par l'invite de commande
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
			if(args[i] == "-a"){
				input = args[i+1];
				distAvoid = float.Parse(input);
			}
			if (args [i] == "-m")	// last argument of the cmd line
				multithreading = true;
		}
	}
}
