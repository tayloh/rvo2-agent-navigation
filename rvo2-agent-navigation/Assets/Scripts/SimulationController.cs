using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using RVO;
using Vector2 = RVO.Vector2;
using Random = System.Random;
using Lean;


public class SimulationController : MonoBehaviour
{

    private class AgentPath
    {
        private int _goalIndex = 0;

        private List<Vector2> _subGoals;

        public AgentPath(List<Vector2> path)
        {
            _subGoals = path;
        }

        public Vector2 GetCurrentGoal()
        {
            return _subGoals[_goalIndex];
        }

        public bool Next()
        {

            if (_goalIndex + 1 < _subGoals.Count)
            {
                _goalIndex++;
                return true;
            }

            return false;
        }

        public bool IsLast()
        {
            if (_goalIndex == _subGoals.Count - 1)
            {
                return true;
            }

            return false;
        }

        public Vector2 GetLastGoal()
        {
            return _subGoals[_subGoals.Count - 1];
        }

        public void SetCurrentGoal(Vector2 position)
        {
            _subGoals[_goalIndex] = position;
        }

    }

    [Header("Simulation")]
    public int Runs = 10;
    public float SimulationTimeStep = 0.25f;
    public bool RecordAgentsVsTime = false;

    [Tooltip("Blocks scenario is not supported any longer.")]
    public ScenarioType Scenario = ScenarioType.Exits;

    [Header("Final goal")]
    public float GoalRadius = 10f;
    public float FinalGoalY = 20.0f;

    [Header("Room dimensions")]
    public float WallWidth = 0.4f;
    public float WallLength = 10.0f;

    [Header("Exits")]
    [Range(1, 4)] public int NumExits = 1;
    public float ExitWidth = 1.0f;
    public float DistanceBetweenExits = 2.0f;

    [Header("Agents")]
    public int NumAgents = 100;
    public float AgentRadius = 0.2f;
    public float AgentMaxSpeed = 1.42f;
    public bool UseRandomSizeAndSpeed = false;
    
    private List<int> _numAgentsEvacuatedVsTime = new List<int>();

    [Header("Dependencies")]
    [SerializeField] private GameObject _agentGameObject;
    [SerializeField] private GameObject _quadObstacleGameObject;
    [SerializeField] private GameObject _exitGameObject;

    private Vector2 _finalGoalPosition;

    private List<GameObject> _quadObstacleGameObjects = new List<GameObject>();

    private Dictionary<int, GameObject> _agentGameObjectsMap = new Dictionary<int, GameObject>();
    private Dictionary<int, AgentPath> _agentPathsMap = new Dictionary<int, AgentPath>();

    private List<Vector2> _exitPositions = new List<Vector2>();
    private List<GameObject> _exitGameObjects = new List<GameObject>();
    private List<Color> _exitColors = new List<Color>() { 
        new Color((float)104/255, (float)32/255, (float)135/255, 1.0f), 
        new Color((float)232/255, (float)95/255, (float)21/255, 1.0f), 
        new Color((float)31/255,(float)31/255,(float)31/255, 1.0f), 
        new Color((float)40/255, (float)162/255, (float)168/255, 1.0f) 
    };

    private Random _random = new Random();
    private int _runCount = 1;

    private SimulationDataWriter _writer;

    private bool _shouldRun = true;

    // Start is called before the first frame update
    void Start()
    {
        if (NumExits == 1 && ExitWidth > WallLength)
        {
            Debug.Log("Exit width is larger than WallLength");
            _shouldRun = false;
            return;
        }
        else if (NumExits * ExitWidth + DistanceBetweenExits * (NumExits - 1) > WallLength)
        {
            Debug.Log("Exit parameters (NumExits, ExitWidth, DistanceBetweenExit) are incompatible with WallLength");
            _shouldRun = false;
            return;
        }

        // Put final goal position on the center x-coordinate
        _finalGoalPosition = new Vector2(WallLength / 2, FinalGoalY);
        
        _writer = new SimulationDataWriter(NumAgents, NumExits, Runs);
        _writer.WriteSimulationParameters(GoalRadius, FinalGoalY, WallWidth, WallLength, Runs, NumAgents,
            ExitWidth, DistanceBetweenExits, NumExits, SimulationTimeStep, AgentRadius, AgentMaxSpeed);
        
        // Call before Simulator.Instance.doStep()
        SetupScenario();

    }

