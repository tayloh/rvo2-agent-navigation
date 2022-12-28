# Evacuation simulation in Unity3D using ORCA

Simulates the evacuation of a rectangular room with a specified number of exits using Optimal Reciprocal Collision Avoidance (ORCA) for agent navigation. Developed to study the effect of varying exit parameters in evacuation scenarios.

![scenario](/images/readme.png)

## Editable parameters
* Number of runs
* Number of agents\*
* Agent radius\* (m)
* Agent maximum speed\* (m / s)
* Simulation time step\* (s)
* Number of exits
* Exit width (m)
* Exit spacing (m)
* Room wall length (m)
* Room wall width (m)
* Final goal position y-offset (m)
* Final goal radius (m)

\*Part of RVO2 library that implements ORCA (see Credits).

![parameters](/images/parameters.png)

## Additional features
* Simulation data (evacuation times per run, and optionally- agents evacuated at each simulated time step) is saved to the global path specified in SimulationDataWriter.cs.

Possible if you are willing to edit the code, see SimulationController.cs:
* Custom scenarios: there is support for easily adding quad obstacles, though all polygonal shapes are possible. 
See SetupExitsScenario() and SetupBlocksScenario() for examples.
* Pre-planned or condition based agent subgoals (global path).
* Scripts for handling the simulation data.

## Requirements
Unity version 2020.3.32f1

Clone the repository and import it as a new Unity project.

**OBS:** Change the path for where to store the simulation data in SimulationDataWriter.cs before running.

## Credits
* RVO2 C# [Library](https://github.com/snape/RVO2-CS)
* ORCA by Jur van den Berg, Stephen J. Guy, Jamie Snape, Ming C. Lin, Dinesh Manocha 
* Example usage of the RVO2 C# Library in Unity by [warmtrue](https://github.com/warmtrue/RVO2-Unity)


