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
        animator.SetTrigger("isAttacking");
    }

    public void TriggerDeath()
    {
        animator.SetTrigger("isDead");
    }
}
