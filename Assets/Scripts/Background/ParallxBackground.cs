using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallxBackground : MonoBehaviour
{
    private GameObject cam;
    [SerializeField] private float parallaxEffect;
    private float xPosition;
    private float yPosition;


    void Start()
    {
        cam = GameObject.Find("Virtual Camera");
        xPosition = cam.transform.position.x;
        yPosition = cam.transform.position.y;
    }

    void Update()
    {
        transform.position = new Vector3(
            xPosition + cam.transform.position.x - parallaxEffect, 
            yPosition + cam.transform.position.y - parallaxEffect, 
            transform.position.z
        );
    }
}
