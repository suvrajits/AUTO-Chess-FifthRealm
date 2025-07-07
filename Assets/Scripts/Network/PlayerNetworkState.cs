using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerNetworkState : NetworkBehaviour
{
    public static Dictionary<ulong, Camera> AllPlayerCameras = new();
    public static Dictionary<ulong, PlayerNetworkState> AllPlayers = new();
    public static PlayerNetworkState LocalPlayer { get; private set; }

    public static event Action<PlayerNetworkState> OnAnyPlayerFullySpawned;

    public GoldManager GoldManager { get; private set; }
    public PlayerCardDeck PlayerDeck { get; private set; }
    

    private Camera playerCamera;
    public PlayerShopState ShopState { get; private set; }
    [SerializeField] private GameObject playerShopStatePrefab;
    public PlayerHealthManager HealthManager { get; private set; }
    public bool IsAlive => HealthManager != null && !HealthManager.IsDead;
    public NetworkVariable<bool> IsEliminated = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> CurrentRound = new NetworkVariable<int>(
    1,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    public int MaxUnitsAllowed => Mathf.Min(2 + CurrentRound.Value - 1, 8);
    public NetworkVariable<int> PlacedUnitCount = new NetworkVariable<int>(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );
    private void Awake()
    {
        GoldManager = GetComponent<GoldManager>();
        HealthManager = GetComponent<PlayerHealthManager>();

    }
    public static PlayerNetworkState GetLocalPlayer()
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        return AllPlayers.TryGetValue(localId, out var player) ? player : null;
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        PlayerDeck = GetComponentInChildren<PlayerCardDeck>();
        if (IsServer && HealthManager != null)
        {
            HealthManager.CurrentHealth.Value = 20;
            Debug.Log($"❤️ Player {OwnerClientId} HP initialized to 20");
        }

        // 📍 Anchor the player to correct grid
        int index = (int)OwnerClientId;
        Transform anchor = SpawnAnchorRegistry.Instance.GetAnchor(index);
        if (anchor != null)
        {
            transform.SetPositionAndRotation(anchor.position + new Vector3(0, 1f, 0), Quaternion.Euler(0f, 180f, 0f));
        }

        // 🧠 Register player
        AllPlayers[OwnerClientId] = this;

        // ✅ Local player reference
        if (IsOwner)
        {
            SetLocalPlayer(this);
        }

        // 🛒 Server-only: spawn shop state
        if (IsServer && playerShopStatePrefab != null)
        {
            GameObject shopGO = Instantiate(playerShopStatePrefab);
            NetworkObject netObj = shopGO.GetComponent<NetworkObject>();
            netObj.SpawnWithOwnership(OwnerClientId);

            ShopState = shopGO.GetComponent<PlayerShopState>();
            ShopState.Init(OwnerClientId, this);

            Debug.Log($"🛒 [Server] Spawned PlayerShopState for client {OwnerClientId}");
        }
        Debug.Log($"🧠 Registering player {OwnerClientId} (IsServer: {IsServer}, IsOwner: {IsOwner})");

        // ✅ Let systems know this player is fully ready
        OnAnyPlayerFullySpawned?.Invoke(this);
    }

    private void Start()
    {
        playerCamera = GetComponentInChildren<Camera>(true);

        if (playerCamera != null)
        {
            AllPlayerCameras[OwnerClientId] = playerCamera;
            playerCamera.enabled = IsOwner;
            playerCamera.gameObject.SetActive(IsOwner);
        }
    }

    public override void OnDestroy()
    {
        AllPlayerCameras.Remove(OwnerClientId);
        AllPlayers.Remove(OwnerClientId);
        base.OnDestroy();
    }

    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
    }

    public static PlayerNetworkState GetPlayerByClientId(ulong clientId)
    {
        return AllPlayers.TryGetValue(clientId, out var player) ? player : null;
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector3 position, Quaternion rotation)
    {
        if (!IsOwner) return;
        transform.SetPositionAndRotation(position, rotation);
    }

    public static void SetLocalPlayer(PlayerNetworkState instance)
    {
        if (LocalPlayer == null)
        {
            LocalPlayer = instance;
        }
    }

    [ServerRpc]
    public void SellHeroCardServerRpc(int heroId, int starLevel, ServerRpcParams rpcParams = default)
    {
        if (PlayerDeck == null || GoldManager == null)
        {
            Debug.LogWarning($"❌ Missing PlayerDeck or GoldManager on Player {OwnerClientId}");
            return;
        }

        // Attempt to find and remove the card
        HeroCardInstance cardToRemove = null;
        
        foreach (var card in PlayerDeck.cards)
        {
            if (card.baseHero.heroId == heroId && card.starLevel == starLevel)
            {
                cardToRemove = card;
                break;
            }
        }

        if (cardToRemove != null)
        {
            // ✅ Calculate refund BEFORE removing
            int baseCost = cardToRemove.baseHero != null ? cardToRemove.baseHero.cost : 1;
            int refund = starLevel switch
            {
                1 => baseCost,
                2 => baseCost * 3,
                3 => baseCost * 4,
                _ => baseCost
            };

            PlayerDeck.SellCard(cardToRemove);
            PlayerDeck.SyncDeckToClient(OwnerClientId);
            GoldManager.AddGold(refund);

            Debug.Log($"💰 Player {OwnerClientId} sold {cardToRemove.baseHero.heroName} (★{starLevel}) for {refund}g");
        }
        else
        {
            Debug.LogWarning($"⚠️ Player {OwnerClientId} attempted to sell a card not in deck: heroId {heroId} ★{starLevel}");
        }
    }
    public void SetSpectatorMode(bool enabled)
    {
        if (playerCamera != null)
        {
            playerCamera.enabled = enabled;
            playerCamera.gameObject.SetActive(enabled);
        }
        // Add more spectator behavior here if needed
        Debug.Log($"👁️ Player {OwnerClientId} set to spectator mode: {enabled}");
    }
    [ClientRpc]
    public void NotifyEliminatedClientRpc(ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;

        Debug.Log("👁️ Eliminated — entering spectator mode.");
        SetSpectatorMode(true);
        
    }
    [ServerRpc]
    public void ClaimRewardServerRpc(int amount)
    {
        GoldManager.AddGold(amount);
        Debug.Log($"🪙 [Server] Reward claimed: {amount}g for Player {OwnerClientId}");
    }




}
