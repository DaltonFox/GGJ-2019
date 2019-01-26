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

    public float projectileSpeed;
    public GameObject projectilePrefab;
    public GameObject muzzlePrefab;

    private Transform pivot;
    private Transform chevron;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        chevron = gameObject.transform.Find("Pivot/Chevron");
        pivot = gameObject.transform.Find("Pivot");
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

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 target = Camera.main.ScreenToWorldPoint(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
            Vector2 myPos = new Vector2(transform.position.x, transform.position.y);
            Vector2 direction = target - myPos;
            direction.Normalize();
            GameObject projectile = (GameObject) Instantiate(projectilePrefab, myPos, Quaternion.identity);
            projectile.GetComponent<Rigidbody>().velocity = direction * projectileSpeed;

            Vector2 mousePos = Input.mousePosition;
            Vector3 screenPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, transform.position.z - Camera.main.transform.position.z));
            projectile.transform.eulerAngles = new Vector3(0, 0, Mathf.Atan2(screenPos.y - transform.position.y, screenPos.x - transform.position.x) * Mathf.Rad2Deg);

            pivot.transform.eulerAngles = new Vector3(0, 0, Mathf.Atan2(screenPos.y - transform.position.y, screenPos.x - transform.position.x) * Mathf.Rad2Deg);
            var muzzle = (GameObject)Instantiate(muzzlePrefab, chevron.position, pivot.rotation);

        }

    }
}
