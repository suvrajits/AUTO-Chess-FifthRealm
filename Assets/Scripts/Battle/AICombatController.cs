using UnityEngine;
using System.Collections;

[RequireComponent(typeof(HeroUnit))]
public class AICombatController : MonoBehaviour
{
    private HeroUnit unit;
    private HeroData data;

    private float cooldownTimer = 0f;
    private Coroutine attackRoutine;

    private void Awake()
    {
        unit = GetComponent<HeroUnit>();
        data = unit.heroData;

        if (data == null)
            Debug.LogError($"❌ AICombatController: Missing HeroData on {name}");
    }

    public void TickAI()
    {
        if (!unit.IsAlive || data == null) return;

        HeroUnit target = FindClosestEnemy();
        if (target == null || !target.IsAlive) return;

        float distance = Vector3.Distance(unit.transform.position, target.transform.position);

        if (distance > data.attackRange)
        {
            MoveToward(target);
        }
        else
        {
            if (attackRoutine == null)
                attackRoutine = StartCoroutine(AttackSequence(target));
        }
    }

    private HeroUnit FindClosestEnemy()
    {
        var enemies = BattleManager.Instance.GetAllAliveUnits()
            .FindAll(u => u.OwnerClientId != unit.OwnerClientId && u.IsAlive);

        if (enemies.Count == 0) return null;

        HeroUnit closest = null;
        float minDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(unit.transform.position, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = enemy;
            }
        }

        return closest;
    }

    private void MoveToward(HeroUnit target)
    {
        if (target == null) return;

        Vector3 dir = (target.transform.position - unit.transform.position).normalized;
        unit.transform.position += dir * unit.moveSpeed * Time.deltaTime;

        Vector3 lookDir = (target.transform.position - unit.transform.position);
        if (lookDir != Vector3.zero)
        {
            lookDir.y = 0;
            unit.transform.forward = lookDir.normalized;
        }

        unit.AnimatorHandler?.SetRunning(true);
    }

    private IEnumerator AttackSequence(HeroUnit target)
    {
        while (target != null && target.IsAlive && unit.IsAlive)
        {
            unit.AnimatorHandler?.SetRunning(false);
            FaceTarget(target);
            unit.AnimatorHandler?.TriggerAttack();

            yield return new WaitForSeconds(data.attackDelay);

            if (target != null && target.IsAlive)
            {
                target.TakeDamage((int)data.attackDamage);
            }

            yield return new WaitForSeconds(1f / data.attackSpeed);
        }

        attackRoutine = null;
    }

    private void FaceTarget(HeroUnit target)
    {
        if (target == null) return;

        Vector3 dir = (target.transform.position - unit.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            unit.transform.rotation = Quaternion.Slerp(unit.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
        }
    }
}
