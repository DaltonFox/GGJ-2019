using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class EnemySpawner : MonoBehaviour
{
    public GameObject map;
    public GameObject pink;
    public GameObject player;

    public GameObject ghastPrefab;
    public GameObject oraclePrefab;
    public GameObject squarePrefab;

    [Range(0, 100)]
    public int maxAliveEnemies;

    [Range(0.0f, 1.0f)]
    public float ghastSpawnChance;

    [Range(0.0f, 1.0f)]
    public float oracleSpawnChance;

    [Range(0.0f, 1.0f)]
    public float squareSpawnChance;

    [Range(0.0f, 1.0f)]
    public float spawnAtPinkChance;

    [Range(0.5f, 120.0f)]
    public float spawnInterval;

    private MapGenerator mapGenerator;
    private PinkLogic pinkLogic;
    private Random spawnerRandom;

    private List<GameObject> spawnedEnemies;

    enum EnemyType
    {
        Ghast,
        Oracle,
        Square
    }

    public void StartSpawner()
    {
        StartCoroutine(SpawnerLogic());
    }

    public void StopSpawner()
    {
        StopAllCoroutines();
    }

    public void KillAllSpawned()
    {
        foreach (var enemy in spawnedEnemies)
        {
            Destroy(enemy);
        }
        spawnedEnemies.Clear();
    }

    void Start()
    {
        mapGenerator = map.GetComponent<MapGenerator>();
        pinkLogic = pink.GetComponent<PinkLogic>();
        spawnedEnemies = new List<GameObject>();
        spawnerRandom = new Random(mapGenerator.seed.GetHashCode());
        StartSpawner();
    }

    Vector3 GetSuitableSpawnLocation()
    {
        if (RollLocationChance())
        {
            return pinkLogic.GetClosestRoom();
        }

        return mapGenerator.roomCenters[spawnerRandom.Next(0, mapGenerator.roomCenters.Length - 1)];
    }

    void SpawnEnemy()
    {
        GameObject spawnedObject;

        switch (GetNextAction())
        {
            case EnemyType.Ghast:
                spawnedObject = Instantiate(ghastPrefab, GetSuitableSpawnLocation(), Quaternion.identity);
                break;
            case EnemyType.Oracle:
                spawnedObject = Instantiate(oraclePrefab, GetSuitableSpawnLocation(), Quaternion.identity);
                break;
            case EnemyType.Square:
                spawnedObject = Instantiate(squarePrefab, GetSuitableSpawnLocation(), Quaternion.identity);
                break;
            default:
                Debug.LogWarning("This should never happen!");
                return;
        }

        spawnedEnemies.Add(spawnedObject);
    }
    
    IEnumerator SpawnerLogic()
    {
        yield return new WaitForSeconds(2);

        for (;;)
        {
            if (spawnedEnemies.Count < maxAliveEnemies)
            {
                SpawnEnemy();
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    bool RollLocationChance()
    {
        float result = GetChanceResult();

        float upto = 0;
        if (upto + ghastSpawnChance >= result)
        {
            return true;
        }

        return false;
    }

    EnemyType GetNextAction()
    {
        float result = GetChanceResult();

        float upto = 0;
        if (upto + ghastSpawnChance >= result)
        {
            return EnemyType.Ghast;
        }
        upto += ghastSpawnChance;

        if (upto + oracleSpawnChance >= result)
        {
            return EnemyType.Oracle;
        }
        upto += oracleSpawnChance;

        if (upto + squareSpawnChance >= result)
        {
            return EnemyType.Square;
        }
        upto += squareSpawnChance;

        throw new ArithmeticException("Should Never Get Here, Math does not work like that!");
    }

    float GetChanceResult()
    {
        // Note Dividing by 1005 is intended to prevent function from returning 1.0f
        return spawnerRandom.Next(0, 1000) / 1005.0f;
    }
}
