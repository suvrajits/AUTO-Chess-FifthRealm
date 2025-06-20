using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerNetworkState : NetworkBehaviour
{
    public static Dictionary<ulong, Camera> AllPlayerCameras = new();
    public static Dictionary<ulong, GridManager> AllPlayerGrids = new();

    private Camera playerCamera;
    private GridManager gridManager;

    [HideInInspector] public Transform[] gridSpawnAnchors; // Assigned from BootstrapGridAssigner
    [SerializeField] private GameObject gridManagerPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        var gridObj = Instantiate(gridManagerPrefab);
        var gridNetObj = gridObj.GetComponent<NetworkObject>();
        gridNetObj.SpawnWithOwnership(OwnerClientId); // 🧠 Ensure ownership

        gridManager = gridObj.GetComponent<GridManager>();
        Vector3 spawnPos = GetGridSpawnPosition();
        gridManager.transform.position = spawnPos;
        gridManager.Init(OwnerClientId);

        AllPlayerGrids[OwnerClientId] = gridManager;

        Vector3 center = gridManager.GetGridCenter();
        transform.SetPositionAndRotation(center + new Vector3(0f, -0.8f, 3.25f), Quaternion.Euler(0f, 180f, 0f));

        Debug.Log($"🧩 Grid initialized for Player {OwnerClientId} at {spawnPos}");

        RegisterGridClientRpc(OwnerClientId, gridManager.NetworkObjectId);
    }

    [ClientRpc]
    private void RegisterGridClientRpc(ulong ownerId, ulong gridNetId)
    {
        if (IsServer) return; // server already assigned

        var gridObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[gridNetId];
        var grid = gridObj.GetComponent<GridManager>();

        if (grid != null && !AllPlayerGrids.ContainsKey(ownerId))
        {
            AllPlayerGrids[ownerId] = grid;
            Debug.Log($"✅ Client registered GridManager for Player {ownerId}");
        }
    }

    private Vector3 GetGridSpawnPosition()
    {
        int index = (int)OwnerClientId;
        if (gridSpawnAnchors != null && index < gridSpawnAnchors.Length && gridSpawnAnchors[index] != null)
        {
            return gridSpawnAnchors[index].position;
        }

        Debug.LogWarning($"⚠️ Grid spawn anchor not set for Player {OwnerClientId}. Using fallback position.");
        return new Vector3(index * 10f, 0, 0);
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
