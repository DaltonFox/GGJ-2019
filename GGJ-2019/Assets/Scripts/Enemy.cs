using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public GameObject hitPrefab;
    public bool canInteract = true;
    private NavMeshAgent agent;
    private PlayerController player;
    public float damage;
    public float drainDamage;
    // Start is called before the first frame update
    void Start()
    {
        agent = gameObject.GetComponentInParent<NavMeshAgent>();
        player = GameObject.Find("Player").GetComponent<PlayerController>();
    }

    private void OnTriggerEnter(Collider collision)
    {
        print(collision.gameObject.name);
        if (collision.gameObject.tag == "Projectile")
        {
            killMe();
        }
        else if (collision.gameObject.name == "Collision Point" && drainDamage <= 0)
        {
            player.health -= damage;
            killMe();
        }
        
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.name == "Collision Point")
        {
            player.health -= drainDamage;
        }
    }

    public void killMe()
    {
        var hit = (GameObject)Instantiate(hitPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (canInteract)
        {
            agent.SetDestination(GameObject.Find("Player").transform.localPosition);
        }
    }
}