    // Update is called once per frame
    void Update()
    {
        if (!_shouldRun) return;

        if (_runCount > Runs) return;

        // Simulation is done if all agents reached their final goal
        if (!AllAgentsReachedFinalGoal())
        {
            // Updates the simulation and its visualization
            // Note: the simulation runs as quickly as it can,
            // each call of Update() results in SimulationTimeStep
            // time passed in the simulation
            UpdateVisualization();
            UpdateAgentGoals();
            UpdatePreferredVelocities();
            Simulator.Instance.doStep();

            // If recording evacuated vs. time
            if (RecordAgentsVsTime)
            {
                int evacuated = GetNumberOfAgentsEvacuated();
                _numAgentsEvacuatedVsTime.Add(evacuated);
            }
        }
        // If all agents reached the goal, check if another run should be done
        else if (_runCount < Runs)
        {
            Debug.Log(Simulator.Instance.getGlobalTime());
            _writer.WriteEvacuationTime(Simulator.Instance.getGlobalTime());
            
            if (RecordAgentsVsTime)
            {
                _writer.WriteNumAgentsVsTime(_numAgentsEvacuatedVsTime);
                _numAgentsEvacuatedVsTime.Clear();
            }

            // Reset and rerun selected scenario
            ResetScenario();
            SetupScenario();

            _runCount++;
        }
        // For writing the final run
        else if (_runCount == Runs)
        {
            Debug.Log(Simulator.Instance.getGlobalTime());
            _writer.WriteEvacuationTime(Simulator.Instance.getGlobalTime());

            if (RecordAgentsVsTime)
            {
                _writer.WriteNumAgentsVsTime(_numAgentsEvacuatedVsTime);
                _numAgentsEvacuatedVsTime.Clear();
            }

            Debug.Log("Simulated " + _runCount + " runs.");
            Debug.Log("Evacuation times written to " + _writer.GetPath());
            _runCount = Runs + 1;
        }
        
    }

    private void SetupScenario()
    {
        switch (Scenario)
        {
            case ScenarioType.Blocks:
                SetupBlocksScenario();
                break;
            case ScenarioType.Exits:
                SetupExitsScenario();
                break;
        }
    }

