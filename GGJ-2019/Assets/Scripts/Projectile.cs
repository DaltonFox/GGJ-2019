﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{

    public GameObject hitPrefab;
    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, 3.0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name != "Player")
        {
            var hit = (GameObject)Instantiate(hitPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

    public static explicit operator GameObject(Projectile v)
    {
        throw new NotImplementedException();
    }
}
