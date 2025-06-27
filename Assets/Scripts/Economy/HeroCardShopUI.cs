using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class HeroCardShopUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image iconImage;
    public TMP_Text heroNameText;
    public TMP_Text costText;
    public Button buyButton;

    private HeroData hero;
    private Action<int> onBuyClicked;

    /// <summary>
    /// Called by ShopUIManager when rendering a new hero card.
    /// </summary>
    public void Setup(HeroData data, Action<int> buyCallback)
    {
        hero = data;
        onBuyClicked = buyCallback;

        iconImage.sprite = hero.heroIcon;
        heroNameText.text = hero.heroName;
        costText.text = $"🪙 {hero.cost}";

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnClickBuy);
        SetBuyable(true); // Default to enabled until updated by gold logic
    }

    private void OnClickBuy()
    {
        if (hero == null)
        {
            Debug.LogWarning("❌ Cannot buy: HeroData is null.");
            return;
        }

        Debug.Log($"🛒 [Client] Buy button clicked for heroId: {hero.heroId}");
        onBuyClicked?.Invoke(hero.heroId);
    }

    /// <summary>
    /// Enables or disables the Buy button externally.
    /// </summary>
    public void SetBuyable(bool canBuy)
    {
        buyButton.interactable = canBuy;
    }
}
