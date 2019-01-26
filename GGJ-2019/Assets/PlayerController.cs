using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody m_Rigidbody;
    public float max_speed;
    public float acceleration;
    private Vector3 movement;
    private float x, y = 0;


    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (m_Rigidbody.velocity.magnitude < max_speed)
            m_Rigidbody.AddForce(movement * acceleration);
    }


    void Update()
    {
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");
        movement = new Vector3(x, y, 0);
    }
}
