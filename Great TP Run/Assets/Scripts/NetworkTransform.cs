using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class NetworkTransform : NetworkComponent
{
    public Vector3 LastPosition = Vector3.zero;
    public Vector3 LastRotation = Vector3.zero;

    public override void HandleMessage(string flag, string value)
    {
        //(x,y,z)
        //Have to remove parenthesis from the ToString of vector3
        char[] remove = {'(', ')'};
        if(flag == "POS")
        {
            //naive approach
            string[] data = value.Trim(remove).Split(',');

            //If you have rigid body
            //Find the distance between client position and server update position.
            //If distance <.1 -- ignore      Probably close enough we don't have to worry about it
            //else if distance <.5 -- lerp   
            //else -- teleport               Lerp will look really bad if greater than .5, just set it

            Vector3 target = new Vector3(
                                                 float.Parse(data[0]),
                                                 float.Parse(data[1]),
                                                 float.Parse(data[2])
                                                 );

            
            this.transform.position = target;
        }

        if(flag == "ROT")
        {
            //naive approach
            string[] data = value.Trim(remove).Split(',');
            Vector3 euler = new Vector3(
                                                 float.Parse(data[0]),
                                                 float.Parse(data[1]),
                                                 float.Parse(data[2])
                                                 );
            this.transform.rotation = Quaternion.Euler(euler);
        }
    }

    public override IEnumerator SlowUpdate()
    {
        //The server will be the only thing running the slow update. It is listening for changes in the transform of the object
        while(IsServer)
        {
            //Is the position different?
            if(LastPosition != this.transform.position)
            {
                //SendUpdate
                SendUpdate("POS", this.transform.position.ToString());
                LastPosition = this.transform.position;
            }

            //Is the rotation different?
            //Synchronizing Euler angles (can do quaternions)
            if (LastRotation != this.transform.rotation.eulerAngles)
            {
                SendUpdate("ROT", this.transform.rotation.eulerAngles.ToString());
                LastRotation = this.transform.rotation.eulerAngles;
            }

            //Scale? (rare, but if I need it, it goes here.)

            if(IsDirty)
            {
                SendUpdate("POS", this.transform.position.ToString());
                SendUpdate("ROT", this.transform.rotation.eulerAngles.ToString());
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
