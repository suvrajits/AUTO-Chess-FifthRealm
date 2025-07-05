using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class HeroStateMachine : NetworkBehaviour
{
    private HeroUnit hero;
    private HeroAnimatorHandler animHandler;

    [SerializeField] private float corpseSinkY = 0.5f;
    [SerializeField] private float vanishDelay = 2f;

    private void Awake()
    {
        hero = GetComponent<HeroUnit>();
        animHandler = GetComponent<HeroAnimatorHandler>();
    }

    public void Die()
    {
        Debug.Log("hero died from hero state machine");
        
        
        animHandler.SetRunning(false);
        animHandler.TriggerDeath();

        StopAllCoroutines();
    }



    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
