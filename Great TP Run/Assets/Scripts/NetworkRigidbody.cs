using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class NetworkRigidbody : NetworkComponent
{
    public Rigidbody myRig;
    public Vector3 LastPosition = Vector3.zero;
    public Vector3 LastRotation = Vector3.zero;
    public Vector3 LastVelocity = Vector3.zero;
    public Vector3 LastAngularVelocity = Vector3.zero;

    public override void HandleMessage(string flag, string value)
    {
        char[] remove = { '(', ')' };
        if (flag == "POS")
        {
            string[] data = value.Trim(remove).Split(',');
            Vector3 target = new Vector3(
                                                 float.Parse(data[0]),
                                                 float.Parse(data[1]),
                                                 float.Parse(data[2])
                                                 );

            if ((target - this.transform.position).magnitude < .5f)
            {
                //lerp
                this.transform.position = Vector3.Lerp(this.transform.position, target, .25f);
            }
            else
            {
                this.transform.position = target;
            }
        }

        if (flag == "ROT")
        {
    
            string[] data = value.Trim(remove).Split(',');
            Vector3 euler = new Vector3(
                                                 float.Parse(data[0]),
                                                 float.Parse(data[1]),
                                                 float.Parse(data[2])
                                                 );
            this.transform.rotation = Quaternion.Euler(euler);
        }

        if (flag == "VEL")
        {
            string[] data = value.Trim(remove).Split(',');
            Vector3 vel = new Vector3(
                                                 float.Parse(data[0]),
                                                 float.Parse(data[1]),
                                                 float.Parse(data[2])
                                                 );


            this.myRig.velocity = vel;
        }

        if (flag == "ANG")
        {
            string[] data = value.Trim(remove).Split(',');
            Vector3 angRot = new Vector3(
                                                 float.Parse(data[0]),
                                                 float.Parse(data[1]),
                                                 float.Parse(data[2])
                                                 );


            this.myRig.angularVelocity = angRot;
        }
    }

    public override IEnumerator SlowUpdate()
    {
        while(IsServer)
        {
            //Is the position different?
            if (LastPosition != this.transform.position)
            {
                //SendUpdate
                SendUpdate("POS", this.transform.position.ToString());
                LastPosition = this.transform.position;
            }

            //Is the rotation different?
            //Synchronizing Euler angles (can do quaternions)

            if (LastRotation != transform.rotation.eulerAngles)
            {
                SendUpdate("ROT", transform.rotation.eulerAngles.ToString());
                LastRotation = transform.rotation.eulerAngles;
            }

            if(LastVelocity != this.myRig.velocity)
            {
                SendUpdate("VEL", this.myRig.velocity.ToString());
                LastVelocity = this.myRig.velocity;
            }

            if(LastAngularVelocity != this.myRig.angularVelocity)
            {
                SendUpdate("ANG", this.myRig.angularVelocity.ToString());
                LastAngularVelocity = this.myRig.angularVelocity;
            }
            //Scale? (rare, but if I need it, it goes here.)

            if (IsDirty)
            {
                SendUpdate("POS", this.transform.position.ToString());
                SendUpdate("ROT", this.transform.rotation.eulerAngles.ToString());
                SendUpdate("VEL", this.myRig.velocity.ToString());
                SendUpdate("ANG", this.myRig.angularVelocity.ToString());
                IsDirty = false;
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