    private void SetupExitsScenario()
    {
        Simulator.Instance.setTimeStep(SimulationTimeStep);

        // How to choose the first 4 parameters reasonably
        // for this type of simulation?
        Simulator.Instance.setAgentDefaults(15.0f, 10, 5.0f, 5.0f, AgentRadius, AgentMaxSpeed, new Vector2(0.0f, 0.0f));

        // -------------OBSTACLES-------------

        // 3 Walls
        // Obstacle vertices are computed using (minX, minY), (maxX, maxY)

        // South wall
        AddQuadObstacle(ComputeObstacleVertices(new Vector2(0.0f, 0.0f), new Vector2(WallLength, WallWidth)));

        // West wall
        AddQuadObstacle(ComputeObstacleVertices(new Vector2(-WallWidth, 0.0f), new Vector2(0.0f, WallLength)));

        // East wall
        AddQuadObstacle(ComputeObstacleVertices(new Vector2(WallLength, 0), new Vector2(WallLength+WallWidth, WallLength)));

        // North walls
        // Construct walls such that there are NumExits exits
        // Number of north walls = NumExits - 1
        // Need special case for 1 exit (can't have it be a large gap) -> 2 walls
        // Exit params to consider: NumExits, ExitWidth, DistanceBetweenExits

        float northWallsMinY = WallLength - WallWidth;
        float northWallsMaxY = WallLength;

        // Keep track of the exit positions while building the north walls
        List<Vector2> exitMiddlePositions = new List<Vector2>();

        // Special case
        // Just add two walls around the exit with the exit in the middle
        if (NumExits == 1)
        {
            float wall1_MinX = 0.0f;
            float wall1_MaxX = WallLength / 2 - ExitWidth / 2;
            List<Vector2> wall1 = ComputeObstacleVertices(
                new Vector2(wall1_MinX, northWallsMinY), 
                new Vector2(wall1_MaxX, northWallsMaxY));
            AddQuadObstacle(wall1);

            float wall2_MinX = WallLength / 2 + ExitWidth / 2;
            float wall2_MaxX = WallLength;
            List<Vector2> wall2 = ComputeObstacleVertices(
                new Vector2(wall2_MinX, northWallsMinY),
                new Vector2(wall2_MaxX, northWallsMaxY));
            AddQuadObstacle(wall2);

            // Only one exit
            exitMiddlePositions.Add(new Vector2(WallLength / 2, WallLength));
        }
        else if (NumExits > 1)
        {
            float totalDistanceForExitsAndSpacing = NumExits * ExitWidth + DistanceBetweenExits * (NumExits - 1);
            float startX = (WallLength - totalDistanceForExitsAndSpacing) / 2;

            // Starting wall
            List<Vector2> startWall = ComputeObstacleVertices(
                new Vector2(0.0f, northWallsMinY),
                new Vector2(startX, northWallsMaxY));
            AddQuadObstacle(startWall);

            // Middle walls
            for (int i = 1; i < NumExits; i++)
            {
                float wallMinX = startX + i * ExitWidth + (i-1) * DistanceBetweenExits;
                float wallMaxX = wallMinX + DistanceBetweenExits;
                //Debug.Log(wallMinX);
                //Debug.Log(wallMaxX);

                List<Vector2> wall = ComputeObstacleVertices(
                    new Vector2(wallMinX, northWallsMinY),
                    new Vector2(wallMaxX, northWallsMaxY));
                AddQuadObstacle(wall);

                //Vector2 exitMiddle = new Vector2(wallMinX + ExitWidth / 2, WallLength + WallWidth);
                Vector2 exitMiddle = new Vector2(wallMinX - ExitWidth / 2, WallLength);
                exitMiddlePositions.Add(exitMiddle);
            }

            // Ending wall
            List<Vector2> endingWall = ComputeObstacleVertices(
                new Vector2(WallLength - startX, northWallsMinY),
                new Vector2(WallLength, northWallsMaxY));
            AddQuadObstacle(endingWall);

            // Add the final exit position (not added in the loop above: 1 more exit than middle walls)
            exitMiddlePositions.Add(new Vector2(WallLength - startX - ExitWidth/2, WallLength));

        }
        
        // Save the exit positions for agent goal planning, see UpdateAgentGoals()
        _exitPositions = exitMiddlePositions;

        // Process the obstacles such that the Simulator accounts for them
        Simulator.Instance.processObstacles();

        // -------------AGENTS AND PATHS-------------

        // Agent starting position bounds, don't start in walls
        float minX = WallWidth;
        float maxX = WallLength - WallWidth;
        float minY = 2 * WallWidth;
        float maxY = WallLength / 2;

        // Instantiate agents at random positions within the far half of the room
        // Add a path consisting of { closest exit, final goal }
        // Though, note that the current goal is dynamically updated in UpdateAgentGoals()
        // So the AgentPath became redundant for this scenario
        for (int i = 0; i < NumAgents; i++)
        {
            float x = minX + (float)_random.NextDouble() * (maxX - minX);
            float y = minY + (float)_random.NextDouble() * (maxY - minY);

            //int index = Mathf.RoundToInt((float)_random.NextDouble() * (exitMiddlePositions.Count - 1));

            Vector2 agentSpawnPosition = new Vector2(x, y);
            List<Vector2> path = new List<Vector2> { FindClosestExit(agentSpawnPosition), _finalGoalPosition };

            AddAgent(agentSpawnPosition, path);
        }

        // -------------VISUALIZE EXITS-------------
        VisualizeExits();

    }

