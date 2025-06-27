using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroCardShopUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image iconImage;
    public TMP_Text heroNameText;
    public TMP_Text costText;
    public Button buyButton;

    private HeroData hero;

    public void Setup(HeroData data)
    {
        hero = data;

        iconImage.sprite = hero.heroIcon;
        heroNameText.text = hero.heroName;
        costText.text = $"🪙 {hero.cost}";

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() =>
        {
            bool success = ShopManager.Instance.TryBuy(hero);
            if (!success)
            {
                // TODO: trigger tooltip or shake animation
            }
        });
    }

    public void SetBuyable(bool canBuy)
    {
        buyButton.interactable = canBuy;
    }
}
