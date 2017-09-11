# unity-parallel-cpu

Source code for the Intel parallel project with Unity.
School of fish is executed, with the ability to choose if the application should be executed in the CPU, inside one singlethread or multi threads or, inside the GPU. 

AnimatedProjector.cs : Script to project the caustics light textures on the ground.

fishState.cs : Contains the class representing the physical properties of each fish.

FPSDisplay.cs : Script to display the frames per second.

moveCam.cs :  Script to move the camera using the directional keys and the mouse.
  
Parallel.cs : Class providing the ability to do multithreading.

Process.compute : Compute shader written in HLSL to execute the flocking algorithm in the GPU.

Underwater.cs : Script to add underwater effects to the scene.

A .unitypackage and a guide also provided, about how to set Unity.
