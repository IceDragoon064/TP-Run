using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public GameManagingScript manager;
    public Camera cam;
    public bool set = false;

    // Start is called before the first frame update
    void Start()
    {
        manager = FindObjectOfType<GameManagingScript>();
        cam = FindObjectOfType<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if(manager.GameStarted && !set && !manager.IsServer)
        {
            GameCharacter[] players = FindObjectsOfType<GameCharacter>();

            foreach (GameCharacter player in players)
            {
                if (player.IsLocalPlayer)
                {
                    target = player.transform;
                    set = true;
                    break;
                }
            }
        }

        if(manager.GameStarted)
        {
            cam.transform.position = target.transform.position + new Vector3(0, 5f, 0);
            transform.LookAt(target);
        }
    }
}
