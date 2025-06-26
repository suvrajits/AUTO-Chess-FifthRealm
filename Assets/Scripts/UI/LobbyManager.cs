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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    [Header("UI References")]
    public GameObject lobbyPanel;
    public GameObject heroSelectionPanel;
    public TMP_Text joinCodeText;
    public Transform playerListContent;
    public GameObject playerSlotPrefab;
    public Button startGameButton;
    public Button readyButton;
    public Button leaveButton;

    [SerializeField] private GameObject gameGridPrefab;

    private Dictionary<string, GameObject> playerSlots = new();
    private readonly Dictionary<ulong, GameObject> playerSlotInstances = new();

    public List<PlayerStatus> ConnectedPlayers = new List<PlayerStatus>();
    private Lobby currentLobby;
    private float heartbeatInterval = 15f;
    private new bool IsHost => NetworkManager.Singleton.IsHost;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public async Task<string> HostGameFromUI()
    {
        await UnityServicesManager.InitUnityServicesIfNeeded();

        string joinCode = await RelayManager.Instance.CreateRelayHostAsync();
        if (!string.IsNullOrEmpty(joinCode))
        {
            string playerName = AuthenticationService.Instance?.PlayerName;
            if (string.IsNullOrEmpty(playerName))
                playerName = $"Player_{UnityEngine.Random.Range(1000, 9999)}";

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
            heroSelectionPanel.SetActive(false);
            joinCodeText.text = $"Join Code: {joinCode}";

            return joinCode;
        }

        return null;
    }

    public async Task<bool> JoinGameFromUI(string joinCode)
    {
        try
        {
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning("[LobbyManager] Netcode already active. Shutting down...");
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

    public void ToggleReadyState()
    {
        Debug.Log("Toggling Ready State (simulate)");
    }

    public void StartGame()
    {
        if (!IsHost) return;

        Debug.Log("[LobbyManager] Host starting game...");
        lobbyPanel.SetActive(false);
        heroSelectionPanel.SetActive(true);
        HideLobbyPanelClientRpc();

        GameObject instance = Instantiate(gameGridPrefab);
        instance.GetComponent<NetworkObject>().Spawn();
    }

    [ClientRpc]
    private void HideLobbyPanelClientRpc()
    {
        if (lobbyPanel != null && lobbyPanel.activeSelf)
            lobbyPanel.SetActive(false);

        heroSelectionPanel.SetActive(true);
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

    public void AddPlayer(string playerId, bool isReady)
    {
        if (playerSlots.ContainsKey(playerId)) return;

        GameObject slot = Instantiate(playerSlotPrefab, playerListContent);
        slot.GetComponentInChildren<TMP_Text>().text = playerId + (isReady ? " ✅" : " ❌");
        playerSlots[playerId] = slot;
    }

    public void UpdateReadyState(string playerId, bool isReady)
    {
        if (playerSlots.TryGetValue(playerId, out var slot))
            slot.GetComponentInChildren<TMP_Text>().text = playerId + (isReady ? " ✅" : " ❌");
    }

    public void ClearLobbyUI()
    {
        foreach (var obj in playerSlots.Values)
            Destroy(obj);

        playerSlots.Clear();
    }

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
        TMP_Text nameText = slot.GetComponentInChildren<TMP_Text>();
        nameText.text = $"Player {player.OwnerClientId}";

        slot.name = $"PlayerSlot_{player.OwnerClientId}";
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

    private IEnumerator BeginGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!IsHost) yield break;

        Debug.Log("[LobbyManager] Starting game scene...");
        Destroy(gameObject);
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
}
