# unity-parallel-cpu

Source code for the Intel parallel project with Unity.
School of fish is executed, with the ability to choose if the application should be executed in the CPU, inside one singlethread or multi threads or, inside the GPU. 

AnimatedProjector.cs : Script to project the caustics light textures on the ground.

fishState.cs : Contains the class representing the physical properties of each fish.

FPSDisplay.cs : Script to display the frames per second.

moveCam.cs : Script to move the camera using the directional keys and the mouse.

Parallel.cs : Class providing the ability to do multithreading.

Process.compute : Compute shader written in HLSL to execute the flocking algorithm in the GPU.

Underwater.cs : Script to add underwater effects to the scene.

A .unitypackage and a guide also provided, about how to set Unity.

We can also set some parameters from the command line using the following :

-“-t” : size of the allowed area for the fishes ( <~> tank)

-“-f” : number of fishes to display

-“-r” : number of rocks to add to the scene

-“-n” : maximal distance for two fishes to be neighbor

-“-m” : mode to launch the application. 0: CPU Singlethread. 1: CPU Multithread. 2: GPU.

-“-s” : (at the END) if this is set, display a simpler scene with a visible tank, like below. If not the scene will look more realistic with dynamic water, and background
