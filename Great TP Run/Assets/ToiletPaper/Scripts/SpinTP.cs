using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class SpinTP : NetworkComponent
{
    //Original y position of the TP
    public float originalY;
    //floatation speed
    public float floatationSpeed;
    //Speed that the TP spins
    public float rotationSpeed;
    
    public override void HandleMessage(string flag, string value)
    {

    }
    
    public override IEnumerator SlowUpdate()
    {
        originalY = transform.position.y;
        rotationSpeed = 0.0f;
        
        while (true)
        {
            if(IsServer)
            {
                //oscillation
                transform.position = new Vector3(transform.position.x, (originalY + Mathf.Sin(floatationSpeed * 0.1f) * 0.2f), transform.position.z);
                //rotation
                transform.Rotate(0,1,0);
                floatationSpeed++;
            }

            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
    }
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
