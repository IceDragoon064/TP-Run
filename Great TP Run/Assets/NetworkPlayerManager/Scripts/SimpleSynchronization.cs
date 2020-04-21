using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class SimpleSynchronization : NetworkComponent
{
    //Synchronized variables
    public int score = 0;
    public int count = 0;
    public int jumpCount = 0;
    public string pname = "";
    public bool CanJump = true;

    //Not synchronized variables
    public float jumpcooldown = 5;
    public float jumptimer = 0;

    public override void HandleMessage(string flag, string value)
    {
        if(flag == "SCORE")
        {
            if(IsClient)
            {
                score = int.Parse(value);
            }
            
        }

        if(flag == "CRT")
        {
            if(IsServer)
            {
                Vector3 ipos = new Vector3(Random.Range(-9, 9), -3, Random.Range(-5, 5));
                MyCore.NetCreateObject(0, int.Parse(value), ipos);
            }
        }

        if(flag == "CJ")
        {
            CanJump = bool.Parse(value);
        }

        if(flag == "JUMP")
        {
            if(IsServer && CanJump)
            {
                Debug.Log("Player " + int.Parse(value) + " jumped");
                jumpCount++;
                CanJump = false;
                SendUpdate("CJ", false.ToString());
                SendUpdate("JUMP", jumpCount.ToString());
                //Set timer.
                StartCoroutine(WaitForJump());
                //Server sends an update to clients with the value of jump count.
                //We are using the same flag so the handle message function doesn't become too complicated. We use 1 if condition instead of 2
            }

            if(IsClient)
            {
                jumpCount = int.Parse(value);
            }
        }

        if(flag == "PN")
        {
            //We are not changing all of the pnames of each client running the simple synchronization script. Only the object that sends a command receives an update.
            pname = value;
            if (IsServer)
            {
                SendUpdate("PN", value);
            }
        }
    }

    public IEnumerator WaitForJump()
    {
        yield return new WaitForSeconds(jumpcooldown);
        CanJump = true;
        SendUpdate("CJ", true.ToString());
    }

    //Every Single object will be running the slow update
    //Once we call this, we know all variables are in place. IsServer or IsClient, IsLocalPlayer, ID
    //This is called from the network component start function.
    public override IEnumerator SlowUpdate()
    {
        //Initialize your class, what we do in the start function just put it before the while loop. If we put it in start we don't know if it will actually do it.
        //Intializing network information goes here.
        //Network Start code would go here.
        if(IsLocalPlayer)
        {
            SendCommand("CRT", this.NetId.ToString());
        }

        while(true)
        {
            //Game logic loop


            //Executes on all of the clients
            if(IsClient)
            {
                
            }

            //Executes if you own the object
            if (IsLocalPlayer)
            {
                if(Input.GetAxisRaw("Jump") > 0 && CanJump)
                {
                    SendCommand("JUMP", MyId.NetId.ToString());
                    jumptimer = jumpcooldown;
                }

                //We are manually updating the timer so we can use it in UI. If there is no UI involved we can just make it a coroutiner.
                if(jumptimer > 0)
                {
                    jumptimer -= MyCore.MasterTimer;
                } 
            }

            if (IsServer)
            {
                //AI goes here
                count++;
                if(count%10 == 0)
                {
                    //increase the score
                    setScore(score += 1);
                }
                if(IsDirty)
                {
                    SendUpdate("SCORE", score.ToString());
                    SendUpdate("JUMP", jumpCount.ToString());
                }
            }

            yield return new WaitForSeconds(MyCore.MasterTimer);   //If we want the game to go faster, or how frequently the game updates, change this value. This is initially set to 20 Hertz. 
        }
    }

    public void SetPlayerName(string n)
    {
        //Player name
        SendCommand("PN", n);
    }

    public void setScore(int s)
    {
        if(IsServer)
        {
            score = s;
            SendUpdate("SCORE", score.ToString());
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
