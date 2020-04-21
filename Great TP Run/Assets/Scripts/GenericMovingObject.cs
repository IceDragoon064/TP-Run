using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class GenericMovingObject : NetworkComponent
{

    public int GameStateVariable;

    public override void HandleMessage(string flag, string value)
    {
        throw new System.NotImplementedException();
    }

    public override IEnumerator SlowUpdate()
    {
        //Start with Network variables
        //
        //

        while(true)
        {
            if(IsLocalPlayer)
            {
                //User Input
            }
            if (IsClient)
            {
                //Affect seed by all clients
            }
            if(IsServer)
            {
                //Game states change
                //AI for enemies
            }

            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
    }

    //Set function pattern on server. Good for decreasing errors and finding them.
    public void SetGameState(int n)
    {
        if(IsServer)
        {
            GameStateVariable = n;
            //SendUpdate
        }
        else
        {
            //Throw error if a client calls this.
            throw new System.Exception("Client called a server Only Function");
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if(IsServer)
        {

        }
        if(IsClient)
        {

        }
        if(IsLocalPlayer)
        {

        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //Grabbing pointers to components
    }

    // Update is called once per frame
    void Update()
    {
        //Changing visual effect - Animations, etc.
        //FOR CLIENTS
    }
}
