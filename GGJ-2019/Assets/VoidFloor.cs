using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoidFloor : MonoBehaviour
{
    public GameObject mapWalls;

    // Start is called before the first frame update
    void Start()
    {
        var shape = GetComponent<ParticleSystem>().shape;
        shape.meshRenderer = mapWalls.GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
