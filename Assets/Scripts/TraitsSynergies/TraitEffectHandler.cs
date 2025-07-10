using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(HeroUnit))]
public class TraitEffectHandler : MonoBehaviour
{
    private HeroUnit unit;
    private List<TraitDefinition> traits = new();
    private bool hasUsedFirstStrike = false;

    private float vajraShockRadius = 2.5f;
    private float vajraShockDamageMultiplier = 0.5f;
    private float yogiTickInterval = 10f;
    private float yogiTimer = 0f;
    private bool isYogiActive = false;
    private int yogiStacks = 0;
    private int maxYogiStacks = 3;
    public TraitTracker traitTracker;
    public bool HasAgniTrait => HasTrait("Agni");
    public bool HasAgniSynergyTier2 => traitTracker?.GetSynergyTier("Agni") >= 2;
    public bool HasAgniSynergyTier4 => traitTracker?.GetSynergyTier("Agni") >= 4;
    public void Initialize(HeroUnit hero, List<TraitDefinition> traitList)
    {
        unit = hero;
        traits = traitList;
        hasUsedFirstStrike = false;
        traitTracker = unit.OwnerPlayerNetworkState?.GetComponent<TraitTracker>();
    }
    private void Update()
    {
        if (!isYogiActive || !unit.IsAlive || !unit.IsInCombat) return;

        yogiTimer += Time.deltaTime;

        if (yogiTimer >= yogiTickInterval && yogiStacks < maxYogiStacks)
        {
            yogiTimer = 0f;
            yogiStacks++;

            ApplyYogiBuff();
        }
        // 🔥 Agni Tier 2 — Monitor HP and grant attack speed bonus under 50%
        if (HasTrait("Agni") && traitTracker != null && traitTracker.IsSynergyActive("Agni"))
        {
            float hpPercent = unit.CurrentHealth / unit.heroData.maxHealth;
            if (hpPercent < 0.5f)
            {
                unit.ApplyAttackSpeedMultiplier(1.5f); // e.g., +50% speed
            }
            else
            {
                unit.ResetAttackSpeedMultiplier(); // Reset if above 50%
            }
        }
    }

    public void OnBattleStart()
    {
        hasUsedFirstStrike = false;
        yogiTimer = 0f;
        yogiStacks = 0;
        isYogiActive = HasTrait("Yogi");
        if (HasTrait("Mantra") && traitTracker.IsSynergyActive("Mantra"))
        {
            int tier = traitTracker.GetSynergyTier("Mantra"); // Returns 2 or 4
            float healPercent = (tier >= 4) ? 7.5f : 5f;
            float duration = 20f; // duration of battle phase

            unit.BuffManager?.ApplyBuff(BuffType.MantraAura, healPercent, duration);
        }
    }

    public void OnAttack(HeroUnit target)
    {
        if (unit == null || target == null || !unit.IsAlive || !target.IsAlive) return;

        foreach (var trait in traits)
        {
            if (trait.traitName == "Vajra" && !hasUsedFirstStrike)
            {
                TriggerVajraShockAoE(target.transform.position);
                hasUsedFirstStrike = true;
            }
            if (trait.traitName == "Vanara")
            {
                ApplyBleed(target);
            }
            // 🐍 Naga – Apply Poison Stack
            if (trait.traitName == "Naga")
            {
                var traitTracker = unit.OwnerPlayerNetworkState?.GetComponent<TraitTracker>();

                // Only apply poison if synergy is active
                if (traitTracker != null && traitTracker.IsSynergyActive("Naga"))
                {
                    ApplyPoison(target);
                }
                else
                {
                    // If synergy is broken, clean up poison from this target
                    target.BuffManager?.ClearAllPoison();
                }
            }

            if (trait.traitName == "Dhanava")
            {
                TryExecuteTarget(target);
            }
        }

    }

