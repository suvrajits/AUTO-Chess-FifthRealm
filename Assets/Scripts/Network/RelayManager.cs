
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
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    private string cachedJoinCode;
    private Allocation cachedAllocation;

    private float joinCodeLifetime = 540f; // 9 minutes
    private float joinCodeTimer = 0f;
    private Allocation allocation;

    private string currentJoinCode;
    public string GetJoinCode() => currentJoinCode;

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

    private void Update()
    {
        if (cachedAllocation != null && NetworkManager.Singleton.IsHost)
        {
            joinCodeTimer += Time.deltaTime;

            if (joinCodeTimer >= joinCodeLifetime)
            {
                Debug.LogWarning("🔁 Join code expired. Reallocating new Relay session...");
                _ = RefreshRelayJoinCodeAsync();
            }
        }
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

            if (!IsNetworkRunning())
            {
                NetworkManager.Singleton.StartHost();
                Debug.Log("[RelayManager] Host started.");
            }

            joinCodeTimer = 0f;
            return cachedJoinCode;
        }
        catch (Exception e)
        {
            Debug.LogError("[RelayManager] Failed to create Relay: " + e.Message);
            return null;
        }
    }

    public async Task<string> CreateRelayHostAsync(int maxPlayers = 8)
    {
        await UnityServicesManager.InitUnityServicesIfNeeded();
        await EnsureNetcodeShutdownAsync();
        return await CreateRelayAsync(maxPlayers);
    }

    private async Task EnsureNetcodeShutdownAsync()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            await Task.Delay(500);
        }
    }

    private async Task RefreshRelayJoinCodeAsync()
    {
        ResetRelay();
        string newJoinCode = await CreateRelayHostAsync();
        Debug.Log($"✅ New Join Code: {newJoinCode}");
        joinCodeTimer = 0f;

        MultiplayerUI.Instance?.UpdateJoinCodeUI(newJoinCode);
    }

    public async Task<JoinAllocation> JoinRelayAsync(string joinCode)
    {
        await EnsureNetcodeShutdownAsync();

        try
        {
            Debug.Log($"🌐 Joining relay with code: {joinCode}");

            await UnityServicesManager.InitUnityServicesIfNeeded();

            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            var relayServerData = new RelayServerData(joinAllocation, "dtls");

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(relayServerData);

            // 🧠 Register callback BEFORE starting
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;

            NetworkManager.Singleton.StartClient();

            Debug.Log("✅ Relay join requested. Awaiting spawn...");

            return joinAllocation;
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError($"❌ Relay join failed: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Unexpected join error: {ex.Message}");
            throw;
        }
    }


    public void ResetRelay()
    {
        cachedJoinCode = null;
        cachedAllocation = null;

        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        Debug.Log("[RelayManager] Relay reset complete.");
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"✅ [RelayManager] OnClientConnected: {clientId}");

        if (NetworkManager.Singleton.IsClient && clientId == NetworkManager.Singleton.LocalClientId)
        {
            MultiplayerUI.Instance?.OnClientConnectedConfirmed();
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsClient && NetworkManager.Singleton.LocalClientId == clientId)
        {
            Debug.Log($"✅ [RelayManager] Client connected successfully. LocalClientId = {clientId}");

            if (PlayerNetworkState.AllPlayers.TryGetValue(clientId, out var player))
            {
                PlayerNetworkState.SetLocalPlayer(player);
                Debug.Log("🧠 LocalPlayer assigned after Relay join.");
            }
            else
            {
                Debug.LogWarning("⚠️ Local player not found in AllPlayers after join.");
            }

            // Optional: Trigger success UI callback
            MultiplayerUI.Instance?.OnClientConnectedConfirmed();
        }
    }
     public async Task HostRelayAsync()
    {
        allocation = await RelayService.Instance.CreateAllocationAsync(4);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        await Lobbies.Instance.UpdateLobbyAsync(
            LobbyManager.Instance.CurrentLobby.Id,
            new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "joinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                }
            });

        Debug.Log($"🔗 Hosted Relay. JoinCode: {joinCode}");
    }



}
