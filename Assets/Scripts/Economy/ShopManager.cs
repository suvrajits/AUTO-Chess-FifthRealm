using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ShopManager : NetworkBehaviour
{
    [Header("Shop Settings")]
    [SerializeField] private int shopSize = 5;
    [SerializeField] private int rerollCost = 2;

    public static ShopManager Instance { get; private set; }
    private static List<int> deferredHeroIds = null;
    public int RerollCost => rerollCost;
    private static Dictionary<ulong, int[]> deferredRerollData = new();
    

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnDestroy() // ✅ override applied
    {
        if (IsServer && NetworkManager != null)
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;

        base.OnDestroy();
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"🛒 [Server] Player {clientId} connected. Generating shop...");
        if (!PlayerNetworkState.AllPlayers.TryGetValue(clientId, out var player))
        {
            Debug.LogWarning($"❌ [Server] No PlayerNetworkState found for {clientId}");
            return;
        }

        if (PlayerShopState.AllShops.ContainsKey(clientId))
        {
            Debug.Log($"ℹ️ [Server] Shop already initialized for {clientId}");
            return;
        }

        var shopState = player.GetComponent<PlayerShopState>();
        shopState.Init(clientId, player);
    }

    // Called from ShopUIManager.cs after spawn
    public void RequestInitialShop()
    {
        if (!IsClient) return;
        RequestShopServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestShopServerRpc(ServerRpcParams rpcParams = default)
    {
        var clientId = rpcParams.Receive.SenderClientId;

        if (!PlayerShopState.AllShops.TryGetValue(clientId, out var shop))
        {
            Debug.LogWarning($"❌ [Server] No PlayerShopState found for Client {clientId}");
            return;
        }

        SyncShopToClient(clientId, shop.CurrentShop);
    }

    public void TryBuy(int heroId)
    {
        TryBuyServerRpc(heroId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryBuyServerRpc(int heroId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!PlayerShopState.AllShops.TryGetValue(clientId, out var shop))
        {
            Debug.LogWarning($"❌ [Server] No PlayerShopState for {clientId}");
            return;
        }

        shop.PurchaseHero(heroId);
    }

    public void TryReroll()
    {
        TryRerollServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryRerollServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!PlayerShopState.AllShops.TryGetValue(clientId, out var shop))
        {
            Debug.LogWarning($"❌ [Server] No PlayerShopState for {clientId}");
            return;
        }

        shop.RerollShop();
    }

    public void SyncShopToClient(ulong clientId, List<HeroData> shopList)
    {
        List<int> heroIds = shopList.ConvertAll(h => h.heroId);

        SyncShopClientRpc(heroIds.ToArray(), new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        });
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    [ClientRpc]
    private void SyncShopClientRpc(int[] heroIds, ClientRpcParams rpcParams = default)
    {
        if (!IsClient) return;
        StartCoroutine(WaitForShopUIAndRender(heroIds.ToArray()));
    }

    private IEnumerator WaitForShopUIAndRender(int[] heroIds)
    {
        float timeout = 5f;
        float elapsed = 0f;

        ShopUIManager ui = null;

        while (ui == null && elapsed < timeout)
        {
            ui = FindFirstObjectByType<ShopUIManager>();
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (ui != null)
        {
            Debug.Log("🟢 Shop UI ready, rendering heroIds.");
            ui.RenderShop(new List<int>(heroIds));
        }
        else
        {
            Debug.LogWarning("⚠️ Shop UI not found in time. Deferring...");
            ulong localId = NetworkManager.Singleton.LocalClientId;
            deferredRerollData[localId] = heroIds;
        }
    }
    [ClientRpc]
    private void ForceRenderClientRpc(int[] heroIds, ClientRpcParams rpcParams = default)
    {
        if (!IsClient) return;

        StartCoroutine(WaitForShopUIAndRender(heroIds));
    }


    public void RerollAllShopsFree()
    {
        if (!IsServer) return;

        Debug.Log("🔁 [ShopManager] Free reroll for all players.");

        foreach (var kvp in PlayerShopState.AllShops)
        {
            ulong clientId = kvp.Key;
            PlayerShopState shop = kvp.Value;

            shop.RerollShopFree();

            List<int> heroIds = shop.CurrentShop.ConvertAll(h => h.heroId);
            ForceRenderClientRpc(heroIds.ToArray(), new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            });

            Debug.Log($"🛒 Synced reroll to client {clientId}");
        }
    }

    public static bool HasDeferredShop() => deferredHeroIds != null && deferredHeroIds.Count > 0;
    public static List<int> ConsumeDeferredShop()
    {
        var copy = deferredHeroIds;
        deferredHeroIds = null;
        return copy;
    }
   
    [ServerRpc(RequireOwnership = false)]
    private void RequestFreshShopServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!PlayerShopState.AllShops.TryGetValue(clientId, out var shop))
        {
            Debug.LogWarning($"❌ No PlayerShopState for {clientId}");
            return;
        }

        shop.RerollShop(); // Uses existing server-side logic, no gold deducted

        // Force sync to client immediately
        List<int> heroIds = shop.CurrentShop.ConvertAll(h => h.heroId);
        ForceRenderClientRpc(heroIds.ToArray(), new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        });
    }
    public void RequestFreshShop()
    {
        if (!IsClient) return;
        RequestFreshShopServerRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    private void RequestFreeShopServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!PlayerShopState.AllShops.TryGetValue(clientId, out var shop))
        {
            Debug.LogWarning($"❌ No PlayerShopState for {clientId}");
            return;
        }

        shop.RerollShopFree(); // ✅ Free version that doesn’t deduct gold
    }
    public void RequestFreeShop()
    {
        if (!IsClient) return;
        RequestFreeShopServerRpc();
    }
    public static bool TryConsumeDeferredReroll(out List<int> heroIds)
    {
        heroIds = null;
        ulong localId = NetworkManager.Singleton.LocalClientId;

        if (deferredRerollData.TryGetValue(localId, out var ids))
        {
            heroIds = new List<int>(ids);
            deferredRerollData.Remove(localId);
            return true;
        }

        return false;
    }

}
