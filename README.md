# Evacuation studies in Unity3D using ORCA

Simulates the evacuation of a rectangular room with a specified number of exits using Optimal Reciprocal Collision Avoidance (ORCA) for agent navigation.

![scenario](/images/readme.png)

## Editable parameters
* Number of runs
* Number of agents\*
* Agent radius\*
* Agent maximum speed\*
* Simulation time step\*
* Number of exits
* Exit width
* Exit spacing
* Room wall length
* Final goal position
* Final goal radius

\*Part of RVO2 library (see Credits).

![parameters](/images/parameters.png)

## Additional features
* Simulation data is saved to global path specified in SimulationDataWriter.cs

Possible if you are willing to edit the code, see SimulationController.cs:
* Custom scenarios: there is support for easily adding quad obstacles, though all polygonal shapes are possible. 
See SetupExitsScenario() and SetupBlocksScenario() for examples.
* Pre-planned or condition based agent subgoals (global path).

## Requirements
Unity version 2020.3.32f1

Clone the repository and import it as a new Unity project, and it should work out of the box.

## Credits
* RVO2 C# [Library](https://github.com/snape/RVO2-CS)
* ORCA by Jur van den Berg, Stephen J. Guy, Jamie Snape, Ming C. Lin, Dinesh Manocha 
* Example usage of the RVO2 C# Library in Unity by [warmtrue](https://github.com/warmtrue/RVO2-Unity)


