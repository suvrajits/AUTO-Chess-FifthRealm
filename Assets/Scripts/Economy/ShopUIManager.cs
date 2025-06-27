using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject heroCardPrefab;
    public Transform cardContainer;

    public Button rerollButton;
    public TMP_Text rerollCostText;

    private List<HeroCardShopUI> cardUIs = new();

    private void Start()
    {
        // Prevent duplicate listeners
        rerollButton.onClick.RemoveAllListeners();
        rerollButton.onClick.AddListener(OnClickReroll);

        // 1. Subscribe to shop update event
        ShopManager.Instance.OnShopUpdated += RenderShop;

        // 2. Update UI for reroll cost
        rerollCostText.text = $"{ShopManager.Instance.RerollCost} 🪙";

        // 3. Refresh the shop after listener is hooked
        ShopManager.Instance.RefreshShop();
    }

    private void RenderShop(List<HeroData> newCards)
    {
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        cardUIs.Clear();

        var player = PlayerNetworkState.LocalPlayer;
        int currentDeckCount = player?.PlayerDeck?.cards.Count ?? 0;
        int maxDeck = player?.PlayerDeck?.Capacity ?? 9;
        int gold = player?.GoldManager?.CurrentGold.Value ?? 0;

        foreach (var hero in newCards)
        {
            var cardObj = Instantiate(heroCardPrefab, cardContainer);
            var ui = cardObj.GetComponent<HeroCardShopUI>();
            ui.Setup(hero);

            bool canAfford = gold >= hero.cost;
            bool hasSpace = currentDeckCount < maxDeck;
            ui.SetBuyable(canAfford && hasSpace);

            cardUIs.Add(ui);
        }
    }

    public void OnClickReroll()
    {
        bool success = ShopManager.Instance.TryReroll();
        
        if (!success)
        {
            Debug.Log("❌ Reroll failed: Not enough gold or player not valid.");
        }
    }
}
