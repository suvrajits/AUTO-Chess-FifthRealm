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
        foreach (var cardInstance in playerDeck.cards)
        {
            GameObject cardObj = Instantiate(cardTemplate, buttonContainer);
            cardObj.SetActive(true);

            HeroCardUI cardUI = cardObj.GetComponent<HeroCardUI>();
            cardUI.Setup(
                cardInstance.baseHero,
                this,
                instantiatedButtons.Count,
                cardInstance.starLevel
            );

            Button btn = cardObj.GetComponent<Button>();
            instantiatedButtons.Add(btn);
        }

        // Optionally select the first card again
        if (instantiatedButtons.Count > 0)
        {
            SelectHero(playerDeck.cards[0].baseHero, 0);
        }
    }


    private void OnHeroClicked(HeroData hero, int index)
    {
        SelectHero(hero, index);
        currentlySelectedCard = playerDeck.cards[index]; // Track currently selected
    }

    public void SelectHero(HeroData hero, int selectedIndex)
    {
        UnitSelectionManager.Instance.SelectHero(hero);

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
