using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using System;

public class TraitTracker : NetworkBehaviour
{
    public Dictionary<TraitDefinition, int> traitCounts = new();
    public Dictionary<TraitDefinition, TraitTierBonus> activeBonuses = new();
    public List<AdvancedSynergyDefinition> activeAdvancedSynergies = new();

    public void RecalculateTraits(List<HeroUnit> currentUnits, int playerLevel)
    {
        Debug.Log("🧮 Recalculating Traits");

        // 🔁 Save previous bonuses to detect changes
        var previousActiveBonuses = new Dictionary<TraitDefinition, TraitTierBonus>(activeBonuses);

        // 🧮 Count traits from current units
        traitCounts.Clear();
        foreach (var unit in currentUnits)
        {
            foreach (var trait in unit.heroData.traits)
            {
                if (!traitCounts.ContainsKey(trait))
                    traitCounts[trait] = 0;
                traitCounts[trait]++;
            }
        }

        // 🧠 Recalculate bonuses
        activeBonuses.Clear();
        foreach (var trait in traitCounts.Keys)
        {
            int count = traitCounts[trait];
            var bonus = trait.tierBonuses
                .Where(t => t.requiredCount <= count)
                .OrderByDescending(t => t.requiredCount)
                .FirstOrDefault();

            if (bonus != null)
            {
                activeBonuses[trait] = bonus;

                // ✅ Trait just got activated
                if (!previousActiveBonuses.ContainsKey(trait))
                {
                    Debug.Log($"🧬 Trait Activated: <b>{trait.traitName}</b> for Player {OwnerClientId} at count {count}");
                }
                else if (previousActiveBonuses[trait].requiredCount != bonus.requiredCount)
                {
                    Debug.Log($"🔁 Trait Upgraded: <b>{trait.traitName}</b> → {bonus.requiredCount} (was {previousActiveBonuses[trait].requiredCount})");
                }
            }
        }

        // 🛑 Log deactivations
        foreach (var trait in previousActiveBonuses.Keys)
        {
            if (!activeBonuses.ContainsKey(trait))
            {
                Debug.Log($"🛑 Trait Deactivated: <b>{trait.traitName}</b> for Player {OwnerClientId}");
            }
        }

        // 🧠 Check advanced synergies
        activeAdvancedSynergies.Clear();
        foreach (var synergy in SynergyDatabase.Instance.AllAdvancedSynergies)
        {
            if (playerLevel >= synergy.requiredPlayerLevel &&
                synergy.requiredTraits.All(trait => traitCounts.ContainsKey(trait)))
            {
                activeAdvancedSynergies.Add(synergy);
            }
        }

        // 🔄 Notify listeners
        OnTraitsChanged?.Invoke();

        // 🧾 Debug breakdown
        foreach (var kvp in traitCounts)
            Debug.Log($"Trait: {kvp.Key.traitName}, Count: {kvp.Value}");

        foreach (var kvp in activeBonuses)
            Debug.Log($"✅ Active Trait Bonus: {kvp.Key.traitName} → {kvp.Value.requiredCount}");
    }



    public event Action OnTraitsChanged;
    public bool IsSynergyActive(string traitName)
    {
        return activeBonuses.Keys.Any(t => t.traitName == traitName);
    }
    public int GetSynergyTier(string traitName)
    {
        foreach (var kvp in activeBonuses)
        {
            if (kvp.Key.traitName == traitName)
            {
                return kvp.Value.requiredCount; // e.g., 1, 2, 3, 4
            }
        }
        return 0;
    }



}
