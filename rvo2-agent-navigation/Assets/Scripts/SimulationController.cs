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

    public float GoalRadius = 20f;
    public float FinalGoalY = 65.0f;
    public float WallWidth = 2.5f;
    public float WallLength = 40.0f;
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

    private Vector2 _finalGoalPosition;

    private List<GameObject> _quadObstacleGameObjects = new List<GameObject>();

    private Dictionary<int, GameObject> _agentGameObjectsMap = new Dictionary<int, GameObject>();
    //private Dictionary<int, Vector2> _goalPositionsMap = new Dictionary<int, Vector2>();
    private Dictionary<int, AgentPath> _agentPathsMap = new Dictionary<int, AgentPath>();

    private Random _random = new Random();
    private int _runCount = 1;

    private SimulationDataWriter _writer;

    // Start is called before the first frame update
    void Start()
    {

        _finalGoalPosition = new Vector2(WallLength / 2, FinalGoalY);
        _writer = new SimulationDataWriter(NumAgents, NumExits, Runs);
        SetupScenario();

    }

    // Update is called once per frame
    void Update()
    {
        if (_runCount > Runs) return;

        if (!AllAgentsReachedFinalGoal())
        {
            // Maybe add something to have the simulation run
            // in realtime (that is, slower)
            UpdateVisualization();
            UpdateAgentGoals();
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

        float minX = WallWidth * 2;
        float maxX = WallLength - WallWidth * 2;
        float minY = WallWidth * 2;
        float maxY = WallLength / 2;


        // 3 Walls
        List<Vector2> wallSouth = new List<Vector2>();
        wallSouth.Add(new Vector2(WallLength, WallWidth));
        wallSouth.Add(new Vector2(0.0f, WallWidth));
        wallSouth.Add(new Vector2(0.0f, 0.0f));
        wallSouth.Add(new Vector2(WallLength, 0.0f));
        AddQuadObstacle(wallSouth);

        List<Vector2> wallWest = new List<Vector2>();
        wallWest.Add(new Vector2(0.0f, WallLength));
        wallWest.Add(new Vector2(-WallWidth, WallLength));
        wallWest.Add(new Vector2(-WallWidth, 0.0f));
        wallWest.Add(new Vector2(0.0f, 0.0f));
        AddQuadObstacle(wallWest);

        List<Vector2> wallEast = new List<Vector2>();
        wallEast.Add(new Vector2(WallLength, 0.0f));
        wallEast.Add(new Vector2(WallLength+WallWidth, 0.0f));
        wallEast.Add(new Vector2(WallLength+WallWidth, WallLength));
        wallEast.Add(new Vector2(WallLength, WallLength));
        AddQuadObstacle(wallEast);

        // 2 Exits

        // Construct walls such that there are NumExits exits
        // Number of north walls = NumExits - 1
        // Need special case for 1 exit (can't have it be a large gap) -> 2 walls
        // Exit params to consider: NumExits, ExitWidth, DistanceBetweenExits
        // Construct some formula considering these parameters
        List<Vector2> wallNorth1 = new List<Vector2>();
        wallNorth1.Add(new Vector2(ExitWidth, WallLength - WallWidth));
        wallNorth1.Add(new Vector2(WallLength - ExitWidth, WallLength - WallWidth));
        wallNorth1.Add(new Vector2(WallLength - ExitWidth, WallLength));
        wallNorth1.Add(new Vector2(ExitWidth, WallLength));
        AddQuadObstacle(wallNorth1);

        Simulator.Instance.processObstacles();

        // Left to right
        List<Vector2> goals = new List<Vector2> { new Vector2(-WallWidth, 65.0f), new Vector2(WallLength + WallWidth, 65.0f) };
        List<Vector2> exits = new List<Vector2> { 
            new Vector2(ExitWidth / 2, WallLength + WallWidth), 
            new Vector2(WallLength - ExitWidth / 2, WallLength + WallWidth) 
        };

        for (int i = 0; i < NumAgents; i++)
        {
            float x = minX + (float)_random.NextDouble() * maxX;
            float y = minY + (float)_random.NextDouble() * maxY;

            int index = Mathf.RoundToInt((float)_random.NextDouble() * (goals.Count - 1));

            List<Vector2> path = new List<Vector2> { exits[index], _finalGoalPosition };

            AddAgent(new Vector2(x, y), path);
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
            // Hack for exits scenario
            // If they exit through a door, just have them go straight for the exit
            if (Scenario == ScenarioType.Exits && Simulator.Instance.getAgentPosition(id).y() > WallLength)
            {
                _agentPathsMap[id].SetCurrentGoal(_finalGoalPosition);
            }
            else if (RVOMath.absSq(Simulator.Instance.getAgentPosition(id) - _agentPathsMap[id].GetCurrentGoal()) < (ExitWidth / 2)*(ExitWidth / 2))
            {
                _agentPathsMap[id].Next();
            }
        }
    }

    private bool AllAgentsReachedFinalGoal()
    {
        // TODO: Consider this returning the number of agents that have reached their goal instead
        foreach (int id in _agentGameObjectsMap.Keys)
        {
            if (RVOMath.absSq(Simulator.Instance.getAgentPosition(id) - _agentPathsMap[id].GetLastGoal()) > GoalRadius * GoalRadius)
            {
                return false;
            }
        }

        return true;
    }

    private void AddAgent(Vector2 position, List<Vector2> path)
    {
        GameObject go = LeanPool.Spawn(_agentGameObject, new Vector3(position.x(), 0, position.y()), Quaternion.identity);
        int id = Simulator.Instance.addAgent(position);

        _agentGameObjectsMap.Add(id, go);
        _agentPathsMap.Add(id, new AgentPath(path));

        //_goalPositionsMap.Add(id, goal);
        
    }

    private void DeleteAgent(int id)
    {
        Simulator.Instance.delAgent(id);

        LeanPool.Despawn(_agentGameObjectsMap[id]);
        _agentGameObjectsMap.Remove(id);
        _agentPathsMap.Remove(id);

        //_goalPositionsMap.Remove(id);
    }

    private void DeleteAllAgents()
    {
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
