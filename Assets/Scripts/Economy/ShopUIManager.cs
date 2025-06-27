using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;


public class ShopUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject heroCardPrefab;
    public Transform cardContainer;
    public TMP_Text goldText;
    public Button rerollButton;
    public TMP_Text rerollCostText;

    private List<HeroCardShopUI> cardUIs = new();

    private void Start()
    {
        ShopManager.Instance.OnShopUpdated += RenderShop;
        rerollButton.onClick.AddListener(() =>
        {
            ShopManager.Instance.TryReroll();
        });

        rerollCostText.text = $"🔁 Reroll ({ShopManager.Instance.RerollCost}🪙)";
    }

    private void Update()
    {
        // Update gold display live
        var player = PlayerNetworkState.LocalPlayer;
        if (player != null && player.GoldManager != null)
            goldText.text = $"🪙 {player.GoldManager.CurrentGold.Value}";
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
}
