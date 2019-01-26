using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoidWall : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var shape = GetComponent<ParticleSystem>().shape;
        shape.meshRenderer = GameObject.Find("Walls").GetComponent<MeshRenderer>();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
