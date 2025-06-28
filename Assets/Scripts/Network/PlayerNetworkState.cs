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
    private void Awake()
    {
        GoldManager = GetComponent<GoldManager>();

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
}
