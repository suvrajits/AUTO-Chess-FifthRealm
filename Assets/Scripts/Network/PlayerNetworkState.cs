using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerNetworkState : NetworkBehaviour
{
    // 🧠 Global registry for camera switching
    public static Dictionary<ulong, Camera> AllPlayerCameras = new();
    public static Dictionary<ulong, PlayerNetworkState> AllPlayers = new();

    private Camera playerCamera;

    public override void OnNetworkSpawn()
    {
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

        if (!AllPlayers.ContainsKey(OwnerClientId))
            AllPlayers.Add(OwnerClientId, this);
    }

    private void Start()
    {
        // Search for camera even if inactive
        playerCamera = GetComponentInChildren<Camera>(true);

        if (playerCamera != null)
        {
            if (!AllPlayerCameras.ContainsKey(OwnerClientId))
                AllPlayerCameras.Add(OwnerClientId, playerCamera);

            if (IsOwner)
            {
                playerCamera.enabled = true;
                playerCamera.gameObject.SetActive(true);
                Debug.Log($"📸 Enabled local camera for Player {OwnerClientId}");
            }
            else
            {
                playerCamera.enabled = false;
                playerCamera.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning($"❌ Camera not found on Player prefab (Client {OwnerClientId})");
        }
    }

    public override void OnDestroy()
    {
        if (AllPlayerCameras.ContainsKey(OwnerClientId))
        {
            AllPlayerCameras.Remove(OwnerClientId);
            Debug.Log($"🧹 Camera removed from registry for Player {OwnerClientId}");
        }

        if (AllPlayers.ContainsKey(OwnerClientId))
        {
            AllPlayers.Remove(OwnerClientId);
        }

        base.OnDestroy();
    }

    // ✅ Called by BattleGroundManager to move the Player prefab
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        Debug.Log($"🚀 Teleported Player {OwnerClientId} to {position}");
    }

    // ✅ For safe lookups
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

}
