using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using RVO;
using Vector2 = RVO.Vector2;
using Random = System.Random;

using Lean;

public class BoxExample : MonoBehaviour
{

    public GameObject AgentGameObject;
    public GameObject ObstacleGameObject;

    private IList<Vector2> _goals = new List<Vector2>();
    private Random _random = new Random();

    private IList<GameObject> _agentGameObjects = new List<GameObject>();

    private float _time = 0;

    private List<List<Vector2>> _obstacles = new List<List<Vector2>>();

    // Start is called before the first frame update
    void Start()
    {
        SetupScenario();
    }

    // Update is called once per frame
    void Update()
    {
        if (!ReachedGoal())
        {
            UpdateVisualization();
            SetPreferedVelocities();
            Simulator.Instance.doStep();

            _time += Time.deltaTime;
        }
        else
        {
            // TODO: Rerunning can be done by
            // Clear(), getGlobalTime()
            // But need to clean up gameobjects then as well
            // Then, run SetupScenario again.
            // Check GameMainManager.cs for how to properly keep track
            // of gameobjects

            // Metrics:
            // Agents evacuated vs. time -> Collect for EVERY run
            // Time to evacuate -> Collect for EVERY run
            // Per: number of agents, number of doors [1, 2, 3, 4]

            // Statistics: Average time to evacuate per indep.
            //             Average evacuated vs. time per indep.
            // T-tests for significance testing between scenarios
            // Presentation: One bar graph per number of agents, one bar per nr of doors
            // Or, a multi-bar graph
            // Or a graph with legend for nr of agents, x-axis: #doors, y-axis: avg. time

            //Simulator.Instance.Clear();
            // Debug.Log(_time);
        }
    }

    /// <summary>
    /// Adds agent to simulation, and as a gameobject
    /// </summary>
    /// <param name="position"></param>
    private void AddAgent(Vector2 position)
    {
        GameObject go = LeanPool.Spawn(AgentGameObject, new Vector3(position.x(), 0, position.y()), Quaternion.identity);
        _agentGameObjects.Add(go);

        Simulator.Instance.addAgent(position);
    } 

    private void SetupScenario()
    {
        Simulator.Instance.setTimeStep(0.25f);

        // Max speed is per timestep I think?
        // So to get 1 m/s, do 0.25 as max
        // No, see comment in SetPreferredVelocitites()
        float maxSpeed = 0.25f;
        float agentRadius = 1.0f;
        Simulator.Instance.setAgentDefaults(15.0f, 10, 5.0f, 5.0f, agentRadius, maxSpeed, new Vector2(0.0f, 0.0f));

        for (int i = 0; i < 5; ++i)
        {
            for (int j = 0; j < 5; ++j)
            {


                //Simulator.Instance.addAgent(new Vector2(55.0f + i * 10.0f, 55.0f + j * 10.0f));
                AddAgent(new Vector2(55.0f + i * 10.0f, 55.0f + j * 10.0f));
                _goals.Add(new Vector2(-75.0f, -75.0f));

                //Simulator.Instance.addAgent(new Vector2(-55.0f - i * 10.0f, 55.0f + j * 10.0f));
                AddAgent(new Vector2(-55.0f - i * 10.0f, 55.0f + j * 10.0f));
                _goals.Add(new Vector2(75.0f, -75.0f));

                //Simulator.Instance.addAgent(new Vector2(55.0f + i * 10.0f, -55.0f - j * 10.0f));
                AddAgent(new Vector2(55.0f + i * 10.0f, -55.0f - j * 10.0f));
                _goals.Add(new Vector2(-75.0f, 75.0f));

                //Simulator.Instance.addAgent(new Vector2(-55.0f - i * 10.0f, -55.0f - j * 10.0f));
                AddAgent(new Vector2(-55.0f - i * 10.0f, -55.0f - j * 10.0f));
                _goals.Add(new Vector2(75.0f, 75.0f));
            }
        }

        // TODO: Fix this mess
        List<Vector2> obstacle1 = new List<Vector2>();
        obstacle1.Add(new Vector2(-10.0f, 40.0f));
        obstacle1.Add(new Vector2(-40.0f, 40.0f));
        obstacle1.Add(new Vector2(-40.0f, 10.0f));
        obstacle1.Add(new Vector2(-10.0f, 10.0f));
        Simulator.Instance.addObstacle(obstacle1);
        _obstacles.Add(obstacle1);

        List<Vector2> obstacle2 = new List<Vector2>();
        obstacle2.Add(new Vector2(10.0f, 40.0f));
        obstacle2.Add(new Vector2(10.0f, 10.0f));
        obstacle2.Add(new Vector2(40.0f, 10.0f));
        obstacle2.Add(new Vector2(40.0f, 40.0f));
        Simulator.Instance.addObstacle(obstacle2);
        _obstacles.Add(obstacle2);  

        List<Vector2> obstacle3 = new List<Vector2>();
        obstacle3.Add(new Vector2(10.0f, -40.0f));
        obstacle3.Add(new Vector2(40.0f, -40.0f));
        obstacle3.Add(new Vector2(40.0f, -10.0f));
        obstacle3.Add(new Vector2(10.0f, -10.0f));
        Simulator.Instance.addObstacle(obstacle3);
        _obstacles.Add(obstacle3);

        List<Vector2> obstacle4 = new List<Vector2>();
        obstacle4.Add(new Vector2(-10.0f, -40.0f));
        obstacle4.Add(new Vector2(-10.0f, -10.0f));
        obstacle4.Add(new Vector2(-40.0f, -10.0f));
        obstacle4.Add(new Vector2(-40.0f, -40.0f));
        Simulator.Instance.addObstacle(obstacle4);
        _obstacles.Add(obstacle4);

        RenderObstacles();

        Simulator.Instance.processObstacles();
    }

