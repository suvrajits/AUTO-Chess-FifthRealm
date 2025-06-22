using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    private string cachedJoinCode;
    private Allocation cachedAllocation;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private bool IsNetworkRunning()
    {
        return NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer;
    }

    public async Task<string> CreateRelayAsync(int maxPlayers = 8)
    {
        if (!string.IsNullOrEmpty(cachedJoinCode))
        {
            Debug.Log("[RelayManager] Returning cached join code: " + cachedJoinCode);
            return cachedJoinCode;
        }

        try
        {
            cachedAllocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            cachedJoinCode = await RelayService.Instance.GetJoinCodeAsync(cachedAllocation.AllocationId);

            var relayServerData = new RelayServerData(cachedAllocation, "dtls");
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(relayServerData);

            Debug.Log($"Relay setup complete with Join Code: {cachedJoinCode}");
            Debug.Log($"[Netcode State] Before Host Start — IsHost: {NetworkManager.Singleton.IsHost}, IsClient: {NetworkManager.Singleton.IsClient}, IsServer: {NetworkManager.Singleton.IsServer}");

            if (!IsNetworkRunning())
            {
                NetworkManager.Singleton.StartHost();
                Debug.Log("[RelayManager] Host started.");
            }
            else
            {
                Debug.LogWarning("[RelayManager] Network already running — host not started.");
            }

            return cachedJoinCode;
        }
        catch (Exception e)
        {
            Debug.LogError("[RelayManager] Failed to create Relay: " + e.Message);
            return null;
        }
    }

    public async Task<JoinAllocation> JoinRelayAsync(string joinCode)
    {
        await EnsureNetcodeShutdownAsync();

        try
        {
            Debug.Log("[RelayManager] Attempting to join with code: " + joinCode);
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var relayServerData = new RelayServerData(joinAllocation, "dtls");
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(relayServerData);

            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.StartClient();
                Debug.Log("[RelayManager] Client started.");
            }
            else
            {
                Debug.LogWarning("[RelayManager] Client already running.");
            }

            return joinAllocation;
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError("[RelayManager] Failed to join relay: " + ex.Message);
            throw;
        }
    }

    private async Task EnsureNetcodeShutdownAsync()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.Log("[RelayManager] Shutting down NetworkManager...");
            NetworkManager.Singleton.Shutdown();

            // Give time for complete shutdown and scene unbinding
            await Task.Delay(500);
        }
    }

    public async Task<string> CreateRelayHostAsync(int maxPlayers = 8)
    {
        await UnityServicesManager.InitUnityServicesIfNeeded();
        await EnsureNetcodeShutdownAsync();

        if (!string.IsNullOrEmpty(cachedJoinCode))
        {
            Debug.Log("[RelayManager] Returning cached join code: " + cachedJoinCode);
            return cachedJoinCode;
        }

        try
        {
            cachedAllocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            cachedJoinCode = await RelayService.Instance.GetJoinCodeAsync(cachedAllocation.AllocationId);

            var relayServerData = new RelayServerData(cachedAllocation, "dtls");
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
            Debug.Log("[RelayManager] Host started with join code: " + cachedJoinCode);

            return cachedJoinCode;
        }
        catch (Exception e)
        {
            Debug.LogError("[RelayManager] Failed to create Relay Host: " + e.Message);
            return null;
        }
    }

    public void ResetRelay()
    {
        cachedJoinCode = null;
        cachedAllocation = null;

        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("[RelayManager] Shutdown complete.");
        }

        Debug.Log("[RelayManager] Relay reset complete.");
    }
}
