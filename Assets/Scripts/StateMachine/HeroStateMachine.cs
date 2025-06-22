using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.Netcode.Components;

public class HeroStateMachine : NetworkBehaviour
{
    private HeroUnit hero;
    private Animator animator;
    private NetworkAnimator netAnimator;

    private HeroUnit targetEnemy;
    private Coroutine attackRoutine;

    public enum HeroState { Idle, Moving, Attacking, Dead }
    private HeroState currentState = HeroState.Idle;

    [SerializeField] private float corpseSinkY = 0.5f;
    [SerializeField] private float vanishDelay = 2f;
    private bool hasEnteredCombat = false;

    private void Awake()
    {
        hero = GetComponent<HeroUnit>();
        animator = GetComponentInChildren<Animator>();
        netAnimator = GetComponent<NetworkAnimator>();
    }

    public void EnterCombat()
    {
        if (!IsServer || !hero.IsAlive || hasEnteredCombat) return;
        hasEnteredCombat = true;

        Debug.Log($"[FSM] Entering combat for {hero.heroData.heroName}");

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        StartCoroutine(CombatLoop());
    }

    private IEnumerator CombatLoop()
    {
        Debug.Log($"[FSM] Combat loop started for {hero.heroData.heroName}");

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

        Debug.Log($"[FSM] Combat loop ended for {hero.heroData.heroName}");
    }

    private void FindTarget()
    {
        targetEnemy = BattleManager.Instance.FindNearestEnemy(hero);

        if (targetEnemy == null || !targetEnemy.IsAlive)
        {
            animator.SetBool("isRunning", false);
            currentState = HeroState.Idle;
            return;
        }

        float distance = Vector3.Distance(transform.position, targetEnemy.transform.position);
        currentState = distance <= hero.heroData.attackRange ? HeroState.Attacking : HeroState.Moving;
    }

    private void MoveTowardTarget()
    {
        if (targetEnemy == null || !targetEnemy.IsAlive)
        {
            currentState = HeroState.Idle;
            animator.SetBool("isRunning", false);
            return;
        }

        float distance = Vector3.Distance(transform.position, targetEnemy.transform.position);
        if (distance <= hero.heroData.attackRange)
        {
            currentState = HeroState.Attacking;
            animator.SetBool("isRunning", false);
            return;
        }

        Vector3 direction = (targetEnemy.transform.position - transform.position).normalized;
        transform.position += direction * hero.heroData.moveSpeed * Time.deltaTime;
        transform.LookAt(new Vector3(targetEnemy.transform.position.x, transform.position.y, targetEnemy.transform.position.z));

        animator.SetBool("isRunning", true);
    }

    private void HandleAttack()
    {
        if (attackRoutine == null)
        {
            attackRoutine = StartCoroutine(AttackCoroutine());
        }
    }

    private IEnumerator AttackCoroutine()
    {
        while (targetEnemy != null && targetEnemy.IsAlive && hero.IsAlive)
        {
            transform.LookAt(new Vector3(targetEnemy.transform.position.x, transform.position.y, targetEnemy.transform.position.z));

            if (netAnimator != null)
                netAnimator.SetTrigger("isAttacking");

            yield return new WaitForSeconds(hero.heroData.attackDelay);

            if (targetEnemy != null && targetEnemy.IsAlive)
            {
                targetEnemy.ApplyDamage(hero.heroData.attackDamage);
            }

            yield return new WaitForSeconds(1f / hero.heroData.attackSpeed);
        }

        attackRoutine = null;
        currentState = HeroState.Idle;
    }

    public void Die()
    {
        if (currentState == HeroState.Dead) return;
        currentState = HeroState.Dead;

        Debug.Log($"💀 {hero.heroData.heroName} died at {transform.position}");

        animator.SetBool("isRunning", false);
        if (netAnimator != null)
            netAnimator.SetTrigger("isDead");

        StopAllCoroutines();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.None;
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        StartCoroutine(FreezeCorpseAfterFall());
    }

    private IEnumerator FreezeCorpseAfterFall()
    {
        yield return new WaitForSeconds(1f);

        Vector3 pos = transform.position;
        pos.y -= corpseSinkY;
        transform.position = pos;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.isKinematic = true;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        yield return new WaitForSeconds(vanishDelay);

        if (IsServer)
        {
            if (GridManager.Instance.TryGetTile(NetworkObject.OwnerClientId, hero.GridPosition, out var tile))
            {
                tile.RemoveUnit();
            }

            BattleManager.Instance.UnregisterUnit(hero);

            var netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn();
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
