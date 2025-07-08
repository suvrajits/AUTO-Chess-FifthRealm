using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerShopState : NetworkBehaviour
{
    public List<HeroData> CurrentShop { get; private set; } = new();
    public const int ShopSize = 5;

    private PlayerNetworkState player;

    public static Dictionary<ulong, PlayerShopState> AllShops = new();

    public void Init(ulong clientId, PlayerNetworkState playerState)
    {
        player = playerState;
        Debug.Log($"🛠 Init PlayerShopState for ClientId: {clientId}");

        if (IsServer)
        {
            AllShops[clientId] = this;
            StartCoroutine(WaitForShopManagerAndGenerate());
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer && !AllShops.ContainsKey(OwnerClientId))
        {
            AllShops.Add(OwnerClientId, this);
            Debug.Log($"📦 [Server] Registered PlayerShopState for Client {OwnerClientId}");
        }

        if (IsOwner && IsServer)
        {
            Debug.Log("⏳ [Host] Scheduling delayed shop sync to self...");
            StartCoroutine(DelayedShopSyncToSelf());
        }
    }

    public override void OnDestroy()
    {
        if (IsServer && AllShops.ContainsKey(OwnerClientId))
            AllShops.Remove(OwnerClientId);

        base.OnDestroy();
    }

    public void RerollShop()
    {
        if (!IsServer || player == null || player.GoldManager == null)
            return;

        if (!player.GoldManager.TrySpendGold(ShopManager.Instance.RerollCost))
        {
            Debug.LogWarning($"❌ [Server] Not enough gold to reroll for client {OwnerClientId}");
            return;
        }

        GenerateNewShop();
    }

    public void PurchaseHero(int heroId)
    {
        if (!IsServer || player == null || player.GoldManager == null || player.PlayerDeck == null)
            return;

        HeroData hero = CurrentShop.Find(h => h.heroId == heroId);
        if (hero == null)
        {
            Debug.LogWarning($"❌ [Server] Hero {heroId} not found in shop for client {OwnerClientId}");
            return;
        }

        if (player.PlayerDeck.cards.Count >= player.PlayerDeck.Capacity)
        {
            Debug.LogWarning($"❌ [Server] Deck full. Cannot buy {hero.heroName} for client {OwnerClientId}");
            return;
        }

        if (!player.GoldManager.TrySpendGold(hero.cost))
        {
            Debug.LogWarning($"❌ [Server] Not enough gold to buy {hero.heroName}");
            return;
        }

        bool didFuse;
        if (!player.PlayerDeck.TryAddCard(hero, out didFuse))
        {
            Debug.LogWarning($"❌ [Server] Deck full. Cannot buy {hero.heroName}");
            return;
        }

        player.PlayerDeck.SyncDeckToClient(OwnerClientId);
        CurrentShop.Remove(hero);
        Debug.Log($"✅ [Server] Client {OwnerClientId} bought {hero.heroName}");

        ShopManager.Instance.SyncShopToClient(OwnerClientId, CurrentShop);
    }

    private void GenerateNewShop()
    {
        CurrentShop.Clear();

        List<HeroData> pool = new(UnitDatabase.Instance.allHeroes);
        Shuffle(pool);

        for (int i = 0; i < Mathf.Min(ShopSize, pool.Count); i++)
            CurrentShop.Add(pool[i]);

        ShopManager.Instance.SyncShopToClient(OwnerClientId, CurrentShop);
    }

    private IEnumerator WaitForShopManagerAndGenerate()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (ShopManager.Instance == null && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (ShopManager.Instance != null)
        {
            Debug.Log($"✅ ShopManager available after {elapsed:F2}s. Generating shop...");
            GenerateNewShop();
        }
        else
        {
            Debug.LogError("❌ ShopManager.Instance still null after timeout. Shop not generated.");
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int k = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[k]) = (list[k], list[i]);
        }
    }

    private IEnumerator DelayedShopSyncToSelf()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (ShopManager.Instance == null && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (ShopManager.Instance != null)
        {
            Debug.Log($"✅ [Host] Sending initial shop to self after delay.");
            ShopManager.Instance.SyncShopToClient(OwnerClientId, CurrentShop);
        }
        else
        {
            Debug.LogError("❌ ShopManager.Instance still null after timeout. Cannot sync shop.");
        }
    }


    public void DisableShop()
    {
        CurrentShop.Clear();
        ShopManager.Instance.SyncShopToClient(OwnerClientId, CurrentShop);
        Debug.Log($"🚫 Shop disabled for player {OwnerClientId}");
    }
}
