using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class timeout : MonoBehaviour
{
    public float duration = 0.25f;
    private float life = 0;

    private void Update()
    {
        life += Time.deltaTime;
        if (life > duration)
            Destroy(gameObject);
    }

}
