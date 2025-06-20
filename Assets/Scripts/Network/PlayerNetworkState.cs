using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerNetworkState : NetworkBehaviour
{
    public static Dictionary<ulong, Camera> AllPlayerCameras = new();

    private Camera playerCamera;
    private GridManager gridManager;
    public static Dictionary<ulong, GridManager> AllPlayerGrids = new();
  
    public override void OnNetworkSpawn()
    {
        // ✅ Only server should initialize grid
        if (!IsServer) return;

        // ✅ Get GridManager component from PlayerPrefab's child
        gridManager = GetComponentInChildren<GridManager>(true);
        if (gridManager == null)
        {
            Debug.LogError($"❌ GridManager missing on PlayerPrefab (ClientId: {OwnerClientId})");
            return;
        }

        // ✅ Calculate unique grid position based on spawn index
        int index = (int)OwnerClientId;
        int row = index / 4;
        int col = index % 4;
        float offset = (gridManager.gridSize + 2f) * gridManager.spacing;

        Vector3 baseOffset = new Vector3(0f, 0f, 10f);
        gridManager.transform.position = baseOffset;

        // ✅ Initialize grid tiles and assign ownership
        gridManager.Init(OwnerClientId);

        // ✅ Register in global player grid map
        if (!PlayerNetworkState.AllPlayerGrids.ContainsKey(OwnerClientId))
        {
            PlayerNetworkState.AllPlayerGrids.Add(OwnerClientId, gridManager);
        }
        else
        {
            PlayerNetworkState.AllPlayerGrids[OwnerClientId] = gridManager;
        }

        Debug.Log($"🧩 Grid initialized for Player {OwnerClientId} at {baseOffset}");

        // ✅ Center player above their own grid
        Vector3 center = gridManager.GetGridCenter();
        transform.SetPositionAndRotation(center + new Vector3(0f, 1f, 0f), Quaternion.Euler(0f, 180f, 0f));
    }


    private void Start()
    {
        playerCamera = GetComponentInChildren<Camera>(true);

        if (playerCamera != null)
        {
            AllPlayerCameras[OwnerClientId] = playerCamera;

            bool isLocal = IsOwner;
            playerCamera.enabled = isLocal;
            playerCamera.gameObject.SetActive(isLocal);
        }
    }

    public void HideLobbyPanelLocally()
    {
        if (LobbyManager.Instance?.lobbyPanel != null)
        {
            Debug.Log("[PlayerNetworkState] Hiding lobby panel locally...");
            LobbyManager.Instance.lobbyPanel.SetActive(false);
        }
    }

    public override void OnDestroy()
    {
        AllPlayerCameras.Remove(OwnerClientId);
        base.OnDestroy();
    }
}
