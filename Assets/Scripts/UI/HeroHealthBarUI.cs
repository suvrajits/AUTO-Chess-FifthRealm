using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;

public class HeroHealthBarUI : MonoBehaviour
{
    [Header("Fill Images")]
    public Image greenFillImage;
    public Image redFillImage;

    private float maxHealth;
    private float currentHealth;

    private Image activeFillImage;
    public Transform traitIconsContainer;    // Drag TraitIconPanel here
    public GameObject traitIconPrefab;

    private void Start()
    {
        // Find NetworkObject in parent
        NetworkObject netObj = GetComponentInParent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogWarning("❗ HeroHealthBarUI: No NetworkObject found in parent.");
            return;
        }

        // Invert the logic: show red for own units, green for others
        bool isMine = netObj.OwnerClientId == NetworkManager.Singleton.LocalClientId;

        // 🔄 REVERSED
        greenFillImage.gameObject.SetActive(!isMine);
        redFillImage.gameObject.SetActive(isMine);

        activeFillImage = isMine ? redFillImage : greenFillImage;
    }

    public void Init(float max)
    {
        maxHealth = max;
        currentHealth = max;
        UpdateBar();
    }

    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (activeFillImage != null)
        {
            activeFillImage.fillAmount = currentHealth / maxHealth;
        }
    }
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
    private Dictionary<TraitDefinition, GameObject> traitIconMap = new();
    public void InitTraitIcons(List<TraitDefinition> traits)
    {
        // ✅ Clear old icons
        foreach (Transform child in traitIconsContainer)
            Destroy(child.gameObject);
        traitIconMap.Clear();

        foreach (var trait in traits)
        {
            GameObject iconGO = Instantiate(traitIconPrefab, traitIconsContainer);

            // 🟨 Optional: Adjust icon scale (e.g., taller for clarity)
            iconGO.transform.localScale = new Vector3(1.3f, 6f, 1.3f);

            Image img = iconGO.GetComponent<Image>();
            if (img != null && trait.traitIcon != null)
                img.sprite = trait.traitIcon;

            traitIconMap[trait] = iconGO;

            // ✅ Enable pulse if trait is active
            var pulse = iconGO.GetComponent<TraitIconPulseHandler>();
            if (pulse != null && GetIsTraitActive(trait))
                pulse.SetActive(true);
        }
    }
    private bool GetIsTraitActive(TraitDefinition trait)
    {
        var hero = GetComponentInParent<HeroUnit>();
        if (hero == null) return false;

        var player = PlayerNetworkState.GetPlayerByClientId(hero.OwnerClientId);
        if (player == null || player.TraitTracker == null) return false;

        return player.TraitTracker.activeBonuses.ContainsKey(trait);
    }

}
