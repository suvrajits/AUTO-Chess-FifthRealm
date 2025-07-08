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
        // Count traits
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

        // Determine trait tier bonuses
        activeBonuses.Clear();
        foreach (var trait in traitCounts.Keys)
        {
            int count = traitCounts[trait];
            var bonus = trait.tierBonuses
                .Where(t => t.requiredCount <= count)
                .OrderByDescending(t => t.requiredCount)
                .FirstOrDefault();
            if (bonus != null)
                activeBonuses[trait] = bonus;
        }

        // Check for advanced synergies
        activeAdvancedSynergies.Clear();
        foreach (var synergy in SynergyDatabase.Instance.AllAdvancedSynergies)
        {
            if (playerLevel >= synergy.requiredPlayerLevel &&
                synergy.requiredTraits.All(trait => traitCounts.ContainsKey(trait)))
            {
                activeAdvancedSynergies.Add(synergy);
            }
        }

        OnTraitsChanged?.Invoke();
    }

    public event Action OnTraitsChanged;

}
