using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class PlayerMovement : NetworkComponent
{
    public Rigidbody myRig;
    public GameCharacter myChar;
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
    }

    public override IEnumerator SlowUpdate()
    {
        myRig = this.GetComponent<Rigidbody>();
        myChar = this.GetComponent<GameCharacter>();

        while (true)
        {
            if(this.IsLocalPlayer)
            {
                if (Input.GetAxisRaw("Vertical") > 0.08 || Input.GetAxisRaw("Vertical") < -0.08)
                {
                    float forward = Input.GetAxisRaw("Vertical");
                    Vector3 vel = new Vector3(0, myRig.velocity.y, 0) +
                        this.transform.forward * forward * myChar.velRate;
                    SendCommand("VELO", vel.ToString());
                }
                if (Input.GetAxisRaw("Horizontal") > 0.08 || Input.GetAxisRaw("Horizontal") < -0.08)
                {
                    float turn = Input.GetAxisRaw("Horizontal");
                    Vector3 rotates = new Vector3(0, turn * myChar.turnRate, 0);

                    SendCommand("ROTATES", rotates.ToString());
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
