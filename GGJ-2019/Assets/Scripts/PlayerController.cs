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
    private AudioManager musicManager;
    private bool locked = false;
    private bool ending = false;

    private Material admissive;
    private float admission;
    private AudioSource soundMaker;
    public AudioClip shootSound;
    public bool canwin;

    void Start()
    {
        musicManager = GameObject.Find("Music Manager").GetComponent<AudioManager>();
        musicManager.startMainLoop();
        m_Rigidbody = GetComponent<Rigidbody>();
        target = GameObject.Find("Target");
        col = gameObject.transform.Find("Collision Point").GetComponent<SphereCollider>();
        chevron = gameObject.transform.Find("Pivot/Chevron");
        pivot = gameObject.transform.Find("Pivot");
        admissive = transform.Find("Sprite/Glow").gameObject.GetComponent<Renderer>().material;
        admission = admissive.GetFloat("_EmissionGain");
        soundMaker = GetComponent<AudioSource>();
    }

    public bool mid = false;
    private void FixedUpdate()
    {
        if (m_Rigidbody.velocity.magnitude < max_speed && !locked)
            m_Rigidbody.AddForce(movement * acceleration);
        else if (locked && mid && !ending)
            m_Rigidbody.velocity = targetDir * (max_speed / 4);
        else if (locked && ending)
            target.GetComponent<Rigidbody>().velocity = -targetDir * max_speed * 1.5f;
    }

    void endSequence()
    {
        Enemy[] enemies = GameObject.FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in enemies)
            enemy.killMe();
        StartCoroutine(holdSequence3(1.80f));
    }

    void stepSequence1()
    {
        float expand = Time.deltaTime * 2;
        Vector3 expander = new Vector3(expand, expand, 0);
        transform.localScale += expander;
        target.transform.localScale += expander;
    }

    void stepSequence2()
    {
        float expand = Time.deltaTime * 2;
        Vector3 expander = new Vector3(expand, expand, 0);
        transform.localScale += expander;
        target.transform.localScale += expander;
    }

    void stepSequenceEnd()
    {
        if (admission < 0.4f)
            admission += Time.deltaTime / 10;

        admissive.SetFloat("_EmissionGain", admission);
    }

    void stepSequenceTransition()
    {
        if (Camera.main.orthographicSize > 0)
            Camera.main.orthographicSize -= Time.deltaTime * 15;
    }

    void startMid()
    {
        mid = true;
        StartCoroutine(holdSequence1_5(1.50f));
    }

    void startTransition()
    {
        StartCoroutine(holdSequence4(4.25f));
    }

    void startMidEnd()
    {
        mid = false;
        StartCoroutine(holdSequence1_55(2f));
    }

    IEnumerator holdSequence1(float duration)
    {
        float totalTime = 0;
        while (totalTime <= duration)
        {
            totalTime += Time.deltaTime;
            yield return null;
        }
        startMid();
    }

    IEnumerator holdSequence1_5(float duration)
    {
        float totalTime = 0;
        while (totalTime <= duration)
        {
            totalTime += Time.deltaTime;
            yield return null;
        }
        startMidEnd();
    }

    IEnumerator holdSequence1_55(float duration)
    {
        float totalTime = 0;
        while (totalTime <= duration)
        {
            totalTime += Time.deltaTime;
            yield return null;
        }
        startEnd();
    }

    IEnumerator holdSequence3(float duration)
    {
        float totalTime = 0;
        while (totalTime <= duration)
        {
            totalTime += Time.deltaTime;
            stepSequenceEnd();
            yield return null;
        }
        startTransition();
    }

    IEnumerator holdSequence4(float duration)
    {
        float totalTime = 0;
        while (totalTime <= duration)
        {
            totalTime += Time.deltaTime;
            stepSequenceTransition();
            yield return null;
        }
        SceneManager.LoadScene("MenuScene");
    }

    void startEnd()
    {
        ending = true;
        StartCoroutine(holdSequence2(6f));
    }

    IEnumerator holdSequence2(float duration)
    {
        float totalTime = 0;
        while (totalTime <= duration)
        {
            totalTime += Time.deltaTime;
            stepSequence2();
            yield return null;
        }
        endSequence();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Bolt")
        {
            Destroy(collision.gameObject);
        }
    }

    void Update()
    {
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");
        movement = new Vector3(x, y, 0);
        
        Vector3 myPos3 = new Vector3(transform.position.x, transform.position.y, 0);
        Vector3 tPos = new Vector3(target.transform.position.x, target.transform.position.y, 0);
        if (canwin)
        {
            
            targetDir = tPos - myPos3;
            targetDir.Normalize();
            
            if (!locked)
            {
                Range[] rangers = GameObject.FindObjectsOfType<Range>();
                foreach (Range ranger in rangers)
                    ranger.canShoot = false;
                Enemy[] enemies = GameObject.FindObjectsOfType<Enemy>();
                foreach (Enemy enemy in enemies)
                    enemy.canInteract = false;
                col.enabled = false;
                target.gameObject.GetComponentInChildren<SphereCollider>().enabled = false;
                musicManager.startEndLoop();
                StartCoroutine(holdSequence1(2.5f));
            }
            
            locked = true;
        }

        if (Input.GetMouseButtonDown(0) && !locked)
        {
            soundMaker.pitch = Random.Range(1.15f, 1.5f);
            soundMaker.PlayOneShot(shootSound);
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
