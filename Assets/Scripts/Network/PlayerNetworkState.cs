using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerNetworkState : NetworkBehaviour
{
    // 🧠 Global registry for camera switching
    public static Dictionary<ulong, Camera> AllPlayerCameras = new();

    private Camera playerCamera;

    public override void OnNetworkSpawn()
    {
        // Define offsets for each player's grid
        const int gridSize = 8;
        const float tileSize = 1f; // Adjust if your tile size differs
        Vector3 baseOffset = new Vector3(0f, 1f, 0f); // Slight Y lift

        // Calculate horizontal offset so each player's grid is side by side
        int playerIndex = (int)OwnerClientId;
        float xOffset = playerIndex * gridSize * tileSize;

        // Place the player near the center of their grid (adjust Z to your needs)
        Vector3 spawnPos = new Vector3(xOffset + (gridSize / 2f), 1f, gridSize + 1f);
        Quaternion spawnRot = Quaternion.Euler(0f, 180f, 0f); // Look toward center (optional)

        transform.SetPositionAndRotation(spawnPos + baseOffset, spawnRot);
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

        base.OnDestroy();
    }
}
