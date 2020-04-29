using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class BadGuyMovement : NetworkComponent
{
    public int health = 3;
    public float speedX = .1f;
    public float speedZ = 0;
    public GameManagingScript manager;
    public float directionTimer = 3f;
    public float directionReset = 3f;
    public float directionLongReset = 5f;
    public bool isInfected = false;
    public Animator anim;

    public int routeNumber = 0;
    public override void HandleMessage(string flag, string value)
    {
       
    }

    public override IEnumerator SlowUpdate()
    {
        manager = FindObjectOfType<GameManagingScript>();
        anim = this.GetComponent<Animator>();
        int movingHash = Animator.StringToHash("moving");

        //Changing the direction the bad guy is facing based on its initial speed.
        if (speedZ > 0)
        {
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0, 270, 0));
        }
        else if (speedZ < 0)
        {
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0, 90, 0));
        }
        else if (speedX < 0)
        {
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0, 180, 0));
        }

        //Bad guy patrols forward and backwards for a specific amount time.
        while (true)
        {
            if(IsServer && manager.GameStarted && !manager.GameEnded)
            {
                if(routeNumber == 1)
                {
                    transform.position += new Vector3(speedX, 0, speedZ);

                    if (directionTimer > 0)
                    {
                        directionTimer -= MyCore.MasterTimer;
                    }
                    else
                    {
                        speedX *= -1;
                        speedZ *= -1;

                        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0, 180, 0));


                        directionTimer = directionReset;
                    }
                }
                else if(routeNumber == 2)
                {
                    transform.position += new Vector3(speedX, 0, speedZ);

                    if (directionTimer > 0)
                    {
                        directionTimer -= MyCore.MasterTimer;
                    }
                    else
                    {
                        //Going left looking at it from the houses direction. Switch directions to go away from houses.
                        if(speedX > 0 && speedZ == 0) 
                        {
                            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0, 90, 0));

                            speedX = 0;
                            speedZ = -.1f;

                            directionTimer = directionLongReset;
                        }
                        else if(speedZ < 0 && speedX == 0)
                        {
                            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0, 90, 0));

                            speedX = -.1f;
                            speedZ = 0;

                            directionTimer = directionReset;
                        }
                        else if(speedX < 0 && speedZ == 0)
                        {
                            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0, 90, 0));

                            speedX = 0;
                            speedZ = .1f;

                            directionTimer = directionLongReset;
                        }
                        else if(speedZ > 0 && speedX == 0)
                        {
                            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0, 90, 0));

                            speedX = .1f;
                            speedZ = 0;

                            directionTimer = directionReset;
                        }
                    }
                }
                else if(routeNumber == 2)
                {

                }
                if (health <= 0)
                {
                    MyCore.NetDestroyObject(this.NetId);
                }
            }

            if (IsClient)
            {
                anim.SetBool(movingHash, true);
            }

            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