    private void TriggerVajraShockAoE(Vector3 center)
    {
        float baseDamage = unit.Attack;
        int shockDamage = Mathf.RoundToInt(baseDamage * vajraShockDamageMultiplier);

        Collider[] hits = Physics.OverlapSphere(center, vajraShockRadius);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out HeroUnit enemy))
            {
                if (enemy != unit && enemy.Faction != unit.Faction && enemy.IsAlive)
                {
                    enemy.TakeDamage(shockDamage);
                    Debug.Log($"⚡ Vajra shock hit {enemy.heroData.heroName} for {shockDamage} AoE damage!");
                }
            }
        }

        // Optional: Play shock VFX
        PlayShockVFX(center);
    }

    private void PlayShockVFX(Vector3 center)
    {
        // TODO: Instantiate a lightning explosion effect or camera shake here
        Debug.Log($"⚡ Shock AoE VFX triggered at {center}");
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, vajraShockRadius);
    }
    private void ApplyBleed(HeroUnit target)
    {
        if (target == null || !target.IsAlive) return;

        float damagePerTick = 5f;
        float duration = 3f;

        target.BuffManager?.ApplyBuff(BuffType.Bleed, damagePerTick, duration, unit);

        Debug.Log($"🐾 Vanara bleed applied to {target.heroData.heroName} for {duration}s");
    }
    public void OnDamaged(HeroUnit attacker, int damageTaken)
    {
        if (unit == null || attacker == null || !unit.IsAlive || !attacker.IsAlive) return;

        foreach (var trait in traits)
        {
            if (trait.traitName == "Raksha")
            {
                ReflectDamage(attacker, damageTaken);
            }
        }
        if (traitTracker != null && traitTracker.GetSynergyTier("Agni") >= 4)
        {
            TriggerAgniRetaliationAoE(unit.transform.position, damageTaken);
        }
    }
    private void TriggerAgniRetaliationAoE(Vector3 center, int damageTaken)
    {
        float aoeRadius = 2.5f;
        float aoeDamageMultiplier = 0.35f; // 35% of incoming damage

        int aoeDamage = Mathf.RoundToInt(damageTaken * aoeDamageMultiplier);

        Collider[] hits = Physics.OverlapSphere(center, aoeRadius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out HeroUnit other))
            {
                if (other != unit && other.Faction != unit.Faction && other.IsAlive)
                {
                    other.TakeDamage(aoeDamage, unit);
                    Debug.Log($"🔥 Agni retaliation hit {other.heroData.heroName} for {aoeDamage} AoE damage!");
                }
            }
        }

        // Optional: Add VFX or camera shake
    }

    private void TriggerAgniAoE(HeroUnit attacker)
    {
        float radius = 1.5f;
        int aoeDamage = Mathf.RoundToInt(unit.Attack * 0.3f);

        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out HeroUnit target) &&
                target != unit && target.Faction != unit.Faction && target.IsAlive)
            {
                target.TakeDamage(aoeDamage, unit);
            }
        }

        // Optional VFX
        Debug.Log($"🔥 Agni AoE triggered by {unit.heroData.heroName} for {aoeDamage} damage.");
    }

    private void ReflectDamage(HeroUnit attacker, int damageTaken)
    {
        float reflectPercentage = 0.25f; // Reflect 25%
        int reflected = Mathf.RoundToInt(damageTaken * reflectPercentage);

        if (reflected <= 0) return;

        attacker.TakeDamage(reflected);

        Debug.Log($"🛡 {unit.heroData.heroName} reflected {reflected} damage back to {attacker.heroData.heroName}");
    }
    private void ApplyPoison(HeroUnit target)
    {
        if (target == null || !target.IsAlive) return;

        float poisonDamagePerTick = 4f;
        float durationPerStack = 4f;

        if (traitTracker != null && traitTracker.IsSynergyActive("Naga"))
        {
            target.BuffManager?.ApplyBuff(BuffType.Poison, poisonDamagePerTick, durationPerStack, unit);
            Debug.Log($"🐍 Poison applied to {target.heroData.heroName} by {unit.heroData.heroName}");
        }
        else
        {
            Debug.Log($"❌ Naga synergy inactive. Poison not applied by {unit.heroData.heroName}");
        }
    }



    private void ApplyYogiBuff()
    {
        float atkBoost = unit.Attack * 0.10f;
        float hpBoost = unit.heroData.maxHealth * 0.10f;

        unit.AddBonusAttack(atkBoost);
        unit.AddBonusMaxHealth(hpBoost);

        Debug.Log($"🧘 Yogi buff applied to {unit.heroData.heroName}: +{atkBoost} ATK, +{hpBoost} HP");

        if (yogiStacks >= 1 && HasTraitCount("Yogi") >= 4)
        {
            unit.EnableLifesteal(0.25f);
            Debug.Log($"🧘 Yogi 4-unit bonus activated: Lifesteal enabled.");
        }
    }
    public bool HasTrait(string traitName)
    {
        return traits.Exists(t => t.traitName == traitName);
    }

    private int HasTraitCount(string traitName)
    {
        return traits.FindAll(t => t.traitName == traitName).Count;
    }
    private void TryExecuteTarget(HeroUnit target)
    {
        if (target == null || !target.IsAlive) return;

        float thresholdPercent = HasTraitCount("Dhanava") >= 4 ? 0.20f : 0.10f;
        float thresholdHP = target.heroData.maxHealth * thresholdPercent;

        if (target.CurrentHealth <= thresholdHP)
        {
            Debug.Log($"🎯 {unit.heroData.heroName} executes {target.heroData.heroName} (HP: {target.CurrentHealth})");

            target.TakeDamage((int)target.CurrentHealth + 999, unit); // Guaranteed overkill
        }
    }
    public void CheckAgniLowHealthBuff(float currentHP, float maxHP)
    {
        if (!HasAgniSynergyTier2) return;

        bool belowThreshold = currentHP <= maxHP * 0.5f;
        unit.SetAttackSpeedMultiplier(belowThreshold ? 1.25f : 1f); // Example: +25%
    }

}
