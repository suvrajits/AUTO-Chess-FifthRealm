using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject heroCardPrefab;
    [SerializeField] private TMP_Text rerollCostText;
    [SerializeField] private Button rerollButton;

    private List<HeroCardShopUI> activeCards = new();

    private void Start()
    {
        rerollButton.onClick.RemoveAllListeners();
        rerollButton.onClick.AddListener(OnClickReroll);
        rerollCostText.text = $"🌀 {ShopManager.Instance.RerollCost} gold";

        // Tell server we are ready to receive our personal shop list
        ShopManager.Instance.RequestInitialShop();
    }

    public void RenderShop(List<int> heroIds)
    {
        Clear();

        foreach (int id in heroIds)
        {
            HeroData hero = UnitDatabase.Instance.GetHeroById(id);
            if (hero == null) continue;

            GameObject cardGO = Instantiate(heroCardPrefab, cardContainer);
            HeroCardShopUI card = cardGO.GetComponent<HeroCardShopUI>();
            card.Setup(hero, OnCardBuyClicked);

            activeCards.Add(card);
        }

        // ✅ Re-enable the reroll button when shop is done rendering
        rerollButton.interactable = true;
    }

    private void OnCardBuyClicked(int heroId)
    {
        rerollButton.interactable = false; // Optionally lock input until shop updates
        ShopManager.Instance.TryBuy(heroId);
    }

    private void OnClickReroll()
    {
        rerollButton.interactable = false;
        ShopManager.Instance.TryReroll();
    }

    public void Clear()
    {
        foreach (var card in activeCards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        activeCards.Clear();
    }
}
