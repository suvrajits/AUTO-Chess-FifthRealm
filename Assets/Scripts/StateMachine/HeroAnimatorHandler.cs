using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkAnimator))]
public class HeroAnimatorHandler : NetworkBehaviour
{
    private Animator animator;
    private NetworkAnimator netAnimator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        netAnimator = GetComponent<NetworkAnimator>();
    }

    public void SetRunning(bool isRunning)
    {
        animator?.SetBool("isRunning", isRunning);
    }

    public void TriggerAttack()
    {
        if (netAnimator != null && IsServer)
            netAnimator.SetTrigger("isAttacking");
    }

    public void TriggerDeath()
    {
        if (netAnimator != null && IsServer)
            netAnimator.SetTrigger("isDead");
    }
}
