using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;
using UnityEngine.UI;

public class GameCharacter : NetworkComponent
{
    //Player info
    public string Pname;
    public int score;
    public int color;

    //Player Stats
    public float maxHealth = 100;
    public float health = 100;
    public float healingRate = 10;
    public float infectedDamageRate = 10;

    public Text MyTextbox;
    public GameManagingScript manager;
    public Rigidbody myRig;
    public Camera cam;

    //Game state
    public bool ready = false;
    public bool end = false;

    public bool CanShoot = true;
    public float shootcooldown = .5f;
    public float shoottimer = 0;

    //Healing timer
    public float healingCD = 1f;
    public float healingReset = 1f;

    //Infected timer
    public float infectedCD = 0.6f;
    public float infectedReset = 2f;

    //Speed buff timer
    public float speedTime = float.PositiveInfinity;

    //movement
    public float turnRate = 3.5f;
    public float velRate = 4.0f;
    private float normalSpeed = 4.0f;

    //Status bool variables
    public bool spedUp = false;
    public bool isInfected = false;
    public bool healing = false;
    public bool dead = false;

    //Testing variables
    public float lerpVal = .25f;
    public float lerpMag = .5f;

    public GameObject SpawnPoint;
    public GameObject HomePoint;

    public GameObject AttackBox;
    public ScoreAndUI Overlay;
    public Inventory inventory;

    public IEnumerator DisableAttack()
    {
        yield return new WaitForSeconds(.9f);
        AttackBox.SetActive(false);
        //Jump cooldown
    }

    public override void HandleMessage(string flag, string value)
    {
        if(flag == "PNAME")
        {
            Pname = value;
            MyTextbox.text = value;
            ready = true;
        }

        if(flag == "COLOR")
        {
            color = int.Parse(value);
            this.GetComponent<MeshRenderer>().material = manager.materialList[color];
        }
        
        char[] remove = { '(', ')' };

        //Testing Infection debuff
        if (flag == "SPEED")
        {
            velRate = float.Parse(value);
        }

        if (flag == "SCORE")
        {
            score = int.Parse(value);
        }

        if (flag == "CARRIED")
        {
            inventory.tpCarried = int.Parse(value);
        }

        if (flag == "HEALTH")
        {
            health = float.Parse(value);
        }

        if (flag == "INFECTION")
        {
            isInfected = bool.Parse(value);
            
            //Activates the InfectedPanel on local players if true, otherwise it disactivates it.
            if(IsLocalPlayer)
            {
                if(bool.Parse(value))
                {
                    Overlay.InfectedPanel.SetActive(true);
                }
                else
                {
                    Overlay.InfectedPanel.SetActive(false);
                }
            }
        }

        if(flag == "HASSPEED")
        {
            spedUp = bool.Parse(value);
        }

        if (flag == "HEALING")
        {
            healing = bool.Parse(value);
        }

        if( flag == "SHOOT")
        {
            if(IsServer && CanShoot)
            {
                CanShoot = false;
                SendUpdate("CS", false.ToString());
                string[] data = value.Split(':');                   //Separates into data[0], data[1], data[2] - data[0] is the owner of bullet.
                string[] data2 = data[1].Trim(remove).Split(',');   //data[1] - position of bullet, 
                string[] data3 = data[2].Trim(remove).Split(',');   //data[2] - direction of bullet.
                Debug.Log(value);
                Vector3 pos = new Vector3(
                                                     float.Parse(data2[0]),
                                                     float.Parse(data2[1]),
                                                     float.Parse(data2[2])
                                                     );
                Vector3 dir = new Vector3(
                                                     float.Parse(data3[0]),
                                                     float.Parse(data3[1]),
                                                     float.Parse(data3[2])
                                                     );
                Debug.Log(pos);
                GameObject bullet = MyCore.NetCreateObject(6, int.Parse(data[0]), pos);
                BulletBehavior bb = bullet.GetComponent<BulletBehavior>();
                bb.direction = dir;
                StartCoroutine(WaitForShoot());
            }
        }

        if (flag == "CS")
        {
            CanShoot = bool.Parse(value);
        }

        if (flag == "DEAD")
        {
            dead = bool.Parse(value);
        }
    }

