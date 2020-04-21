using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class HitBox : NetworkComponent
{
    public override void HandleMessage(string flag, string value)
    {
        throw new System.NotImplementedException();
    }

    public override IEnumerator SlowUpdate()
    {
        throw new System.NotImplementedException();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Finish")
        {

        }
    }
}
