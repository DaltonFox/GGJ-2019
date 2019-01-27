using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class PinkLightLogic : MonoBehaviour
{
    public float despawnDistance;
    public float secondsToFade;
    public int fadingIterations;

    private Light pinkLight;
    private Light brightLight;
    private GameObject player;
    private bool despawning;
    private bool kill;

    // Start is called before the first frame update
    void Start()
    {
        pinkLight = transform.Find("Point Light").gameObject.GetComponent<Light>();
        brightLight = transform.Find("Point Light Bright").gameObject.GetComponent<Light>();
        player = GameObject.Find("Player");
    }

    IEnumerator FadeOut(Light fadingLight)
    {
        float timeIterationAmount = secondsToFade / fadingIterations;
        float fadeIterationAmount = fadingLight.range / fadingIterations;

        for (;;)
        {
            float newValue = fadingLight.range - fadeIterationAmount;

            if (newValue > 0)
            {
                fadingLight.range = newValue;
            }
            else
            {
                break;
            }

            yield return new WaitForSeconds(timeIterationAmount);
        }

        kill = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!despawning && Vector3.Distance(transform.position, player.transform.position) < despawnDistance)
        {
            player.GetComponent<PlayerController>().AddHealth(0.15f);

            despawning = true;
            StartCoroutine(FadeOut(pinkLight));
            StartCoroutine(FadeOut(brightLight));
        }

        if (kill)
        {
            StopAllCoroutines();
            Destroy(gameObject);
        }
    }
}
