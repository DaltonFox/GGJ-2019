using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

public class PinkLogic : MonoBehaviour
{
    public MapGenerator mapGenerator;

    public GameObject pinkPathLightPrefab;

    [Range(0.0f, 1.0f)]
    public float winChance;

    [Range(0.0f, 1.0f)]
    public float waitForPlayerChance;

    [Range(0.0f, 1.0f)]
    public float spawnLightChance;

    [Range(0.0f, 1.0f)]
    public float spawnMimicChance;

    [Range(0.0f, 1.0f)]
    public float doNothingChance;

    public float runDistance;
    public float winDistance;

    private NavMeshAgent agent;
    private bool setDestinationOnce;

    private Vector3[] corners;
    private Vector3[] roomTraversals;
    private EnemySpawner enemySpawner;

    enum PinkStateMachine
    {
        StartUp,
        MovingToPoint,
        AwaitingPlayerToRun,
        AwaitingPlayerToWin
    }

    enum PinkAction
    {
        WaitThenRun,
        SpawnLight,
        SpawnMimic,
        DoNothing
    }

    private int pinkRoomIndex;
    private PinkStateMachine stateMachine;
    private GameObject playerGameObject;
    private Random pinkRandom;
    private bool finalActionTaken;

    public Vector3[] GetRoomTraversalPath()
    {
        return roomTraversals;
    }

    public Vector3 GetClosestRoom()
    {
        return transform.position.ClosestTo(roomTraversals);
    }

    // Start is called before the first frame update
    void Start()
    {
        playerGameObject = GameObject.Find("Player");
        agent = GetComponent<NavMeshAgent>();
        stateMachine = PinkStateMachine.StartUp;
        pinkRoomIndex = 1;
        pinkRandom = new Random(mapGenerator.seed.GetHashCode());
        enemySpawner = GameObject.Find("EnemySpawner").GetComponent<EnemySpawner>();
    }

    // Update is called once per frame
    void Update()
    {
        if (stateMachine == PinkStateMachine.StartUp)
        {
            setDestinationOnce = false;
        }

        if (Input.GetKeyDown(KeyCode.Backslash))
        {
            Vector3 p = new Vector3(transform.position.x + 3, transform.position.y - 1, transform.position.z);
            playerGameObject.transform.localPosition = p;
        }

        if (agent.enabled && !setDestinationOnce)
        {
            setDestinationOnce = true;

            var path = new NavMeshPath();
            agent.CalculatePath(mapGenerator.GetExitLocation(), path);

            corners = path.corners;

            roomTraversals = ComputeRoomTraversals(corners, mapGenerator.roomCenters);

            agent.SetDestination(roomTraversals[pinkRoomIndex]);
            pinkRoomIndex++;

            stateMachine = PinkStateMachine.MovingToPoint;
        }

        if (stateMachine == PinkStateMachine.MovingToPoint)
        {
            if (agent.remainingDistance < 0.1f)
            {
                if (pinkRoomIndex >= roomTraversals.Length)
                {
                    stateMachine = PinkStateMachine.AwaitingPlayerToWin;
                }
                else
                {
                    PinkAction action = GetNextAction();

                    if (Vector3.Distance(playerGameObject.transform.position, transform.position) < runDistance)
                    {
                        action = PinkAction.DoNothing;
                    }

                    switch (action)
                    {
                        case PinkAction.WaitThenRun:
                            stateMachine = PinkStateMachine.AwaitingPlayerToRun;
                            // Debug.Log("Waiting then Running");
                            break;
                        case PinkAction.SpawnLight:
                            SpawnLight();
                            break;
                        case PinkAction.SpawnMimic:
                            SpawnMimic();
                            break;
                        case PinkAction.DoNothing:
                            // Debug.Log("Doing nothing");
                            break;
                    }

                    if (action != PinkAction.WaitThenRun)
                    {
                        GoToNextPoint();
                    }
                }
            }
        }
        else if (stateMachine == PinkStateMachine.AwaitingPlayerToRun)
        {
            if (Vector3.Distance(playerGameObject.transform.position, transform.position) < runDistance)
            {
                playerGameObject.GetComponent<PlayerController>().AddHealth(0.5f);
                GoToNextPoint();
            }
        }
        else if (stateMachine == PinkStateMachine.AwaitingPlayerToWin)
        {
            if (!finalActionTaken && Vector3.Distance(playerGameObject.transform.position, transform.position) < winDistance)
            {
                finalActionTaken = true;
                agent.enabled = false;

                enemySpawner.StopSpawner();
                enemySpawner.KillAllSpawned();

                if (GetChanceResult() < winChance)
                {
                    Win();
                }
                else
                {
                    NextLevel();
                }
            }
        }
    }

    void GoToNextPoint()
    {
        stateMachine = PinkStateMachine.MovingToPoint;
        agent.SetDestination(roomTraversals[pinkRoomIndex]);
        pinkRoomIndex++;
    }

    void SpawnMimic()
    {
        Debug.Log("Mimic!");
    }

    void SpawnLight()
    {
        Vector3 position = transform.position;
        position.z = -5;
        Instantiate(pinkPathLightPrefab, position, Quaternion.identity);
    }

    void Win()
    {
        playerGameObject.GetComponent<PlayerController>().health = 1.5f;
        Debug.Log("You Win!!!");
        playerGameObject.GetComponent<PlayerController>().canWin = true;
    }

