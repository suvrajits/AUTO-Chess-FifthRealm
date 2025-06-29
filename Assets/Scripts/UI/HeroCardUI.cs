using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeroCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TMP_Text heroName;
    public Transform starContainer; // ⭐ Holds instantiated star icons
    public GameObject starPrefab;   // ⭐ A single star image prefab
    public Button sellButton;       // 🔄 NEW: Sell button

    private HeroData assignedHero;
    private UnitSelectionUI selectionUI;
    private int index;
    private HeroCardInstance cardInstance;

    public void Setup(HeroCardInstance instance, UnitSelectionUI selectionUIRef, int cardIndex)
    {
        cardInstance = instance;
        assignedHero = instance.baseHero;
        selectionUI = selectionUIRef;
        index = cardIndex;

        if (iconImage != null)
            iconImage.sprite = assignedHero.heroIcon;

        if (heroName != null)
            heroName.text = assignedHero.heroName;

        GenerateStars(instance.starLevel);

        // Selection
        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(OnClick);

        // ✅ SELL BUTTON hook
        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(OnSellClicked);
        }
    }

    private void OnClick()
    {
        if (assignedHero != null)
        {
            selectionUI.SelectHero(cardInstance, index);
        }
    }

    private void OnSellClicked()
    {
        if (cardInstance == null || cardInstance.baseHero == null)
        {
            Debug.LogWarning("❌ Cannot sell: No card assigned.");
            return;
        }

        var localPlayer = PlayerNetworkState.GetLocalPlayer();
        if (localPlayer != null)
        {
            Debug.Log($"💸 Selling heroId {cardInstance.baseHero.heroId} (★{cardInstance.starLevel})");
            localPlayer.SellHeroCardServerRpc(cardInstance.baseHero.heroId, cardInstance.starLevel);
        }
        else
        {
            Debug.LogWarning("❌ Local player not found. Cannot sell.");
        }
    }

    private void GenerateStars(int starLevel)
    {
        foreach (Transform child in starContainer)
        {
            Destroy(child.gameObject);
        }

        int clampedStars = Mathf.Clamp(starLevel, 1, 5);

        for (int i = 0; i < clampedStars; i++)
        {
            GameObject star = Instantiate(starPrefab, starContainer);
            star.SetActive(true);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(starContainer.GetComponent<RectTransform>());
    }
}
