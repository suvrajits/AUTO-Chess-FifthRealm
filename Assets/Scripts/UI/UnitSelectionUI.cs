using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject cardTemplate;
    [SerializeField] private Transform buttonContainer;

    [Header("Optional")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    private List<Button> instantiatedButtons = new();
    private PlayerCardDeck playerDeck;
    private HeroCardInstance currentlySelectedCard;

    private void Start()
    {
        cardTemplate.SetActive(false);

        // Find the player's deck
        var localPlayer = PlayerNetworkState.GetLocalPlayer();
        if (localPlayer == null)
        {
            Debug.LogError("❌ Local PlayerNetworkState not found.");
            return;
        }

        playerDeck = localPlayer.PlayerDeck;

        if (playerDeck == null)
        {
            Debug.LogError("❌ PlayerCardDeck not assigned to local player.");
            return;
        }

        if (playerDeck == null)
        {
            Debug.LogError("❌ PlayerCardDeck not found in scene.");
            return;
        }

        // Subscribe to deck updates
        playerDeck.DeckChanged += RefreshDeckUI;

        // Initial render
        RefreshDeckUI();
    }

    private void RefreshDeckUI()
    {
        // Clear existing buttons
        foreach (var btn in instantiatedButtons)
            Destroy(btn.gameObject);

        instantiatedButtons.Clear();

        // Rebuild UI from updated deck
        for (int i = 0; i < playerDeck.cards.Count; i++)
        {
            HeroCardInstance cardInstance = playerDeck.cards[i];

            GameObject cardObj = Instantiate(cardTemplate, buttonContainer);
            cardObj.SetActive(true);

            HeroCardUI cardUI = cardObj.GetComponent<HeroCardUI>();
            cardUI.Setup(cardInstance, this, i);

            Button btn = cardObj.GetComponent<Button>();

            // ✅ Closure-safe local copy for correct onClick binding
            int capturedIndex = i;
            HeroCardInstance capturedCard = cardInstance;
            btn.onClick.AddListener(() => OnHeroClicked(capturedCard, capturedIndex));

            instantiatedButtons.Add(btn);
        }

        // ✅ Optionally select the first card again
        if (playerDeck.cards.Count > 0)
        {
            SelectHero(playerDeck.cards[0], 0); // ✅ Pass full card instance
        }
    }



    private void OnHeroClicked(HeroCardInstance cardInstance, int index)
    {
        SelectHero(cardInstance, index); // ✅ FIXED
        currentlySelectedCard = cardInstance;
    }

    public void SelectHero(HeroCardInstance cardInstance, int selectedIndex)
    {
        UnitSelectionManager.Instance.SelectHero(cardInstance.baseHero);
        UnitSelectionManager.Instance.SetSelectedCard(cardInstance);

        for (int i = 0; i < instantiatedButtons.Count; i++)
        {
            var colors = instantiatedButtons[i].colors;
            colors.normalColor = (i == selectedIndex) ? selectedColor : normalColor;
            instantiatedButtons[i].colors = colors;
        }
    }


    private void OnDestroy()
    {
        if (playerDeck != null)
            playerDeck.DeckChanged -= RefreshDeckUI;
    }
}
