using Unity.Netcode.Components;
using UnityEngine;

public class HeroAnimatorHandler : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void SetRunning(bool isRunning)
    {
        animator.SetBool("isRunning", isRunning);
    }

    public void TriggerAttack()
    {
        GetComponent<NetworkAnimator>().SetTrigger("isAttacking");
    }

    public void TriggerDeath()
    {
        GetComponent<NetworkAnimator>().SetTrigger("isDead");
    }
}