    public override IEnumerator SlowUpdate()
    {
        //At this point I know everything is initialized and ready to go.
        manager = FindObjectOfType<GameManagingScript>();
        myRig = this.GetComponent<Rigidbody>();

        if(IsLocalPlayer)
        {
            cam = FindObjectOfType<Camera>();

            cam.transform.position = transform.position + new Vector3(0, 15f, 0);
            cam.transform.LookAt(transform);
        }

        if(IsServer)
        {
            SpawnPoint = manager.spawnObjects[Owner % 4];
        }
        
        while(true)
        {
            if(!end)
            {
                if (IsServer)
                {
                    //Player dies and respawns at their home. Player loses TP carried. Cures Infection.
                    if (health <= 0 && !dead)
                    {
                        dead = true;
                        SendUpdate("DEAD", true.ToString());
                        StartCoroutine(PlayerDeath());
                    }

                    if(spedUp)
                    {
                        if(Time.time >= speedTime)
                        {
                            speedTime = float.PositiveInfinity;
                            NormalSpeed();
                        }
                    }
                }

                if (IsLocalPlayer)
                {
                    if (((cam.transform.position - transform.position) + new Vector3(0,12,0)).magnitude < 21)
                    {
                        //lerp
                        cam.transform.position = Vector3.Lerp(cam.transform.position, transform.position + new Vector3(0, 12, 0), lerpVal);
                    }
                    else
                    {
                        cam.transform.position = transform.position + new Vector3(0,12,0);
                    }

                    if(Input.GetAxisRaw("Jump") > 0 && CanShoot)
                    {
                        SendCommand("SHOOT", MyId.NetId.ToString() + ":" + (transform.position + transform.forward).ToString() + ":" + transform.forward.ToString());
                        SendCommand("ATTACK", "1");
                        shoottimer = shootcooldown;
                    }

                    if(shoottimer > 0)
                    {
                        shoottimer -= MyCore.MasterTimer;
                    }
                }
            }

            if(manager.GameEnded)
            {
                end = true;
            }

            if(IsServer)
            {
                if (IsDirty)
                {
                    SendUpdate("PNAME", Pname);
                    SendUpdate("COLOR", color.ToString());
                    IsDirty = false;
                }
            }
            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
    }

    public IEnumerator PlayerDeath()
    {
        if(IsServer)
        {
            yield return new WaitForSeconds(3);
            dead = false;
            SendUpdate("DEAD", false.ToString());
            SetHealth(maxHealth);
            GameObject[] spawnObjects = manager.spawnObjects;
            this.gameObject.GetComponent<Rigidbody>().position = SpawnPoint.transform.position;
            SetScore(score - 1);
            CureInfection();
            SetTPCarried(0);
            speedTime = float.PositiveInfinity;
            spedUp = false;
            SendUpdate("HASSPEED", spedUp.ToString());
            SetSpeed(normalSpeed);
        }
    }

    //Timer for infected status effect. Lowers health every few seconds.
    public IEnumerator InfectedStatus()
    {
        if (IsServer)
        {
            while (isInfected)
            {
                yield return new WaitForSeconds(infectedReset);
                if (isInfected)
                {
                    SetHealth(health - infectedDamageRate);
                }
            }
        }
    }

    //Timer for healing over time effect. Increases health every few seconds if player has the healing effect.
    public IEnumerator HealingOverTime()
    {
        if (IsServer)
        {
            while (healing)
            {
                yield return new WaitForSeconds(healingReset);
                if (health < maxHealth && healing)
                {
                    SetHealth(health + healingRate);
                }
            }
        }
    }

    //Sets score of player. Updates score for all clients to see.
    public void SetScore(int s)
    {
        if(IsServer)
        {
            score = s;
            SendUpdate("SCORE", score.ToString());
        }
    }

    //Set TP carried of player character. Updates client TP carried.
    public void SetTPCarried(int c)
    {
        if(IsServer)
        {
            inventory.tpCarried = c;
            SendUpdate("CARRIED", inventory.tpCarried.ToString());
        }
    }

    //Set health of player character. Updates client health.
    public void SetHealth(float value)
    {
        if(IsServer)
        {
            health = value;
            if(health > maxHealth)
            {
                health = maxHealth;
            }
            SendUpdate("HEALTH", health.ToString());
        }
    }

    //Set health of player character. Updates client speed.
    public void SetSpeed(float value)
    {
        if(IsServer)
        {
            velRate = value;
            SendUpdate("SPEED", velRate.ToString());
        }
    }

    //Infects the player that calls this function. Does not infect if they have the healing status.
    public void Infect()
    {
        if(IsServer)
        {
            if (!healing)
            {
                isInfected = true;
                SendUpdate("INFECTION", true.ToString());
                SetSpeed(velRate - 1.0f);
                StartCoroutine(InfectedStatus());
            }
        }
    }

    //Cures the infection of the player that calls this function.
    public void CureInfection()
    {
        if (IsServer)
        {
            isInfected = false;
            infectedCD = infectedReset;
            SendUpdate("INFECTION", false.ToString());
            SetSpeed(velRate + 1.0f);
        }
    }

    public void IncreaseSpeed()
    {
        if(IsServer)
        {
            if(!spedUp)
            {
                SetSpeed(velRate + 2.5f);
                SpeedCountdown();
                spedUp = true;
                SendUpdate("HASSPEED", true.ToString());
            }
            else
            {
                SpeedCountdown();
            }
        }
    }

    public void NormalSpeed()
    {
        if(IsServer)
        {
            SetSpeed(velRate - 2.5f);
            spedUp = false;
            SendUpdate("HASSPEED", false.ToString());
        }
    }

    public IEnumerator WaitForShoot()
    {
        yield return new WaitForSeconds(shootcooldown);
        CanShoot = true;
        SendUpdate("CS", true.ToString());
    }

    public void SpeedCountdown()
    {
        //yield return new WaitForSeconds(10.0f);
        speedTime = Time.time + 10f;
        /*
        spedUp = false;
        NormalSpeed();
        */
    }

    //Collisions

    //Triggers
    private void OnTriggerEnter(Collider other)
    {
        if (IsServer)
        {
            if (other.CompareTag("enemy"))
            {
                SetHealth(0);
            }

            if (other.CompareTag("TP"))
            {
                if (inventory.tpCarried < inventory.maxCarried)
                {
                    SetTPCarried(inventory.tpCarried + 1);
                    MyCore.NetDestroyObject(other.GetComponent<NetworkID>().NetId);
                }
            }

            if(other.CompareTag("GoldenTP"))
            {
                SetTPCarried(inventory.tpCarried + 3);
                MyCore.NetDestroyObject(other.GetComponent<NetworkID>().NetId);
            }

            if (other.CompareTag("House"))
            {
                if (inventory.tpCarried > 0)
                {
                    SetScore(score + inventory.tpCarried);
                    manager.playerTurnedIn(inventory.tpCarried);
                    SetTPCarried(0);
                }
            }

            if (other.CompareTag("HealingPad"))
            {
                healing = true;
                if(isInfected)
                {
                    CureInfection();
                }
                StartCoroutine(HealingOverTime());
                SendUpdate("HEALING", true.ToString());
            }

            if (other.CompareTag("radius"))
            {
                //Making sure the player isn't already infected and that the radius hit is not your own.
                GameObject colObj = other.transform.parent.gameObject;
                if(!isInfected && colObj != this.transform.gameObject)
                {
                    Debug.Log("TOO CLOSE, YOU'RE INFECTED NOW");

                    //See if they are an enemy or player
                    if(colObj.CompareTag("enemy"))
                    {
                        Debug.Log("Enemy infected you"); 
                        Infect();
                    }
                    if(colObj.CompareTag("Player"))
                    {
                        if(colObj.GetComponent<GameCharacter>().isInfected)
                        {
                            Debug.Log("Player infected you");
                            Infect();
                        }
                    }
                }   
            }

            if (other.CompareTag("SpeedDrink"))
            {
                if(spedUp == false)
                {
                    MyCore.NetDestroyObject(other.GetComponent<NetworkID>().NetId);
                    IncreaseSpeed();
                }
            }

            if(other.CompareTag("Medicine"))
            {
                if(isInfected == true)
                {
                    CureInfection();
                }
            }
        }

        if(IsClient)
        {
            if(other.CompareTag("Finish"))
            {
                //play sound effect
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(IsServer)
        {
            if(other.CompareTag("HealingPad"))
            {
                healing = false;
                healingCD = healingReset;
                SendUpdate("HEALING", false.ToString());
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
