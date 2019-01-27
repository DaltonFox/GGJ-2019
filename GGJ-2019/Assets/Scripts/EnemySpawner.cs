using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject map;
    public GameObject pink;
    public GameObject player;

    [Range(0, 100)]
    public int maxAliveEnemies;

    [Range(0.0f, 1.0f)]
    public float ghastSpawnChance;

    [Range(0.0f, 1.0f)]
    public float oracleSpawnChance;

    [Range(0.0f, 1.0f)]
    public float squareSpawnChance;

    private MapGenerator mapGenerator;
    private PinkLogic pinkLogic;

    private int aliveEnemies = 0;

    // Start is called before the first frame update
    void Start()
    {
        mapGenerator = map.GetComponent<MapGenerator>();
        pinkLogic = pink.GetComponent<PinkLogic>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
