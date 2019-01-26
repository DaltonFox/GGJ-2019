using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private GameObject target;

    private SphereCollider col;
    private Vector2 targetDir;
    private bool locked = false;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        target = GameObject.Find("Target");
        col = gameObject.transform.Find("Collision Point").GetComponent<SphereCollider>();
        chevron = gameObject.transform.Find("Pivot/Chevron");
        pivot = gameObject.transform.Find("Pivot");
    }

    private void FixedUpdate()
    {
        if (m_Rigidbody.velocity.magnitude < max_speed && !locked)
            m_Rigidbody.AddForce(movement * acceleration);
        else if (locked)
            m_Rigidbody.velocity = targetDir * max_speed * 3;
    }

    void endSequence()
    {
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void stepSequence()
    {
        Time.timeScale -= Time.deltaTime;
        if (Time.timeScale < 0.4f)
            Time.timeScale = 0.4f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        float expand = Time.deltaTime / 20;
        Vector3 expander = new Vector3(expand, expand, 0);
        transform.localScale += expander;
        target.transform.localScale += expander;
    }

    IEnumerator holdSequence(float duration)
    {
        float totalTime = 0;
        while (totalTime <= duration)
        {
            totalTime += Time.deltaTime;
            stepSequence();
            yield return null;
        }
        endSequence();
    }

    void Update()
    {
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");
        movement = new Vector3(x, y, 0);
        
        Vector3 myPos3 = new Vector3(transform.position.x, transform.position.y, 0);
        Vector3 tPos = new Vector3(target.transform.position.x, target.transform.position.y, 0);
        if (Vector3.Distance(myPos3, tPos) < 4.0f)
        {
            locked = true;
            targetDir = tPos - myPos3;
            targetDir.Normalize();
            col.enabled = false;
            StartCoroutine(holdSequence(1.25f));
        }

        if (Input.GetMouseButtonDown(0) && !locked)
        {
            Vector2 myPos = new Vector2(transform.position.x, transform.position.y);
            Vector2 target = Camera.main.ScreenToWorldPoint(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
            
            Vector2 direction = target - myPos;
            direction.Normalize();
            GameObject projectile = (GameObject) Instantiate(projectilePrefab, new Vector3(myPos.x, myPos.y, -2), Quaternion.identity);
            projectile.GetComponent<Rigidbody>().velocity = direction * projectileSpeed;

            Vector2 mousePos = Input.mousePosition;
            Vector3 screenPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, transform.position.z - Camera.main.transform.position.z));
            projectile.transform.eulerAngles = new Vector3(0, 0, Mathf.Atan2(screenPos.y - transform.position.y, screenPos.x - transform.position.x) * Mathf.Rad2Deg);

            pivot.transform.eulerAngles = new Vector3(0, 0, Mathf.Atan2(screenPos.y - transform.position.y, screenPos.x - transform.position.x) * Mathf.Rad2Deg);
            var muzzle = (GameObject)Instantiate(muzzlePrefab, chevron.position, pivot.rotation);

        }
    }
}
