using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class BulletBehavior : NetworkComponent
{
    public Vector3 direction;
    public Rigidbody myRig;
    public float speedBullet = 7f;

    public override void HandleMessage(string flag, string value)
    {

    }

    public override IEnumerator SlowUpdate()
    {
        myRig = this.GetComponent<Rigidbody>();
        StartCoroutine(DeathTimer(5f));

        while (true)
        {
            if(IsServer)
            {
                myRig.velocity = new Vector3(direction.x, myRig.velocity.y + direction.y, direction.z).normalized * speedBullet;
            }

            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
    }

    public IEnumerator DeathTimer(float time)
    {
        yield return new WaitForSeconds(time);
        MyCore.NetDestroyObject(this.NetId);
    }
    
    public void OnTriggerEnter(Collider other)
    {
        if(other.tag == "enemy")
        {
            BadGuyMovement bg = other.GetComponent<BadGuyMovement>();
            Debug.Log("Hit enemy with bullet");
            bg.health--;
            StartCoroutine(DeathTimer(0.05f));
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
