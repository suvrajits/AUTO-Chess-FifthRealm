using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("Shop Settings")]
    [SerializeField] private int shopSize = 5;
    [SerializeField] private int rerollCost = 2;
    public int RerollCost => rerollCost;
    public List<HeroData> CurrentShopHeroes { get; private set; } = new();

    public static ShopManager Instance { get; private set; }

    public event Action<List<HeroData>> OnShopUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RefreshShop();
    }

    // 🎲 Replaces shop with new heroes
    public void RefreshShop()
    {
        CurrentShopHeroes.Clear();

        List<HeroData> pool = new(UnitDatabase.Instance.allHeroes);
        Shuffle(pool);

        for (int i = 0; i < Mathf.Min(shopSize, pool.Count); i++)
            CurrentShopHeroes.Add(pool[i]);

        OnShopUpdated?.Invoke(CurrentShopHeroes);
        Debug.Log("🔄 Shop refreshed with heroes: " + string.Join(", ", CurrentShopHeroes.ConvertAll(h => h.heroName)));
    }

    // 💰 Attempt to purchase a card
    public bool TryBuy(HeroData heroData)
    {
        var player = PlayerNetworkState.LocalPlayer;
        if (player == null || player.GoldManager == null || player.PlayerDeck == null)
        {
            Debug.LogWarning("❌ Cannot complete purchase – missing player systems.");
            return false;
        }

        if (player.GoldManager.CurrentGold.Value < heroData.cost)
        {
            Debug.Log("❌ Not enough gold to buy " + heroData.heroName);
            return false;
        }

        if (!player.PlayerDeck.TryAddCard(heroData))
        {
            Debug.Log("❌ Deck full! Sell a card before buying more.");
            return false;
        }

        player.GoldManager.TrySpendGold(heroData.cost);
        Debug.Log($"🛒 Purchased: {heroData.heroName} for {heroData.cost} gold");

        RefreshShop(); // Optional: auto-refresh
        return true;
    }

    // 🔁 Reroll the shop
    public bool TryReroll()
    {
        var player = PlayerNetworkState.LocalPlayer;
        if (player == null || player.GoldManager == null)
        {
            Debug.LogWarning("❌ Cannot reroll – player or GoldManager missing.");
            return false;
        }

        if (player.GoldManager.TrySpendGold(rerollCost))
        {
            RefreshShop();
            Debug.Log("🔁 Shop rerolled.");
            return true;
        }

        Debug.Log("❌ Not enough gold to reroll.");
        return false;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int k = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[k]) = (list[k], list[i]);
        }
    }
}
