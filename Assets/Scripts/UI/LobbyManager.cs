using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    [Header("UI References")]
    public GameObject lobbyPanel;
    public TMP_Text joinCodeText;
    public Transform playerListContent;
    public GameObject playerSlotPrefab;
    public Button startGameButton;
    public Button readyButton;
    public Button leaveButton;

    private readonly Dictionary<ulong, GameObject> playerSlotInstances = new();
    public List<PlayerStatus> ConnectedPlayers = new List<PlayerStatus>();

    private Lobby currentLobby;
    private float heartbeatInterval = 15f;
    private bool IsHost => NetworkManager.Singleton.IsHost;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    #region Lobby Hosting / Joining

    public async Task<string> HostGameFromUI()
    {
        await UnityServicesManager.InitUnityServicesIfNeeded();

        string joinCode = await RelayManager.Instance.CreateRelayHostAsync();
        if (!string.IsNullOrEmpty(joinCode))
        {
            string playerName = AuthenticationService.Instance?.PlayerName ?? $"Player_{UnityEngine.Random.Range(1000, 9999)}";

            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) },
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "Standard") },
                    { "Region", new DataObject(DataObject.VisibilityOptions.Public, "auto") },
                    { "HostName", new DataObject(DataObject.VisibilityOptions.Public, playerName) }
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync("BattleLobby", 8, options);
            Debug.Log($"🟢 Lobby created: {currentLobby.Id} - JoinCode: {joinCode}");

            StartHeartbeatLoop();
            lobbyPanel.SetActive(true);
            joinCodeText.text = $"Join Code: {joinCode}";
            return joinCode;
        }

        return null;
    }

    public async Task<bool> JoinGameFromUI(string joinCode)
    {
        try
        {
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning("[LobbyManager] Shutting down existing session...");
                NetworkManager.Singleton.Shutdown();
                await Task.Delay(500);
            }

            var joinAllocation = await RelayManager.Instance.JoinRelayAsync(joinCode);
            Debug.Log("[LobbyManager] Joined relay. Starting client...");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Join failed: {ex.Message}");
            return false;
        }
    }

    private async void StartHeartbeatLoop()
    {
        while (currentLobby != null)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
                Debug.Log($"💓 Heartbeat sent to Lobby: {currentLobby.Id}");
            }
            catch (LobbyServiceException ex)
            {
                Debug.LogWarning($"⚠️ Heartbeat failed: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(heartbeatInterval));
        }
    }

    #endregion

    #region UI Events

    public void StartGame()
    {
        if (!IsHost) return;
        Debug.Log("[LobbyManager] Host starting game...");
        StartCoroutine(BeginGameAfterDelay(1.5f));
    }

    public void LeaveLobby()
    {
        Debug.Log("👋 Leaving lobby...");
        lobbyPanel.SetActive(false);

        if (currentLobby != null)
        {
            try { LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id); } catch { }
            currentLobby = null;
        }

        NetworkManager.Singleton.Shutdown();
    }

    private IEnumerator BeginGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!IsHost) yield break;

        Debug.Log("[LobbyManager] Loading GameScene...");
        _ = SceneTransitionManager.LoadSceneAsync("GameScene");
    }

    #endregion

    #region Player Slot Management

    public void RegisterLocalPlayer(PlayerStatus player)
    {
        ConnectedPlayers.Add(player);
        AddPlayerSlotUI(player);
    }

    public void UpdatePlayerReadyState(PlayerStatus player, bool isReady)
    {
        RefreshPlayerSlotUI(player);
        if (IsHost && AllPlayersReady())
            StartCoroutine(BeginGameAfterDelay(1.5f));
    }

    private bool AllPlayersReady() => ConnectedPlayers.Count > 0 && ConnectedPlayers.All(p => p.IsReady.Value);

    private void AddPlayerSlotUI(PlayerStatus player)
    {
        GameObject slot = Instantiate(playerSlotPrefab, playerListContent);
        slot.name = $"PlayerSlot_{player.OwnerClientId}";

        TMP_Text nameText = slot.GetComponentInChildren<TMP_Text>();
        nameText.text = $"Player {player.OwnerClientId}";

        playerSlotInstances[player.OwnerClientId] = slot;
        UpdateReadyVisual(slot, player.IsReady.Value);
    }

    private void RefreshPlayerSlotUI(PlayerStatus player)
    {
        if (playerSlotInstances.TryGetValue(player.OwnerClientId, out GameObject slot))
            UpdateReadyVisual(slot, player.IsReady.Value);
    }

    private void UpdateReadyVisual(GameObject slot, bool isReady)
    {
        var img = slot.GetComponentInChildren<Image>();
        if (img != null)
            img.color = isReady ? Color.green : Color.red;
    }

    public void ClearLobbyUI()
    {
        foreach (var obj in playerSlotInstances.Values)
            Destroy(obj);

        playerSlotInstances.Clear();
    }

    #endregion
}