    private void SetupBlocksScenario()
    {
        Simulator.Instance.setTimeStep(SimulationTimeStep);

        float maxSpeed = AgentMaxSpeed;
        float agentRadius = AgentRadius;
        Simulator.Instance.setAgentDefaults(15.0f, 10, 5.0f, 5.0f, agentRadius, maxSpeed, new Vector2(0.0f, 0.0f));

        // Add agents and goals
        for (int i = 0; i < 5; ++i)
        {
            for (int j = 0; j < 5; ++j)
            {

                AddAgent(new Vector2(55.0f + i * 10.0f, 55.0f + j * 10.0f), new List<Vector2> { new Vector2(-75.0f, -75.0f) });
                
                AddAgent(new Vector2(-55.0f - i * 10.0f, 55.0f + j * 10.0f), new List<Vector2> { new Vector2(75.0f, -75.0f) });

                AddAgent(new Vector2(55.0f + i * 10.0f, -55.0f - j * 10.0f), new List<Vector2> { new Vector2(-75.0f, 75.0f) });

                AddAgent(new Vector2(-55.0f - i * 10.0f, -55.0f - j * 10.0f), new List<Vector2> { new Vector2(75.0f, 75.0f) });
            }
        }

        // Add obstacles
        List<Vector2> obstacle1 = new List<Vector2>();
        obstacle1.Add(new Vector2(-10.0f, 40.0f));
        obstacle1.Add(new Vector2(-40.0f, 40.0f));
        obstacle1.Add(new Vector2(-40.0f, 10.0f));
        obstacle1.Add(new Vector2(-10.0f, 10.0f));
        AddQuadObstacle(obstacle1);

        List<Vector2> obstacle2 = new List<Vector2>();
        obstacle2.Add(new Vector2(10.0f, 40.0f));
        obstacle2.Add(new Vector2(10.0f, 10.0f));
        obstacle2.Add(new Vector2(40.0f, 10.0f));
        obstacle2.Add(new Vector2(40.0f, 40.0f));
        AddQuadObstacle(obstacle2);

        List<Vector2> obstacle3 = new List<Vector2>();
        obstacle3.Add(new Vector2(10.0f, -40.0f));
        obstacle3.Add(new Vector2(40.0f, -40.0f));
        obstacle3.Add(new Vector2(40.0f, -10.0f));
        obstacle3.Add(new Vector2(10.0f, -10.0f));
        AddQuadObstacle(obstacle3);

        List<Vector2> obstacle4 = new List<Vector2>();
        obstacle4.Add(new Vector2(-10.0f, -40.0f));
        obstacle4.Add(new Vector2(-10.0f, -10.0f));
        obstacle4.Add(new Vector2(-40.0f, -10.0f));
        obstacle4.Add(new Vector2(-40.0f, -40.0f));
        AddQuadObstacle(obstacle4);

        // Must be called for the obstacles to be accounted for by Simulator
        Simulator.Instance.processObstacles();
    }
    
    private void ResetScenario()
    {
        // Does not do the same thing as
        // looping over DeleteAgent()

        // Deletes the gameobjects
        // and then clears the list of references
        DeleteAllAgents();

        // Same here
        DeleteAllObstacles();

        // Same for exits
        ClearExitsAndVisualization();

        Simulator.Instance.Clear();
    }

    private List<Vector2> ComputeObstacleVertices(Vector2 bottomLeft, Vector2 topRight)
    {
        // Vertices added in counterclockwise order as specified 
        // by RVO2 library
        List<Vector2> vertices = new List<Vector2>();
        vertices.Add(bottomLeft);
        vertices.Add(new Vector2(topRight.x(), bottomLeft.y()));
        vertices.Add(topRight);
        vertices.Add(new Vector2(bottomLeft.x(), topRight.y()));

        return vertices;
    }

