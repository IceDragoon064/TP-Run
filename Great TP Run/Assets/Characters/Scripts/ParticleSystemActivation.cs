using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class ParticleSystemActivation : NetworkComponent
{
    public ParticleSystem healthSystem;
    public ParticleSystem infectedSystem;

    public GameCharacter myChar;

    public override void HandleMessage(string flag, string value)
    {

    }

    public override IEnumerator SlowUpdate()
    {
        healthSystem.Stop();
        infectedSystem.Stop();

        if(IsClient)
        {
            StartCoroutine(StartHealthParticles());
            StartCoroutine(StartInfectedParticles());
        }

        yield return new WaitForSeconds(MyCore.MasterTimer);
    }
    
    public IEnumerator StartHealthParticles()
    {
        yield return new WaitUntil(() => myChar.healing); 

        healthSystem.Play();

        yield return new WaitUntil(() => !myChar.healing);

        healthSystem.Stop();

        StartCoroutine(StartHealthParticles());
    }

    public IEnumerator StartInfectedParticles()
    {
        yield return new WaitUntil(() => myChar.isInfected && myChar.health < 70);

        infectedSystem.Play();

        yield return new WaitUntil(() => !myChar.isInfected);

        infectedSystem.Stop();

        StartCoroutine(StartInfectedParticles());
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
