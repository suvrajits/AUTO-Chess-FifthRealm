using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class HeroStateMachine : NetworkBehaviour
{
    private HeroUnit hero;
    private Animator animator;

    private HeroUnit targetEnemy;
    private float attackCooldown = 0f;

    public enum HeroState { Idle, Moving, Attacking, Dead }
    private HeroState currentState = HeroState.Idle;
    private Coroutine attackRoutine;
    private void Awake()
    {
        hero = GetComponent<HeroUnit>();
        animator = GetComponentInChildren<Animator>();
    }

    public void EnterCombat()
    {
        if (!IsServer || !hero.IsAlive) return;
        Debug.Log($"[FSM] Entering combat for {hero.heroData.heroName}");
        StartCoroutine(CombatLoop());
    }
    private void HandleAttack()
    {
        if (attackRoutine == null)
            attackRoutine = StartCoroutine(AttackCoroutine());
    }
    private IEnumerator AttackCoroutine()
    {
        while (targetEnemy != null && targetEnemy.IsAlive && hero.IsAlive)
        {
            // Rotate toward target
            Vector3 lookPos = targetEnemy.transform.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);

            animator.SetTrigger("isAttacking");

            // Wait for hit timing (matches animation)
            yield return new WaitForSeconds(hero.heroData.attackDelay);

            // Apply damage if still valid
            if (targetEnemy != null && targetEnemy.IsAlive)
            {
                targetEnemy.ApplyDamage(hero.heroData.attackDamage);
            }

            // Wait full cooldown before next attack
            yield return new WaitForSeconds(1f / hero.heroData.attackSpeed);
        }

        attackRoutine = null;
        currentState = HeroState.Idle;
    }


    private IEnumerator CombatLoop()
    {
        if (!IsServer || hero == null || !hero.IsAlive || BattleManager.Instance.CurrentPhase != GamePhase.Battle)
            yield break;
        Debug.Log($"[FSM] Starting combat loop for {hero.heroData.heroName}");

        while (hero.IsAlive && BattleManager.Instance.CurrentPhase == GamePhase.Battle)
        {
            switch (currentState)
            {
                case HeroState.Idle:
                    FindTarget();
                    break;

                case HeroState.Moving:
                    MoveTowardTarget();
                    break;

                case HeroState.Attacking:
                    HandleAttack();
                    break;
            }

            yield return null;
        }
    }

    private void FindTarget()
    {
        targetEnemy = BattleManager.Instance.FindNearestEnemy(hero);

        if (targetEnemy == null)
        {
            animator.SetBool("isRunning", false);
            return;
        }

        float distance = Vector3.Distance(transform.position, targetEnemy.transform.position);
        if (distance <= hero.heroData.attackRange)
        {
            currentState = HeroState.Attacking;
        }
        else
        {
            currentState = HeroState.Moving;
        }
    }

    private void MoveTowardTarget()
    {
        Debug.DrawLine(transform.position, targetEnemy.transform.position, Color.yellow);
        if (targetEnemy == null || !targetEnemy.IsAlive)
        {
            currentState = HeroState.Idle;
            return;
        }

        float distance = Vector3.Distance(transform.position, targetEnemy.transform.position);

        if (distance <= hero.heroData.attackRange)
        {
            animator.SetBool("isRunning", false);
            currentState = HeroState.Attacking;
            return;
        }

        // Basic forward movement toward target
        Vector3 dir = (targetEnemy.transform.position - transform.position).normalized;
        transform.position += dir * hero.heroData.moveSpeed * Time.deltaTime;
        transform.LookAt(targetEnemy.transform);

        animator.SetBool("isRunning", true);
    }

    

    public void Die()
    {
        currentState = HeroState.Dead;
        animator.SetTrigger("isDead");
        StopAllCoroutines();
        GetComponent<Collider>().enabled = false;
    }
}
