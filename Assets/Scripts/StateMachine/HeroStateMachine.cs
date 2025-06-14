using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.Netcode.Components;

public class HeroStateMachine : NetworkBehaviour
{
    private HeroUnit hero;
    private Animator animator;

    private HeroUnit targetEnemy;
    private float attackCooldown = 0f;

    public enum HeroState { Idle, Moving, Attacking, Dead }
    private HeroState currentState = HeroState.Idle;
    private Coroutine attackRoutine;
    [SerializeField] private float corpseSinkY = 0.5f;
    [SerializeField] private float vanishDelay = 2f;
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

            GetComponent<NetworkAnimator>().SetTrigger("isAttacking");

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

        Debug.Log($" {hero.heroData.heroName} died at {transform.position}");

        var netAnim = GetComponent<NetworkAnimator>();
        if (netAnim != null)
        {
            Debug.Log(" Setting 'isDead' trigger on NetworkAnimator");
            netAnim.SetTrigger("isDead");
        }
        else
        {
            Debug.LogWarning(" No NetworkAnimator found on this GameObject!");
        }

        StopAllCoroutines();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;                //  Let gravity apply
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
        }

        animator.SetBool("isRunning", false);

        // Delay collider disable until corpse hits ground
        StartCoroutine(FreezeCorpseAfterFall());
    }
    private IEnumerator FreezeCorpseAfterFall()
    {
        yield return new WaitForSeconds(1f); // allow time for fall

        // Sink slightly for grounded look
        Vector3 pos = transform.position;
        pos.y -= corpseSinkY;
        transform.position = pos;

        // Freeze Rigidbody
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }

        // Disable Collider
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // Wait then despawn (on server only)
        yield return new WaitForSeconds(vanishDelay);

        if (IsServer)
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn(); //  This will vanish the object on all clients
            }
        }
    }

}
