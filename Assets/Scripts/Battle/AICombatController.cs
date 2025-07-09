using UnityEngine;
using System.Collections;
using Unity.Netcode;

[RequireComponent(typeof(HeroUnit))]
public class AICombatController : NetworkBehaviour
{
    private HeroUnit unit;
    private HeroData data;

    private float cooldownTimer = 0f;
    private bool isInBattle = false;
    private TraitEffectHandler traitHandler;
    private void Awake()
    {
        unit = GetComponent<HeroUnit>();
        data = unit.heroData;

        if (data == null)
            Debug.LogError($"❌ AICombatController: Missing HeroData on {name}");

        unit = GetComponent<HeroUnit>();
        data = unit.heroData;
        traitHandler = GetComponent<TraitEffectHandler>();
    }
    void Update()
    {
        /*if (!NetworkManager.Singleton.IsServer) return;
        TickAI();*/
    }


    public void SetBattleMode(bool enabled)
    {
        isInBattle = enabled;

        if (!enabled)
        {
            cooldownTimer = 0f;
            unit.AnimatorHandler?.SetRunning(false);
            unit.AnimatorHandler?.PlayIdle();
        }
    }

    public void TickAI()
    {
        if (!isInBattle || !unit.IsAlive || BattleManager.Instance.IsBattleOver())
            return;

        unit.SnapToGroundedTile(); //  Keep centered + upright every frame

        HeroUnit target = FindClosestEnemy();
        if (target == null || !target.IsAlive) return;

        float distance = Vector3.Distance(unit.transform.position, target.transform.position);

        if (distance > data.attackRange)
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

        Vector3 lookDir = (target.transform.position - unit.transform.position);
        if (lookDir != Vector3.zero)
        {
            lookDir.y = 0;
            unit.transform.forward = lookDir.normalized;
        }

        unit.AnimatorHandler?.SetRunning(true);
    }

    private void TryAttack(HeroUnit target)
    {
        if (!unit.IsAlive || BattleManager.Instance.IsBattleOver())
            return;

        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        Vector3 lookDir = (target.transform.position - unit.transform.position);
        if (lookDir != Vector3.zero)
        {
            lookDir.y = 0;
            unit.transform.forward = lookDir.normalized;
        }

        unit.AnimatorHandler?.SetRunning(false);
        unit.AnimatorHandler?.TriggerAttack();

        StartCoroutine(DelayedHit(target, data.attackDelay));
        cooldownTimer = data.attackSpeed;
    }

    private IEnumerator DelayedHit(HeroUnit target, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (target != null && unit != null && unit.IsAlive && target.IsAlive)
        {
            int damage = (int)unit.Attack;

            // 🗡️ Apply damage with attacker reference (for Raksha reflect)
            target.TakeDamage(damage, unit);

            // 🩸 Lifesteal Hook — heal based on damage dealt
            if (unit.HasLifesteal())
            {
                float healAmount = damage * unit.GetLifestealPercentage();
                unit.Heal(Mathf.RoundToInt(healAmount));
            }

            // ⚡ Trait effect handler — AoE shock, bleed, poison, etc.
            traitHandler?.OnAttack(target);
        }

        unit.AnimatorHandler?.SetRunning(false);
    }

}
