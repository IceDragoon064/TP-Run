using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NameLookAtCamera : MonoBehaviour
{
    public Camera camera;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (camera == null)
        {
            camera = Camera.main;
        }

        transform.LookAt(camera.transform);
        transform.rotation = camera.transform.rotation;
    }
}
