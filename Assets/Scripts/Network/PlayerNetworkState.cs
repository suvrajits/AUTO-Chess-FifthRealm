using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerNetworkState : NetworkBehaviour
{
    // 🧠 Global registry
    public static Dictionary<ulong, Camera> AllPlayerCameras = new();
    public static Dictionary<ulong, PlayerNetworkState> AllPlayers = new();

    private Camera playerCamera;
    public GoldManager GoldManager { get; private set; }
    public static PlayerNetworkState LocalPlayer { get; private set; }

    private void Awake()
    {
        GoldManager = GetComponent<GoldManager>();
    }

    public override void OnNetworkSpawn()
    {
        // 📍 Position at spawn anchor
        int index = (int)OwnerClientId;
        Transform anchor = SpawnAnchorRegistry.Instance.GetAnchor(index);
        if (anchor != null)
        {
            transform.SetPositionAndRotation(anchor.position + new Vector3(0, 1f, 0), Quaternion.Euler(0f, 180f, 0f));
        }
        else
        {
            Debug.LogWarning($"❌ No spawn anchor found for player {index}");
        }

        // 🧠 Track all players
        if (!AllPlayers.ContainsKey(OwnerClientId))
            AllPlayers.Add(OwnerClientId, this);

        // ✅ Safe local player registration
        if (IsOwner)
            SetLocalPlayer(this);
    }

    private void Start()
    {
        playerCamera = GetComponentInChildren<Camera>(true);

        if (playerCamera != null)
        {
            if (!AllPlayerCameras.ContainsKey(OwnerClientId))
                AllPlayerCameras.Add(OwnerClientId, playerCamera);

            playerCamera.enabled = IsOwner;
            playerCamera.gameObject.SetActive(IsOwner);

            if (IsOwner)
                Debug.Log($"📸 Enabled local camera for Player {OwnerClientId}");
        }
        else
        {
            Debug.LogWarning($"❌ Camera not found on Player prefab (Client {OwnerClientId})");
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
        Debug.Log($"🚀 Teleported Player {OwnerClientId} to {position}");
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
        Debug.Log($"🚀 [ClientRpc] Teleported local player {OwnerClientId} to {position}");
    }

    // ✅ Public safe assignment
    public static void SetLocalPlayer(PlayerNetworkState instance)
    {
        if (LocalPlayer == null)
        {
            LocalPlayer = instance;
            Debug.Log($"🧠 LocalPlayer registered: {instance.OwnerClientId}");
        }
    }
}
