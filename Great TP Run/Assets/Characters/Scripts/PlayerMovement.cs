using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class PlayerMovement : NetworkComponent
{
    public Rigidbody myRig;
    public SimpleSynchronization mySync;
    public override void HandleMessage(string flag, string value)
    {
        char[] remove = { '(', ')' };
        if (flag == "VEL")
        {
            if(IsServer)
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

        if(flag == "ROT")
        {
            if(IsServer)
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

        if (IsClient)
        {
            GameObject[] playerIDs = GameObject.FindGameObjectsWithTag("manager");
            foreach (GameObject player in playerIDs)
            {
                mySync = player.GetComponent<SimpleSynchronization>();
                if (mySync.IsLocalPlayer && mySync.NetId == this.Owner)
                {
                    Debug.Log("Owner NetID: " + mySync.NetId + ", thing Owner: " + this.Owner);
                    break;
                }
            }
        }

        while (true)
        {
            if(this.IsLocalPlayer)
            {
                if(Input.GetAxisRaw("Vertical") > 0.08 || Input.GetAxisRaw("Vertical") < -0.08)
                {
                    float forward = Input.GetAxisRaw("Vertical");
                    Vector3 vel = new Vector3(0, myRig.velocity.y, 0) +
                        this.transform.forward * forward * 4.0f;
                    Debug.Log("Sending vert info");
                    SendCommand("VEL", vel.ToString());
                }
                if(Input.GetAxisRaw("Horizontal") > 0.08 || Input.GetAxisRaw("Horizontal") < -0.08)
                {
                    float Turn = Input.GetAxisRaw("Horizontal");
                    Vector3 rot = new Vector3(0, Turn * 2f, 0);

                    SendCommand("ROT", rot.ToString());
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
