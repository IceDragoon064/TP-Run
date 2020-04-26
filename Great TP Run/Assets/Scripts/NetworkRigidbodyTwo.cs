using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

[RequireComponent(typeof(Rigidbody))]
public class NetworkRigidbodyTwo : NetworkComponent
{
    public Rigidbody MyRig;
    public Vector3 LastPosition = Vector3.zero;
    public Vector3 LastVelocity = Vector3.zero;
    public Vector3 LastRotation = Vector3.zero;
    public Vector3 LastRotAcc = Vector3.zero;
    public bool SyncAngVel = false;
    public float threshold;

    public Vector3 VelocityCorrect;
    public Vector3 LastValidVelocity;

    public override void HandleMessage(string flag, string value)
    {
        //Vector 3 format is 
        //(x,y,z)  float...
        if (flag == "POS")
        {
            char[] remove = { '(', ')' };
            string[] temp = value.Trim(remove).Split(',');

            Vector3 target = new Vector3(float.Parse(temp[0]), float.Parse(temp[1]), float.Parse(temp[2]));
            float distance = (MyRig.position - target).magnitude;

            if (distance < .05f || MyRig.velocity.magnitude < .01f)
            {
                MyRig.position = target;
                VelocityCorrect = Vector3.zero;

            }
            if (distance <= 2 * threshold)
            {
                VelocityCorrect = (target - MyRig.position);
                MyRig.velocity = LastValidVelocity + VelocityCorrect;

            }

            else if (distance > 2 * threshold)
            {
                MyRig.position = target;
            }

        }
        if (flag == "ROT")
        {
            char[] remove = { '(', ')' };
            string[] temp = value.Trim(remove).Split(',');

            Vector3 target = new Vector3(float.Parse(temp[0]), float.Parse(temp[1]), float.Parse(temp[2]));
            MyRig.rotation = Quaternion.Euler(target);
        }
        if (flag == "VEL")
        {
            char[] remove = { '(', ')' };
            string[] temp = value.Trim(remove).Split(',');
            LastValidVelocity = new Vector3(float.Parse(temp[0]), float.Parse(temp[1]), float.Parse(temp[2]));
            //This only works if the player controller is stopping the movement when no input is given.
            if (LastValidVelocity != Vector3.zero)
            {
                MyRig.velocity = LastValidVelocity + VelocityCorrect;
            }
            else
            {
                MyRig.velocity = Vector3.zero;
            }
        }
        if (flag == "ANG")
        {
            char[] remove = { '(', ')' };
            string[] temp = value.Trim(remove).Split(',');
            MyRig.angularVelocity = new Vector3(float.Parse(temp[0]), float.Parse(temp[1]), float.Parse(temp[2]));
        }
    }

    public override IEnumerator SlowUpdate()
    {
        while (IsServer)
        {
            if ((LastPosition - MyRig.position).magnitude > threshold)
            {
                SendUpdate("POS", MyRig.position.ToString("F4"));
                LastPosition = MyRig.position;
            }
            if ((LastRotation - MyRig.rotation.eulerAngles).magnitude > threshold)
            {
                SendUpdate("ROT", MyRig.rotation.eulerAngles.ToString("F4"));
                LastRotation = MyRig.rotation.eulerAngles;
            }
            if ((LastVelocity - MyRig.velocity).magnitude > threshold)
            {
                SendUpdate("VEL", MyRig.velocity.ToString("F4"));
                LastVelocity = MyRig.velocity;
            }
            if (SyncAngVel && (LastRotAcc - MyRig.angularVelocity).magnitude > threshold)
            {
                SendUpdate("ANG", MyRig.angularVelocity.ToString("F4"));
                LastRotAcc = MyRig.angularVelocity;
            }
            if (IsDirty)
            {
                SendUpdate("POS", MyRig.position.ToString("F4"));
                SendUpdate("ROT", MyRig.rotation.eulerAngles.ToString("F4"));
                SendUpdate("VEL", MyRig.velocity.ToString("F4"));
                if (SyncAngVel)
                {
                    SendUpdate("ANG", MyRig.angularVelocity.ToString("F4"));
                }
                IsDirty = false;
            }
            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        MyRig = this.gameObject.GetComponent<Rigidbody>();
        if (threshold < 0.1f)
        {
            threshold = .5f;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
