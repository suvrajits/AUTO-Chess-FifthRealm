using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum BuffType
{
    Poison,
    Bleed,
    Shield,
    LifestealAura
}

public class BuffManager : MonoBehaviour
{
    private HeroUnit hero;

    private readonly Dictionary<BuffType, Coroutine> activeBuffs = new();

    private float currentShield = 0;

    void Awake()
    {
        hero = GetComponent<HeroUnit>();
    }

    // Core entry point
    public void ApplyBuff(BuffType type, float value, float duration, HeroUnit source = null)
    {
        if (activeBuffs.ContainsKey(type))
        {
            StopCoroutine(activeBuffs[type]);
            activeBuffs.Remove(type);
        }

        Coroutine buffRoutine = StartCoroutine(HandleBuff(type, value, duration, source));
        activeBuffs[type] = buffRoutine;
    }

    private IEnumerator HandleBuff(BuffType type, float value, float duration, HeroUnit source)
    {
        switch (type)
        {
            case BuffType.Poison:
                float poisonTickRate = 1f;
                float poisonElapsed = 0f;
                while (poisonElapsed < duration)
                {
                    hero.TakeDamage(Mathf.RoundToInt(value));
                    yield return new WaitForSeconds(poisonTickRate);
                    poisonElapsed += poisonTickRate;
                }
                break;

            case BuffType.Bleed:
                float bleedTickRate = 0.5f;
                float bleedElapsed = 0f;
                while (bleedElapsed < duration)
                {
                    hero.TakeDamage(Mathf.RoundToInt(value));
                    yield return new WaitForSeconds(bleedTickRate);
                    bleedElapsed += bleedTickRate;
                }
                break;

            case BuffType.Shield:
                currentShield += value;
                hero.SetShieldVisual(true);
                yield return new WaitForSeconds(duration);
                currentShield = 0;
                hero.SetShieldVisual(false);
                break;

            case BuffType.LifestealAura:
                hero.EnableLifesteal(value); // Assume this sets a flag in HeroUnit
                yield return new WaitForSeconds(duration);
                hero.DisableLifesteal();
                break;
        }

        activeBuffs.Remove(type);
    }

    // For damage taken: apply shield if active
    public float AbsorbDamage(float incoming)
    {
        if (currentShield > 0)
        {
            float absorbed = Mathf.Min(currentShield, incoming);
            currentShield -= absorbed;
            incoming -= absorbed;
        }

        return incoming;
    }
}
