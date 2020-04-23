using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;
using UnityEngine.UI;

public class GameCharacter : NetworkComponent
{
    //This script goes on the player character created.
    public string Pname;
    public int score;
    public int color;
    public float health = 100;

    public Text MyTextbox;
    public GameManagingScript manager;
    public Rigidbody myRig;
    public Camera cam;

    public bool ready = false;
    public bool end = false;

    public bool CanShoot = true;
    public float shootcooldown = .5f;
    public float shoottimer = 0;

    public float turnRate = 3.5f;
    public float velRate = 4.0f;
    private float normalSpeed = 4.0f;


    //Status bool variables
    public bool spedUp = false;
    public bool isInfected = false;

    //Testing variables
    public float lerpVal = .25f;
    public float lerpMag = .5f;

    public GameObject AttackBox;
    public Inventory inventory;

    public IEnumerator DisableAttack()
    {
        yield return new WaitForSeconds(.9f);
        AttackBox.SetActive(false);
        //Jump cooldown
    }

    public override void HandleMessage(string flag, string value)
    {
        //can also set color the same way
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
        if (flag == "VELO")
        {
            if (IsServer)
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

        //Don't name the flag the same as the network rigid body
        if (flag == "ROTATES")
        {
            if (IsServer)
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

        if (flag == "SCORE")
        {
            score = int.Parse(value);
        }

        if (flag == "CARRIED")
        {
            inventory.tpCarried = int.Parse(value);
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

        if( flag == "CS")
        {
            CanShoot = bool.Parse(value);
        }
    }

    public override IEnumerator SlowUpdate()
    {
        //At this point I know everything is initialized and ready to go.
        //Poll and get our data.
        manager = FindObjectOfType<GameManagingScript>();
        myRig = this.GetComponent<Rigidbody>();

        if (IsServer)
        {
            /*
            NetworkPlayerOption[] AllPlayers = GameObject.FindObjectsOfType<NetworkPlayerOption>();

            for (int i = 0; i < AllPlayers.Length; i++)
            {
                //if the network player option owner is the same as this game character owner.
                if (AllPlayers[i].Owner == Owner)   
                {
                    Pname = AllPlayers[i].Pname;
                    SendUpdate("PNAME", Pname);
                }
            }*/
        }

        if(IsLocalPlayer)
        {
            cam = FindObjectOfType<Camera>();

            cam.transform.position = transform.position + new Vector3(0, 15f, 0);
            cam.transform.LookAt(transform);
        }
        
        while(true)
        {
            if(!end)
            {
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

                    if (Input.GetAxisRaw("Vertical") > 0.08 || Input.GetAxisRaw("Vertical") < -0.08)
                    {
                        float forward = Input.GetAxisRaw("Vertical");
                        Vector3 vel = new Vector3(0, myRig.velocity.y, 0) +
                            this.transform.forward * forward * velRate;
                        SendCommand("VELO", vel.ToString());
                    }
                    if (Input.GetAxisRaw("Horizontal") > 0.08 || Input.GetAxisRaw("Horizontal") < -0.08)
                    {
                        float turn = Input.GetAxisRaw("Horizontal");
                        Vector3 rotates = new Vector3(0, turn * turnRate, 0);

                        SendCommand("ROTATES", rotates.ToString());
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

    public void SetScore(int s)
    {
        if(IsServer)
        {
            score = s;
            SendUpdate("SCORE", score.ToString());
        }
    }

    //Server will increase the TP carried in the inventory to the value that's passed in and send an update to clients
    public void SetTPCarried(int c)
    {
        if(IsServer)
        {
            inventory.tpCarried = c;
            SendUpdate("CARRIED", inventory.tpCarried.ToString());
        }
    }

    public void IncreaseSpeed()
    {
        if(IsServer)
        {
            velRate = 5.5f;
            SendUpdate("SpeedUp", velRate.ToString());
        }
    }

    public IEnumerator WaitForShoot()
    {
        yield return new WaitForSeconds(shootcooldown);
        CanShoot = true;
        SendUpdate("CS", true.ToString());
    }


    

    //Collisions

    //Triggers
    private void OnTriggerEnter(Collider other)
    {
        if (IsServer)
        {
            if (other.tag == "enemy")
            {
                //send to start location.
                GameObject[] spawnObjects = GameObject.FindGameObjectsWithTag("Respawn");
                this.gameObject.GetComponent<Rigidbody>().position = spawnObjects[Owner % 4].transform.position;
                SetScore(score - 1);
            }
            //Change tag to toilet paper or something later on
            if (other.tag == "coin")
            {
                if (inventory.tpCarried < 2)
                {
                    SetTPCarried(inventory.tpCarried + 1);
                    MyCore.NetDestroyObject(other.GetComponent<NetworkID>().NetId);
                }
            }
            if (other.tag == "House")
            {
                if (inventory.tpCarried > 0)
                {
                    SetScore(score + inventory.tpCarried);
                    SetTPCarried(inventory.tpCarried = 0);
                }
            }

            if (other.tag == "SpeedDrink")
            {
                if(spedUp == false)
                {

                }
            }

            if(other.tag == "Medicine")
            {
                if(isInfected == true)
                {

                }
            }


        }

        if(IsClient)
        {
            if(other.tag == "Finish")
            {
                //play sound effect
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
