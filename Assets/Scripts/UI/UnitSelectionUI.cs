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
        // Clear previous UI
        foreach (var btn in instantiatedButtons)
        {
            if (btn != null)
                Destroy(btn.gameObject);
        }
        instantiatedButtons.Clear();

        var deck = playerDeck.cards;
        if (deck == null || deck.Count == 0)
        {
            Debug.Log("📭 No cards in deck. Unit selection is empty.");
            return;
        }

        for (int i = 0; i < deck.Count; i++)
        {
            HeroCardInstance cardInstance = deck[i];
            HeroData hero = cardInstance.baseHero;

            GameObject cardObj = Instantiate(cardTemplate, buttonContainer);
            cardObj.SetActive(true);

            HeroCardUI cardUI = cardObj.GetComponent<HeroCardUI>();
            cardUI.Setup(hero, this, i, cardInstance.starLevel); // Pass star level if needed

            Button btn = cardObj.GetComponent<Button>();
            int index = i; // Capture loop variable
            btn.onClick.AddListener(() => OnHeroClicked(hero, index));
            instantiatedButtons.Add(btn);
        }

        // Auto-select first hero if none selected
        if (currentlySelectedCard == null && deck.Count > 0)
        {
            OnHeroClicked(deck[0].baseHero, 0);
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
