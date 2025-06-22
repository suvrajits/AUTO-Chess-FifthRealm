using UnityEngine;
using Unity.Netcode.Components;

public class HeroAnimatorHandler : MonoBehaviour
{
    private Animator animator;
    private NetworkAnimator netAnimator;

    private static readonly int IsRunningHash = Animator.StringToHash("isRunning");
    private static readonly int IsAttackingHash = Animator.StringToHash("isAttacking");
    private static readonly int IsDeadHash = Animator.StringToHash("isDead");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        netAnimator = GetComponent<NetworkAnimator>();
    }

    public void SetRunning(bool isRunning)
    {
        animator?.SetBool(IsRunningHash, isRunning);
    }

    public void TriggerAttack()
    {
        netAnimator?.SetTrigger(IsAttackingHash);
    }

    public void TriggerDeath()
    {
        netAnimator?.SetTrigger(IsDeadHash);
    }
    public void PlayIdle()
    {
        animator.SetBool("isRunning", false);
        animator.ResetTrigger("isAttacking");
        animator.ResetTrigger("isDead");
    }

}
