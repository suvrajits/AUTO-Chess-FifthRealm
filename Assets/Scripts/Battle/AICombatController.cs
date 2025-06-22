using UnityEngine;
using System.Collections;

public class AICombatController : MonoBehaviour
{
    public HeroUnit unit;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.0f;
    public int attackDamage = 10;

    private float cooldownTimer = 0f;

    private void Awake()
    {
        if (unit == null)
            unit = GetComponent<HeroUnit>();
    }

    public void TickAI()
    {
        if (!unit.IsAlive) return;

        HeroUnit target = FindClosestEnemy();

        if (target == null || !target.IsAlive)
            return;

        float distance = Vector3.Distance(unit.transform.position, target.transform.position);

        if (distance > attackRange)
        {
            MoveToward(target);
        }
        else
        {
            TryAttack(target);
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
        Vector3 dir = (target.transform.position - unit.transform.position).normalized;
        unit.transform.position += dir * unit.moveSpeed * Time.deltaTime;
    }

    private void TryAttack(HeroUnit target)
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        target.TakeDamage(attackDamage);
        cooldownTimer = attackCooldown;
    }
}
