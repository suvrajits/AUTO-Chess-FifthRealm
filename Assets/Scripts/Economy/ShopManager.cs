using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ShopManager : NetworkBehaviour
{
    [Header("Shop Settings")]
    [SerializeField] private int shopSize = 5;
    [SerializeField] private int rerollCost = 2;

    public static ShopManager Instance { get; private set; }
    public int RerollCost => rerollCost;

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

    [ClientRpc]
    private void SyncShopClientRpc(int[] heroIds, ClientRpcParams rpcParams = default)
    {
        if (!IsClient) return;

        Debug.Log($"📦 [Client] Received shop list: {string.Join(", ", heroIds)}");

        var ui = UnityEngine.Object.FindFirstObjectByType<ShopUIManager>();
        if (ui != null)
        {
            ui.RenderShop(new List<int>(heroIds)); // ✅ Refresh with new list
        }
    }


    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
