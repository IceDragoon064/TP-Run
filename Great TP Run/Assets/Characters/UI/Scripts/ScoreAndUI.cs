using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NETWORK_ENGINE;

public class ScoreAndUI : NetworkComponent
{
    public string Pname;

    public Text apText;
    public Text localScoreText;
    public Text playerScoreText;
    public Text pnameText;
    public Text tpRemainingText;
    public Text tpCarried;

    public GameObject InfectedPanel;

    public Slider hpBar;

    public Canvas apCanvas;

    public GameCharacter[] players;
    public GameCharacter myChar;
    public GameManagingScript manager;

    public override void HandleMessage(string flag, string value)
    {

    }

    public override IEnumerator SlowUpdate()
    {
        yield return new WaitUntil(() => myChar.ready);
        if(myChar.ready)
        {
            //UI stuff is happening locally to each player, score is the only synchronized value that the server sends.
            apCanvas.enabled = false;
            
            if (IsClient)
            {
                Pname = myChar.Pname;
            }

            if (IsLocalPlayer)
            {
                apCanvas.enabled = true;
                InfectedPanel.SetActive(false);
                manager = myChar.manager;
            }

            while (true)
            {
                if (IsLocalPlayer)
                {
                    apText.text = "";
                    playerScoreText.text = "";
                    localScoreText.text = myChar.score.ToString();
                    pnameText.text = Pname;
                    players = FindObjectsOfType<GameCharacter>();
                    tpRemainingText.text = "TP Left" + "\n" + (manager.maxTurnIn - manager.turnedIn).ToString();
                    tpCarried.text = "TP Carried: " + myChar.inventory.tpCarried.ToString();
                    if (myChar.inventory.tpCarried >= myChar.inventory.maxCarried)
                    {
                        tpCarried.text += " (max)";
                    } 
                    hpBar.value = myChar.health;
                    foreach (GameCharacter player in players)
                    {
                        apText.text += player.Pname + "\n";
                        playerScoreText.text += player.score + "\n";
                    }
                }

                yield return new WaitForSeconds(MyCore.MasterTimer);
            }
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
