using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;
using UnityEngine.UI;

public class NetworkPlayerOption : NetworkComponent
{
    public string Pname = "Player";
    public int color = 0;
    public int shape = 0;
    public bool ready = false;

    public Canvas apCanvas;
    public Text pnText;
    public Dropdown shapeDrop;
    public Dropdown colorDrop;

    public override void HandleMessage(string flag, string value)
    {

        if(flag == "PNAME")
        {
            Pname = value;
            if(IsServer)
            {
                SendCommand("PNAME", value);
            }
        }

        if (flag == "COLOR")
        {
            color = int.Parse(value);
            if (IsServer)
            {
                SendUpdate("COLOR", color.ToString());
            }
        }
        if (flag == "SHAPE")
        {
            shape = int.Parse(value);
            if (IsServer)
            {
                SendUpdate("SHAPE", shape.ToString());
            }
        }
        if (flag == "READY")
        {
            ready = bool.Parse(value);
            if (IsServer)
            {
                SendUpdate("READY", ready.ToString());
            }
        }
    }

    public override IEnumerator SlowUpdate()
    {
        //Intialize things
        apCanvas.enabled = false;

        if (IsLocalPlayer)
        {
            apCanvas.enabled = true;
        }

        while (true)
        {
            if (IsServer)
            {
                if (IsDirty)
                {
                    SendUpdate("COLOR", color.ToString());
                    SendUpdate("SHAPE", shape.ToString());
                    SendUpdate("PNAME", Pname);
                    SendUpdate("READY", ready.ToString());
                    IsDirty = false;
                }
            }
            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
    }

    public void SetPlayerName()
    {
        if(IsLocalPlayer)
        {
            SendCommand("PNAME", pnText.text);
        }
    }

    public void SetPlayerShape()
    {
        if(IsLocalPlayer)
        {
            SendCommand("SHAPE", shapeDrop.value.ToString());
        }
    }

    public void SetPlayerColor()
    {
        if (IsLocalPlayer)
        {
            SendCommand("COLOR", colorDrop.value.ToString());
        }
    }

    public void SetPlayerReady()
    {
        if (IsLocalPlayer)
        {
            SendCommand("READY", true.ToString());
        }
    }
}