    private void SetPreferedVelocities()
    {
        for (int i = 0; i < Simulator.Instance.getNumAgents(); ++i)
        {
            Vector2 goalVector = _goals[i] - Simulator.Instance.getAgentPosition(i);

            if (RVOMath.absSq(goalVector) > 1.0f)
            {
                goalVector = RVOMath.normalize(goalVector);
            }

            // TODO: Why are agents so much faster than 1m/s?
            // Because doStep() is called in Update()
            // doStep simulates 0.25 seconds, and Update() is called
            // way more then 4 times a second
            Simulator.Instance.setAgentPrefVelocity(i, goalVector);
            //Debug.Log(goalVector);

            /* Perturb a little to avoid deadlocks due to perfect symmetry. */
            float angle = (float)_random.NextDouble() * 2.0f * (float)Math.PI;
            float dist = (float)_random.NextDouble() * 0.0001f;

            Simulator.Instance.setAgentPrefVelocity(i, Simulator.Instance.getAgentPrefVelocity(i) +
                dist * new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)));
        }
    }

    private void UpdateVisualization()
    {
        for (int i = 0; i < Simulator.Instance.getNumAgents(); i++)
        {

            Vector2 pos = Simulator.Instance.getAgentPosition(i);
            Vector2 vel = Simulator.Instance.getAgentPrefVelocity(i);
            _agentGameObjects[i].transform.position = new Vector3(pos.x(), transform.position.y, pos.y());

            if (Math.Abs(vel.x()) > 0.01f && Math.Abs(vel.y()) > 0.01f)
            {
                _agentGameObjects[i].transform.forward = new Vector3(vel.x(), 0, vel.y()).normalized;
            }
             
        }
    }

    private void RenderObstacles()
    {
        for (int i = 0; i < _obstacles.Count; i++)
        {
            GameObject ga = Instantiate(ObstacleGameObject, Vector3.zero, Quaternion.identity);
            Mesh mesh = ga.GetComponent<MeshFilter>().mesh;

            Vector2[] vertices2D = _obstacles[i].ToArray();

            List<Vector3> vertices3D = new List<Vector3>();

            for (int v = 0; v < vertices2D.Length; v++)
            {
                vertices3D.Add(new Vector3(vertices2D[v].x(), 1, vertices2D[v].y()));
            }
            
            mesh.vertices = vertices3D.ToArray();

            // Vertex indices are backwards such that the normals point
            // towards the camera
            int[] triangles = new int[6] {2, 1, 0, 3, 2, 0 };
            mesh.triangles = triangles;
        }
    }

    private bool ReachedGoal()
    {
        for (int i = 0; i < Simulator.Instance.getNumAgents(); ++i)
        {
            // What is this 400 constant? Becomes 20 in distance, I guess thats the radius
            // of an agent. No, it's just the threshold for being close enough to the goal.
            if (RVOMath.absSq(Simulator.Instance.getAgentPosition(i) - _goals[i]) > 400.0f)
            {
                return false;
            }
        }

        return true;
    }

}