    void NextLevel()
    {
        playerGameObject.GetComponent<PlayerController>().health = 1.5f;
        float px = playerGameObject.transform.position.x;
        float py = playerGameObject.transform.position.y;
        mapGenerator.transform.position = new Vector3(px, py, 0);
        GameObject.Find("Map Generator Walls").transform.position = mapGenerator.transform.position;
        mapGenerator.GenerateMap();
        finalActionTaken = false;
        pinkRoomIndex = 0;
        stateMachine = PinkStateMachine.StartUp;
        winChance += 0.1f;
        enemySpawner.StartSpawner();
    }

    Vector3[] ComputeRoomTraversals(Vector3[] agentCorners, Vector3[] roomCenters)
    {
        HashSet<Vector3> rooms = new HashSet<Vector3>();

        List<Vector3> interpolatedCorners = new List<Vector3>();
        for (int i = 0; i < agentCorners.Length - 1; i++)
        {
            Vector3 p1 = agentCorners[i + 0];
            Vector3 p2 = agentCorners[i + 1];

            interpolatedCorners.Add(p1);
            interpolatedCorners.Add(Vector3.Lerp(p1, p2, 0.25f));
            interpolatedCorners.Add(Vector3.Lerp(p1, p2, 0.50f));
            interpolatedCorners.Add(Vector3.Lerp(p1, p2, 0.75f));
            interpolatedCorners.Add(p2);
        }

        foreach (Vector3 corner in interpolatedCorners)
        {
            rooms.Add(corner.ClosestTo(roomCenters));
        }

        return rooms.ToArray();
    }

    void DebugDrawRawAgentCornerGizmos()
    {
        if (corners != null)
        {
            Gizmos.color = new Color(1, 0, 1);
            for (int i = 0; i < corners.Length; i++)
            {
                Gizmos.DrawSphere(corners[i], 1);
            }
        }
    }

    void DebugDrawRoomTraversalGizmos()
    {
        if (roomTraversals != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < roomTraversals.Length; i++)
            {
                Gizmos.DrawSphere(roomTraversals[i], 2);
            }
        }
    }

    void OnDrawGizmos()
    {
        // DebugDrawRawAgentCornerGizmos();
        DebugDrawRoomTraversalGizmos();
    }

    PinkAction GetNextAction()
    {
        float result = GetChanceResult();

        float upto = 0;
        if (upto + waitForPlayerChance >= result)
        {
            return PinkAction.WaitThenRun;
        }
        upto += waitForPlayerChance;

        if (upto + spawnLightChance >= result)
        {
            return PinkAction.SpawnLight;
        }
        upto += spawnLightChance;

        if (upto + spawnMimicChance >= result)
        {
            return PinkAction.SpawnMimic;
        }
        upto += spawnMimicChance;

        if (upto + doNothingChance >= result)
        {
            return PinkAction.DoNothing;
        }
        upto += doNothingChance;

        throw new ArithmeticException("Should Never Get Here, Math does not work like that!");
    }

    float GetChanceResult()
    {
        // Note Dividing by 1005 is intended to prevent function from returning 1.0f
        return pinkRandom.Next(0, 1000) / 1005.0f;
    }
}

public static class GameUtilityExtensionMethods
{
    public static int DistanceSquaredAsInt(this Vector3 src, Vector3 other)
    {
        int x1 = (int)src.x;
        int x2 = (int)other.x;
        int y1 = (int)src.y;
        int y2 = (int)other.y;
        int z1 = (int)src.z;
        int z2 = (int)other.z;

        int p1 = x1 - x2;
        int p2 = y1 - y2;
        int p3 = z1 - z2;

        p1 *= p1;
        p2 *= p2;
        p3 *= p3;

        return p1 + p2 + p3;
    }

    public static float DistanceSquaredAsFloat(this Vector3 src, Vector3 other)
    {
        float p1 = src.x - other.x;
        float p2 = src.y - other.y;
        float p3 = src.z - other.z;

        p1 *= p1;
        p2 *= p2;
        p3 *= p3;

        return p1 + p2 + p3;
    }

    public static Vector3 ClosestTo(this Vector3 src, Vector3[] others)
    {
        if (others == null || others.Length == 0)
        {
            throw new ArgumentException("Vector3 cannot be closest to any point in a list of 0 points!");
        }

        Vector3 best = Vector3.zero;
        int bestDistance = Int32.MaxValue;
        foreach (Vector3 point in others)
        {
            int distanceSquaredBetweenPoints = src.DistanceSquaredAsInt(point);
            if (distanceSquaredBetweenPoints < bestDistance)
            {
                bestDistance = distanceSquaredBetweenPoints;
                best = point;
            }
        }
        return best;
    }

//    public static Vector3[] InterpolationList(this Vector3 p1, Vector3 p2, int count)
//    {
//        List<Vector3> interpolatedCorners = new List<Vector3>();
//
//        interpolatedCorners.Add(p1);
//        interpolatedCorners.Add(Vector3.Lerp(p1, p2, 0.25f));
//        interpolatedCorners.Add(Vector3.Lerp(p1, p2, 0.50f));
//        interpolatedCorners.Add(Vector3.Lerp(p1, p2, 0.75f));
//        interpolatedCorners.Add(p2);
//
//        return interpolatedCorners.ToArray();
//    }
}
