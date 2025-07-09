using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public enum BuffType
{
    Poison,
    Bleed,
    Shield,
    LifestealAura
}

public class BuffManager : NetworkBehaviour
{
    private HeroUnit hero;
    private readonly Dictionary<BuffType, Coroutine> activeBuffs = new();

    private float currentShield = 0;

    // ✅ Poison stack tracking
    private Dictionary<HeroUnit, int> poisonStackCounts = new();
    private PoisonStackUI poisonUIInstance;

    // ✅ Prefab to assign from HeroUnit
    public GameObject poisonStackUIPrefab;
    private NetworkVariable<int> poisonStackCount = new NetworkVariable<int>(
    0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
);

    void Awake()
    {
        hero = GetComponent<HeroUnit>();
    }

    // 📍 Entry point to apply buff
    public void ApplyBuff(BuffType type, float value, float duration, HeroUnit source = null)
    {
        if (type != BuffType.Poison && activeBuffs.ContainsKey(type))
        {
            StopCoroutine(activeBuffs[type]);
            activeBuffs.Remove(type);
        }

        Coroutine buffRoutine = StartCoroutine(HandleBuff(type, value, duration, source));
        if (type != BuffType.Poison)
            activeBuffs[type] = buffRoutine;
    }

    private IEnumerator HandleBuff(BuffType type, float value, float duration, HeroUnit source)
    {
        switch (type)
        {
            case BuffType.Poison:
                yield return StartCoroutine(ApplyStackingPoison(hero, value, duration, source));
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
                hero.EnableLifesteal(value);
                yield return new WaitForSeconds(duration);
                hero.DisableLifesteal();
                break;
        }

        if (activeBuffs.ContainsKey(type))
            activeBuffs.Remove(type);
    }

    private IEnumerator ApplyStackingPoison(HeroUnit hero, float damagePerTick, float duration, HeroUnit sourceUnit)
    {
        if (sourceUnit == null || hero == null) yield break;

        if (!poisonStackCounts.ContainsKey(sourceUnit))
            poisonStackCounts[sourceUnit] = 0;
        poisonStackCounts[sourceUnit]++;

        UpdatePoisonUI();

        float tickRate = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 🛡 Safety check
            if (hero == null || !hero.IsAlive || BattleManager.Instance.CurrentPhase != GamePhase.Battle)
                yield break;

            hero.TakeDamage(Mathf.RoundToInt(damagePerTick), sourceUnit);

            yield return new WaitForSeconds(tickRate);
            elapsed += tickRate;
        }

        // Decrement after done
        if (poisonStackCounts.ContainsKey(sourceUnit))
        {
            poisonStackCounts[sourceUnit]--;
            if (poisonStackCounts[sourceUnit] <= 0)
                poisonStackCounts.Remove(sourceUnit);
        }

        UpdatePoisonUI();
    }


    private void UpdatePoisonUI()
    {
        if (poisonStackUIPrefab == null || hero == null)
            return;

        // 🔁 Total stacks from all attackers
        int totalStacks = 0;
        foreach (var kvp in poisonStackCounts)
            totalStacks += kvp.Value;

        // ✅ Server sets the network variable
        if (NetworkManager.Singleton.IsServer)
            poisonStackCount.Value = totalStacks;

        // ✅ Lazy instantiate UI (once per client)
        if (poisonUIInstance == null)
        {
            GameObject go = Instantiate(poisonStackUIPrefab, transform.position, Quaternion.identity);
            poisonUIInstance = go.GetComponent<PoisonStackUI>();
            poisonUIInstance?.Init(transform);
        }

        // ✅ Always update the displayed stack count from the synced value
        poisonUIInstance?.SetStacks(poisonStackCount.Value);
        if (poisonUIInstance != null)
        {
            poisonUIInstance.SetStacks(totalStacks);
            poisonUIInstance.gameObject.SetActive(totalStacks > 0); // 👈 only visible if poisoned
        }
    }


    // 🔰 Optional: Visual shield support
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
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        poisonStackCount.OnValueChanged += (oldVal, newVal) =>
        {
            if (poisonUIInstance == null && poisonStackUIPrefab != null)
            {
                GameObject go = Instantiate(poisonStackUIPrefab, transform.position, Quaternion.identity, transform);
                poisonUIInstance = go.GetComponent<PoisonStackUI>();
                poisonUIInstance?.Init(transform);
            }

            poisonUIInstance?.SetStacks(newVal);
        };
    }

    public void ClearAllPoison()
    {
        poisonStackCounts.Clear();
        poisonStackCount.Value = 0;
        poisonUIInstance?.SetStacks(0);
    }

    public void StopAllBuffs()
    {
        foreach (var pair in activeBuffs)
        {
            if (pair.Value != null)
                StopCoroutine(pair.Value);
        }

        activeBuffs.Clear();
        poisonStackCounts.Clear();
        poisonUIInstance?.SetStacks(0);
    }
    public void ClearAllPoisonFrom(HeroUnit source)
    {
        if (poisonStackCounts.ContainsKey(source))
        {
            poisonStackCounts.Remove(source);
            UpdatePoisonUI();
        }
    }
    public void ClearAllBuffs()
    {
        foreach (var kvp in activeBuffs)
        {
            if (kvp.Value != null)
                StopCoroutine(kvp.Value);
        }

        activeBuffs.Clear();
        poisonStackCounts.Clear();

        // Optional: reset shield, lifesteal, and UI
        currentShield = 0;
        hero.DisableLifesteal();

        if (poisonUIInstance != null)
            poisonUIInstance.SetStacks(0);
    }
    public void HidePoisonUI()
    {
        if (NetworkManager.Singleton.IsServer)
            HidePoisonUIClientRpc(); // Ensure all clients are notified
        else
            InternalHidePoisonUI(); // fallback

        InternalHidePoisonUI(); // host/local fallback
    }
    [ClientRpc]
    private void HidePoisonUIClientRpc()
    {
        InternalHidePoisonUI();
    }
    private void InternalHidePoisonUI()
    {
        if (poisonUIInstance != null)
            poisonUIInstance.gameObject.SetActive(false);
    }
    public void ShowPoisonUIIfNeeded()
    {
        if (poisonUIInstance != null && poisonStackCount.Value > 0)
            poisonUIInstance.gameObject.SetActive(true);
    }
}
