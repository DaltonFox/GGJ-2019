using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public GameObject hitPrefab;

    public AudioClip hitSound;
    public AudioClip ambientSound;
    public AudioClip attackSound;
    private AudioSource audioMaker;

    public bool canInteract = true;
    private NavMeshAgent agent;
    private PlayerController player;
    public float damage;
    public float drainDamage;
    private bool found = false;
    public int health = 1;
    public float pmin, pmax, hitVolume, attackVolume, spawnVolume;
    // Start is called before the first frame update
    void Start()
    {
        audioMaker = gameObject.GetComponent<AudioSource>();
        agent = gameObject.GetComponentInParent<NavMeshAgent>();
        player = GameObject.Find("Player").GetComponent<PlayerController>();
        audioMaker.volume = spawnVolume;
        audioMaker.pitch = Random.Range(pmin, pmax);
        audioMaker.PlayOneShot(ambientSound);
    }

    private void OnTriggerEnter(Collider collision)
    {
        print(collision.gameObject.name);
        if (collision.gameObject.tag == "Projectile")
        {
            audioMaker.pitch = Random.Range(pmin, pmax);
            audioMaker.volume = hitVolume;
            audioMaker.PlayOneShot(hitSound);
            health -= 1;
            if (health <= 0)
                killMe();
        }
        else if (collision.gameObject.name == "Collision Point" && drainDamage <= 0)
        {
            audioMaker.volume = hitVolume;
            audioMaker.pitch = Random.Range(pmin, pmax);
            audioMaker.PlayOneShot(hitSound);
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
        Destroy(transform.parent.gameObject, 0.1f);
    }

    // Update is called once per frame
    void Update()
    {
        if (canInteract)
        {
            agent.SetDestination(player.transform.position);

            float distance = Vector3.Distance(player.transform.position, transform.position);
            if (distance < 20 && !found)
            {
                audioMaker.volume = attackVolume;
                audioMaker.PlayOneShot(attackSound);
                found = true;
            }
        }
    }
}
