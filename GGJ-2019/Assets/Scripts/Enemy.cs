using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{

    public GameObject hitPrefab;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Projectile")
        {
            var hit = (GameObject)Instantiate(hitPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
        else if (collision.gameObject.name == "Collision Point")
        {
            var hit = (GameObject)Instantiate(hitPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
