using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public GameObject hitPrefab;
    public bool canInteract = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Projectile")
        {
            killMe();
        }
        else if (collision.gameObject.name == "Collision Point")
        {
            killMe();
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

        }
    }
}
