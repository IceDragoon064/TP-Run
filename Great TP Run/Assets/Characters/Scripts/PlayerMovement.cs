using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class PlayerMovement : NetworkComponent
{
    public Rigidbody myRig;
    public GameCharacter myChar;
    public Animator anim;

    public bool moving = false;
    public bool movingBack = false;
    public bool turning = false;
    public bool running = false;
    public bool dead = false;

    public override void HandleMessage(string flag, string value)
    {
        char[] remove = { '(', ')' };
        if (flag == "VELO")
        {
            if (IsServer)
            {
                string[] data = value.Trim(remove).Split(',');
                Vector3 vel = new Vector3(
                                                     float.Parse(data[0]),
                                                     float.Parse(data[1]),
                                                     float.Parse(data[2])
                                                     );
                myRig.velocity = vel;
            }
        }

        if (flag == "ROTATES")
        {
            if (IsServer)
            {
                string[] data = value.Trim(remove).Split(',');
                Vector3 rot = new Vector3(
                                                     float.Parse(data[0]),
                                                     float.Parse(data[1]),
                                                     float.Parse(data[2])
                                                     );
                myRig.angularVelocity = rot;
            }
        }

        if (flag == "MOVING")
        {
            moving = bool.Parse(value);

            if(IsServer)
            {
                SendUpdate("MOVING", bool.Parse(value).ToString());
            }
        }

        if (flag == "TURNING")
        {
            turning = bool.Parse(value);

            if(IsServer)
            {
                SendUpdate("TURNING", bool.Parse(value).ToString());
            }
        }
        if (flag == "BACK")
        {
            movingBack = bool.Parse(value);

            if(IsServer)
            {
                SendUpdate("BACK", bool.Parse(value).ToString());
            }
        }
    }

    public override IEnumerator SlowUpdate()
    {
        myRig = this.GetComponent<Rigidbody>();
        myChar = this.GetComponent<GameCharacter>();
        anim = this.GetComponent<Animator>();
        int movingHash = Animator.StringToHash("moving");
        int angHash = Animator.StringToHash("angularRotation");
        int backHash = Animator.StringToHash("back");
        int deadHash = Animator.StringToHash("dead");

        while (true)
        {
            if(IsLocalPlayer && !myChar.dead)
            {
                if (Input.GetAxisRaw("Vertical") > 0.08)
                {
                    float forward = Input.GetAxisRaw("Vertical");
                    Vector3 vel = new Vector3(0, myRig.velocity.y, 0) +
                        this.transform.forward * forward * myChar.velRate;
                    SendCommand("VELO", vel.ToString());
                    moving = true;
                    SendCommand("MOVING", true.ToString());
                }
                else
                {
                    if(moving)
                    {
                        SendCommand("MOVING", false.ToString());
                    }
                }
                if(Input.GetAxisRaw("Vertical") < -0.08)
                {
                    float forward = Input.GetAxisRaw("Vertical");
                    Vector3 vel = new Vector3(0, myRig.velocity.y, 0) +
                        this.transform.forward * forward * myChar.velRate;
                    SendCommand("VELO", vel.ToString());
                    movingBack = true;
                    SendCommand("BACK", true.ToString());
                }
                else
                {
                    if(movingBack)
                    {
                        SendCommand("BACK", false.ToString());
                    }
                }
                if (Input.GetAxisRaw("Horizontal") > 0.08 || Input.GetAxisRaw("Horizontal") < -0.08)
                {
                    float turn = Input.GetAxisRaw("Horizontal");
                    Vector3 rotates = new Vector3(0, turn * myChar.turnRate, 0);

                    SendCommand("ROTATES", rotates.ToString());
                    turning = true;
                    SendCommand("TURNING", true.ToString());
                }
                else
                {
                    if (turning)
                    {
                        SendCommand("TURNING", false.ToString());
                    }
                }
            }

            if (IsClient)
            {
                running = myChar.spedUp;
                anim.SetBool("running", myChar.spedUp);
                anim.SetBool(deadHash, myChar.dead);
                //Probably changing to boolean stuff but tired now
                if(moving || turning || movingBack)
                {
                    anim.SetBool(movingHash, true);
                }
                else
                {
                    anim.SetBool(movingHash, false);
                }
                if(myChar.spedUp)
                {
                    if (movingBack)
                    {
                        anim.SetBool(backHash, true);
                    }
                    else
                    {
                        anim.SetBool(backHash, false);
                    }
                }
                
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
