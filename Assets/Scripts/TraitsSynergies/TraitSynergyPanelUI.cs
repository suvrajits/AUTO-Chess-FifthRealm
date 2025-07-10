using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TraitSynergyPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject traitEntryPrefab;
    [SerializeField] private Transform traitListContainer;
    [SerializeField] private GameObject advancedSynergyPrefab;
    [SerializeField] private Transform advancedSynergyContainer;

    private TraitTracker localTracker;

    private void Start()
    {
        // Get the local player
        var player = PlayerNetworkState.GetLocalPlayer();
        if (player == null) return;

        localTracker = player.TraitTracker;
        if (localTracker != null)
        {
            localTracker.OnTraitsChanged += UpdateTraitPanel;
            UpdateTraitPanel();
        }
    }

    private void OnDestroy()
    {
        if (localTracker != null)
            localTracker.OnTraitsChanged -= UpdateTraitPanel;
    }

    private void UpdateTraitPanel()
    {
        foreach (Transform child in traitListContainer) Destroy(child.gameObject);
        foreach (Transform child in advancedSynergyContainer) Destroy(child.gameObject);

        foreach (var kvp in localTracker.activeBonuses)
        {
            var entry = Instantiate(traitEntryPrefab, traitListContainer);
            var icon = entry.transform.Find("Icon").GetComponent<Image>();
            var text = entry.transform.Find("Text").GetComponent<TMP_Text>();

            icon.sprite = kvp.Key.traitIcon;
            text.text = $"{kvp.Key.symbol} {kvp.Key.traitName} ({localTracker.traitCounts[kvp.Key]}/{GetMaxTier(kvp.Key)})\n<size=70%>{kvp.Value.description}</size>";
        }

        foreach (var synergy in localTracker.activeAdvancedSynergies)
        {
            var entry = Instantiate(advancedSynergyPrefab, advancedSynergyContainer);
            var icon = entry.transform.Find("Icon").GetComponent<Image>();
            var text = entry.transform.Find("Text").GetComponent<TMP_Text>();

            icon.sprite = synergy.icon;
            text.text = $"{synergy.synergyName}\n<size=70%>{synergy.description}</size>";
        }
    }

    private int GetMaxTier(TraitDefinition trait)
    {
        int max = 0;
        foreach (var tier in trait.tierBonuses)
            if (tier.requiredCount > max)
                max = tier.requiredCount;
        return max;
    }
}