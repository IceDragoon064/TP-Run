using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;
public class GameManagerTemplate : NetworkComponent
{
    public override void HandleMessage(string flag, string value)
    {
        //if flag == "GAMESTART"
        //   Want to disable PlayerInfo
    }

    public override IEnumerator SlowUpdate()
    {
        if (IsServer)
        {
            //while(all players have not hit ready)
            //Wait

            //SendUpdate "GAMESTART"

            //Go to each NetworkPlayerManager and look at their options
            //Create the appropriate character for their options
            //GameObject temp = MyCore.NetCreateObject(1,Owner,new Vector3);
            //temp.GetComponent<MyCharacterScript>().team = //set the team;

            MyId.NotifyDirty();
        }
        yield return new WaitForSeconds(.1f);
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
