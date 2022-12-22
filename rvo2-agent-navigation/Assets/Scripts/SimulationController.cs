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
    public float GoalRadius = 20f;
    public int Runs = 10;
    public int NumAgents = 100;
    public float ExitWidth = 2.0f;
    [Range(1, 4)] public int NumExits = 1;
    public float SimulationTimeStep = 0.25f;
    public float AgentRadius = 0.75f;
    public float AgentMaxSpeed = 1.5f;
    public ScenarioType Scenario = ScenarioType.Blocks;

    [SerializeField] private GameObject _agentGameObject;
    [SerializeField] private GameObject _quadObstacleGameObject;

    private List<GameObject> _quadObstacleGameObjects = new List<GameObject>();

    //private List<GameObject> _agentGameObjects = new List<GameObject>();
    //private List<Vector2> _goalPositions = new List<Vector2>();
    private Dictionary<int, GameObject> _agentGameObjectsMap = new Dictionary<int, GameObject>();
    private Dictionary<int, Vector2> _goalPositionsMap = new Dictionary<int, Vector2>();

    private Random _random = new Random();
    private int _runCount = 1;

    private SimulationDataWriter _writer;

    // Start is called before the first frame update
    void Start()
    {
        _writer = new SimulationDataWriter(NumAgents, NumExits, Runs);
        SetupScenario();

    }

    // Update is called once per frame
    void Update()
    {
        if (_runCount > Runs) return;

        if (!AllAgentsReachedGoal())
        {
            // Maybe add something to have the simulation run
            // in realtime (that is, slower)
            UpdateVisualization();
            UpdatePreferredVelocities();
            Simulator.Instance.doStep();
        }
        // If all agents reached the goal, check if another run should be done
        else if (_runCount < Runs)
        {
            // Does not work since agent ids are not reset in the Simulator
            Debug.Log(Simulator.Instance.getGlobalTime());

            _writer.WriteEvacuationTime(Simulator.Instance.getGlobalTime());

            // Reset and rerun selected scenario
            ResetScenario();
            SetupScenario();

            _runCount++;
        }
        else if (_runCount == Runs)
        {
            Debug.Log(Simulator.Instance.getGlobalTime());
            _writer.WriteEvacuationTime(Simulator.Instance.getGlobalTime());

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
        Simulator.Instance.setAgentDefaults(15.0f, 10, 5.0f, 5.0f, AgentRadius, AgentMaxSpeed, new Vector2(0.0f, 0.0f));

        float wallWidth = 2.5f;
        float wallLength = 40.0f;

        float minX = wallWidth * 2;
        float maxX = wallLength - wallWidth * 2;
        float minY = wallWidth * 2;
        float maxY = wallLength - wallWidth * 2;

        List<Vector2> goals = new List<Vector2> { new Vector2(wallLength+wallWidth, 65.0f), new Vector2(-wallWidth, 65.0f) };

        // 3 Walls
        List<Vector2> wallSouth = new List<Vector2>();
        wallSouth.Add(new Vector2(wallLength, wallWidth));
        wallSouth.Add(new Vector2(0.0f, wallWidth));
        wallSouth.Add(new Vector2(0.0f, 0.0f));
        wallSouth.Add(new Vector2(wallLength, 0.0f));
        AddQuadObstacle(wallSouth);

        List<Vector2> wallWest = new List<Vector2>();
        wallWest.Add(new Vector2(0.0f, wallLength));
        wallWest.Add(new Vector2(-wallWidth, wallLength));
        wallWest.Add(new Vector2(-wallWidth, 0.0f));
        wallWest.Add(new Vector2(0.0f, 0.0f));
        AddQuadObstacle(wallWest);

        List<Vector2> wallEast = new List<Vector2>();
        wallEast.Add(new Vector2(wallLength, 0.0f));
        wallEast.Add(new Vector2(wallLength+wallWidth, 0.0f));
        wallEast.Add(new Vector2(wallLength+wallWidth, wallLength));
        wallEast.Add(new Vector2(wallLength, wallLength));
        AddQuadObstacle(wallEast);

        // 2 Exits

        // Construct walls such that there are NumExits exits
        // Number of north walls = NumExits - 1
        // Need special case for 1 exit (can't have it be a large gap) -> 2 walls
        // Exit params to consider: NumExits, ExitWidth, DistanceBetweenExits
        // Construct some formula considering these parameters
        List<Vector2> wallNorth1 = new List<Vector2>();
        wallNorth1.Add(new Vector2(ExitWidth/2, wallLength - wallWidth));
        wallNorth1.Add(new Vector2(wallLength - ExitWidth/2, wallLength - wallWidth));
        wallNorth1.Add(new Vector2(wallLength - ExitWidth/2, wallLength));
        wallNorth1.Add(new Vector2(ExitWidth/2, wallLength));
        AddQuadObstacle(wallNorth1);

        Simulator.Instance.processObstacles();

        for (int i = 0; i < NumAgents; i++)
        {
            float x = minX + (float)_random.NextDouble() * maxX;
            float y = minY + (float)_random.NextDouble() * maxY;

            int goalIdx = Mathf.RoundToInt((float)_random.NextDouble() * (goals.Count - 1));
            AddAgent(new Vector2(x, y), goals[goalIdx]);
        }

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

                AddAgent(new Vector2(55.0f + i * 10.0f, 55.0f + j * 10.0f), new Vector2(-75.0f, -75.0f));
                
                AddAgent(new Vector2(-55.0f - i * 10.0f, 55.0f + j * 10.0f), new Vector2(75.0f, -75.0f));

                AddAgent(new Vector2(55.0f + i * 10.0f, -55.0f - j * 10.0f), new Vector2(-75.0f, 75.0f));

                AddAgent(new Vector2(-55.0f - i * 10.0f, -55.0f - j * 10.0f), new Vector2(75.0f, 75.0f));
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

        Simulator.Instance.Clear();
    }

    private void UpdatePreferredVelocities()
    {
        foreach (int id in _agentGameObjectsMap.Keys)
        {
            Vector2 goalVector = _goalPositionsMap[id] - Simulator.Instance.getAgentPosition(id);

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

    private bool AllAgentsReachedGoal()
    {
        // TODO: Consider this returning the number of agents that have reached their goal instead
        foreach (int id in _agentGameObjectsMap.Keys)
        {
            if (RVOMath.absSq(Simulator.Instance.getAgentPosition(id) - _goalPositionsMap[id]) > GoalRadius * GoalRadius)
            {
                return false;
            }
        }

        return true;
    }

    private void AddAgent(Vector2 position, Vector2 goal)
    {
        GameObject go = LeanPool.Spawn(_agentGameObject, new Vector3(position.x(), 0, position.y()), Quaternion.identity);
        int id = Simulator.Instance.addAgent(position);

        _agentGameObjectsMap.Add(id, go);
        _goalPositionsMap.Add(id, goal);

        
    }

    private void DeleteAgent(int id)
    {
        Simulator.Instance.delAgent(id);

        LeanPool.Despawn(_agentGameObjectsMap[id]);
        _agentGameObjectsMap.Remove(id);
        _goalPositionsMap.Remove(id);
    }

    private void DeleteAllAgents()
    {
        foreach (int id in _agentGameObjectsMap.Keys)
        {
            LeanPool.Despawn(_agentGameObjectsMap[id]);
        }
        _agentGameObjectsMap.Clear();
        _goalPositionsMap.Clear();
    }

    private void AddQuadObstacle(List<Vector2> obstacle)
    {
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
        // There is no way to remove obstacles from Simulator
        // But Clear() removes all obstacles

        LeanPool.Despawn(_quadObstacleGameObjects[id]);
        _quadObstacleGameObjects.RemoveAt(id);
    }

    private void DeleteAllObstacles()
    {
        for (int i = 0; i < _quadObstacleGameObjects.Count; i++)
        {
            LeanPool.Despawn(_quadObstacleGameObjects[i]);
        }
        _quadObstacleGameObjects.Clear();
    }

    public enum ScenarioType
    {
        Blocks,
        Exits
    }
}
