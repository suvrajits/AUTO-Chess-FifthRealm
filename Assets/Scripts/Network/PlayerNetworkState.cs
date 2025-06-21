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