    private void UpdatePreferredVelocities()
    {
        foreach (int id in _agentGameObjectsMap.Keys)
        {
            
            Vector2 goalVector = _agentPathsMap[id].GetCurrentGoal() - Simulator.Instance.getAgentPosition(id);

            if (RVOMath.absSq(goalVector) > 1.0f)
            {
                goalVector = RVOMath.normalize(goalVector);
            }

            Simulator.Instance.setAgentPrefVelocity(id, goalVector);

            /* Perturb a little to avoid deadlocks due to perfect symmetry. */
            float angle = (float)_random.NextDouble() * 2.0f * (float)Math.PI;
            float dist = (float)_random.NextDouble() * 0.0001f;

            Simulator.Instance.setAgentPrefVelocity(id, Simulator.Instance.getAgentPrefVelocity(id) +
                dist * new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)));
        }
    }

    private void UpdateVisualization()
    {
        foreach (int id in _agentGameObjectsMap.Keys)
        {

            Vector2 pos = Simulator.Instance.getAgentPosition(id);
            Vector2 vel = Simulator.Instance.getAgentPrefVelocity(id);
            _agentGameObjectsMap[id].transform.position = new Vector3(pos.x(), transform.position.y, pos.y());

            if (Math.Abs(vel.x()) > 0.01f && Math.Abs(vel.y()) > 0.01f)
            {
                _agentGameObjectsMap[id].transform.forward = new Vector3(vel.x(), 0, vel.y()).normalized;
            }

        }
    }

    private void UpdateAgentGoals()
    {
        foreach (int id in _agentGameObjectsMap.Keys)
        {
            Vector2 currentAgentPosition = Simulator.Instance.getAgentPosition(id);

            // Hack for exits scenario
            // If they exit through a door, just have them go straight for the exit
            // If they are not past a door, have them navigate to the closest door
            if (Scenario == ScenarioType.Exits && currentAgentPosition.y() > WallLength - WallWidth / 4)
            {
                _agentPathsMap[id].SetCurrentGoal(_finalGoalPosition);
            }
            else if (Scenario == ScenarioType.Exits && currentAgentPosition.y() < WallLength + WallWidth)
            {
                _agentPathsMap[id].SetCurrentGoal(FindClosestExit(currentAgentPosition));

                // Color agents by their exit if they have not yet exited
                Color exitColor = GetColorByPosition(currentAgentPosition);
                SetAgentColor(id, exitColor);

            }
            // Updates the agent goal as specified in their AgentPath if they reached their current goal
            else if (RVOMath.absSq(currentAgentPosition - _agentPathsMap[id].GetCurrentGoal()) < (ExitWidth / 2)*(ExitWidth / 2))
            {
                // Won't get run in the Exits scenario
                // Thus, AgentPath is useless in current implementation of Exits scenario
                _agentPathsMap[id].Next();
            }
        }
    }

    private Vector2 FindClosestExit(Vector2 fromPosition)
    {
        float minDistance = float.MaxValue;
        Vector2 closestExit = new Vector2();

        foreach (Vector2 exit in _exitPositions)
        {
            float distanceSquared = RVOMath.absSq(fromPosition - exit);
            if (distanceSquared < minDistance)
            {
                minDistance = distanceSquared;
                closestExit = exit;
            }
        }

        return closestExit;
    }

    private int FindIndexOfClosestExit(Vector2 fromPosition)
    {
        float minDistance = float.MaxValue;
        int closestExitIndex = -1;

        for (int i = 0; i < _exitPositions.Count; i++)
        {
            Vector2 exit = _exitPositions[i];
            float distanceSquared = RVOMath.absSq(fromPosition - exit);
            if (distanceSquared < minDistance)
            {
                minDistance = distanceSquared;
                closestExitIndex = i;
            }
        }
        return closestExitIndex;
    }

    private Color GetColorByPosition(Vector2 position)
    {
        int index = FindIndexOfClosestExit(position);
        return _exitColors[index];
    }

    private void SetAgentColor(int id, Color color)
    {
        _agentGameObjectsMap[id].gameObject.GetComponent<Renderer>().material.SetColor("_Color", color);
    }

    private bool AllAgentsReachedFinalGoal()
    {
        foreach (int id in _agentGameObjectsMap.Keys)
        {
            // If the scenario is exits, just check if they evacuated the building
            // Exit y locations are at WallLength
            if (Scenario == ScenarioType.Exits && Simulator.Instance.getAgentPosition(id).y() < WallLength)
            {
                return false;
            }

            //if (Scenario != ScenarioType.Exits && 
            //    RVOMath.absSq(Simulator.Instance.getAgentPosition(id) - _agentPathsMap[id].GetLastGoal()) > GoalRadius * GoalRadius)
            //{
            //    return false;
            //}
        }

        return true;
    }

    private void AddAgent(Vector2 position, List<Vector2> path)
    {
        // Adds agent GO to scene via LeanPool
        // Adds agent to simulation
        // Adds agent and its ID to map
        // Adds agent path to map

        GameObject go = LeanPool.Spawn(_agentGameObject, new Vector3(position.x(), 0, position.y()), Quaternion.identity);
        go.transform.localScale = new Vector3(AgentRadius * 2, 1, AgentRadius * 2);

        int id = Simulator.Instance.addAgent(position);

        if (UseRandomSizeAndSpeed)
        {
            // Radius between 0.32/2 and 0.44/2 
            float radius = 0.16f + (float)_random.NextDouble() * (0.22f - 0.16f);
            go.transform.localScale = new Vector3(radius * 2, 1, radius * 2);

            Simulator.Instance.setAgentRadius(id, radius);

            // Speed between 1 m/s and 3 m/s
            float speed = 1 + (float)_random.NextDouble() * 2.0f;
            Simulator.Instance.setAgentMaxSpeed(id, speed);
        }

        _agentGameObjectsMap.Add(id, go);
        _agentPathsMap.Add(id, new AgentPath(path));

        //_goalPositionsMap.Add(id, goal);
        
    }

    private void DeleteAgent(int id)
    {
        // Deletes agent from simulation,
        // from scene
        // from go map, and path map

        Simulator.Instance.delAgent(id);

        LeanPool.Despawn(_agentGameObjectsMap[id]);
        _agentGameObjectsMap.Remove(id);
        _agentPathsMap.Remove(id);

        //_goalPositionsMap.Remove(id);
    }

    private void DeleteAllAgents()
    {
        // Removes all agents from the scene
        // Then, cleares their references
        foreach (int id in _agentGameObjectsMap.Keys)
        {
            LeanPool.Despawn(_agentGameObjectsMap[id]);
        }
        _agentGameObjectsMap.Clear();
        _agentPathsMap.Clear();

        //_goalPositionsMap.Clear();
    }

    private void AddQuadObstacle(List<Vector2> obstacle)
    {
        // Adds obstacle to scene
        // Adds obstacle to GO references
        // Visualizes the obstacle using custom quad mesh
        // Adds obstacle to simulation

        // Add as gameobject
        GameObject go = LeanPool.Spawn(_quadObstacleGameObject, Vector3.zero, Quaternion.identity);
        _quadObstacleGameObjects.Add(go);

        
        // Visualize it
        Mesh mesh = go.GetComponent<MeshFilter>().mesh;

        List<Vector3> vertices3D = new List<Vector3>();

        for (int v = 0; v < obstacle.Count; v++)
        {
            vertices3D.Add(new Vector3(obstacle[v].x(), 1, obstacle[v].y()));
        }

        mesh.vertices = vertices3D.ToArray();

        // Vertex indices are backwards such that the normals point
        // towards the camera
        int[] triangles = new int[6] { 2, 1, 0, 3, 2, 0 };
        mesh.triangles = triangles;



        // Add to simulation (by geometry)
        Simulator.Instance.addObstacle(obstacle);
    }

    private void DeleteQuadObstacle(int id)
    {
        // There is no way to remove obstacles from Simulator after processing the obstacle
        // But Simulator.Instance.Clear() removes all obstacles
        // Use this method with care...

        LeanPool.Despawn(_quadObstacleGameObjects[id]);
        _quadObstacleGameObjects.RemoveAt(id);
    }

    private void DeleteAllObstacles()
    {
        // Deletes all obstacle gameobjects and their references
        // Should be used in cohesion with Simulator.Instance.Clear()
        // This just clears their visualization
        for (int i = 0; i < _quadObstacleGameObjects.Count; i++)
        {
            LeanPool.Despawn(_quadObstacleGameObjects[i]);
        }
        _quadObstacleGameObjects.Clear();
    }

    private void VisualizeExits()
    {
        for (int i = 0; i < _exitPositions.Count; i++)
        {
            GameObject go = LeanPool.Spawn(
                _exitGameObject, 
                new Vector3(_exitPositions[i].x(), 0, _exitPositions[i].y()), 
                Quaternion.identity);
            go.transform.localScale = new Vector3(AgentRadius * 3, 1, AgentRadius * 3);

            _exitGameObjects.Add(go);

            go.GetComponent<Renderer>().material.SetColor("_Color", _exitColors[i]);
        }
    }

    private void ClearExitsAndVisualization()
    {
        for (int i = 0; i < _exitGameObjects.Count; i++)
        {
            LeanPool.Despawn(_exitGameObjects[i]);
        }
        _exitGameObjects.Clear();
        _exitPositions.Clear();
    }

    private int GetNumberOfAgentsEvacuated()
    {
        int numEvacuated = 0;
        foreach (int id in _agentGameObjectsMap.Keys)
        {
            //if (RVOMath.absSq(Simulator.Instance.getAgentPosition(id) - _agentPathsMap[id].GetLastGoal()) < GoalRadius * GoalRadius)
            //{
            //    numEvacuated++;
            //}
            if (Simulator.Instance.getAgentPosition(id).y() >= WallLength)
            {
                numEvacuated++;
            }

        }
        return numEvacuated;
    }

    public enum ScenarioType
    {
        Blocks,
        Exits
    }
}
