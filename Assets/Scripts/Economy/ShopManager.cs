using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

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
        // Shop is triggered after UI subscribes
    }

    public void RefreshShop()
    {
        CurrentShopHeroes.Clear();

        List<HeroData> pool = new(UnitDatabase.Instance.allHeroes);
        Shuffle(pool);

        for (int i = 0; i < Mathf.Min(shopSize, pool.Count); i++)
            CurrentShopHeroes.Add(pool[i]);

        OnShopUpdated?.Invoke(CurrentShopHeroes);

        Debug.Log("🔄 Shop refreshed with heroes: " +
                  string.Join(", ", CurrentShopHeroes.ConvertAll(h => h.heroName)));
    }

    // ✅ Called from UI/client — routes to server
    public bool TryBuy(HeroData heroData)
    {
        TryBuyServerRpc(heroData.heroId);
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryBuyServerRpc(int heroId, ServerRpcParams rpcParams = default)
    {
        var player = PlayerNetworkState.GetPlayerByClientId(rpcParams.Receive.SenderClientId);
        if (player == null || player.GoldManager == null || player.PlayerDeck == null)
        {
            Debug.LogWarning("❌ [Server] Missing player systems.");
            return;
        }

        HeroData hero = UnitDatabase.Instance.GetHeroById(heroId);
        if (hero == null)
        {
            Debug.LogWarning($"❌ [Server] Hero ID {heroId} not found in database.");
            return;
        }

        if (player.GoldManager.CurrentGold.Value < hero.cost)
        {
            Debug.Log("❌ [Server] Not enough gold.");
            return;
        }

        if (!player.PlayerDeck.TryAddCard(hero))
        {
            Debug.Log("❌ [Server] Deck full.");
            return;
        }

        if (!player.GoldManager.TrySpendGold(hero.cost))
        {
            Debug.Log("❌ [Server] Spend failed.");
            return;
        }

        Debug.Log($"🛒 [Server] Purchased {hero.heroName} for {hero.cost} gold");
    }

    // ✅ Called from client — safely routed to server
    public bool TryReroll()
    {
        TryRerollServerRpc();
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryRerollServerRpc(ServerRpcParams rpcParams = default)
    {
        var player = PlayerNetworkState.GetPlayerByClientId(rpcParams.Receive.SenderClientId);
        if (player == null || player.GoldManager == null)
        {
            Debug.LogWarning("❌ [Server] Missing player or gold manager.");
            return;
        }

        if (!player.GoldManager.TrySpendGold(rerollCost))
        {
            Debug.Log("❌ [Server] Not enough gold to reroll.");
            return;
        }

        RefreshShop();
        Debug.Log("🔁 [Server] Shop rerolled.");
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
