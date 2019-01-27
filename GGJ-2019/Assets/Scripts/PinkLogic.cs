using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PinkLogic : MonoBehaviour
{
    public MapGenerator mapGenerator;

    private NavMeshAgent agent;
    private bool once;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        if (agent.enabled && !once)
        {
            once = true;
            agent.SetDestination(mapGenerator.GetExitLocation());
        }
    }
}
