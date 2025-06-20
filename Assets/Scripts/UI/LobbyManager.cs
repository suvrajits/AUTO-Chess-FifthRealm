using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using System.Collections;
using System.Linq; // Needed for .All()
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System;

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
   

    private Dictionary<string, GameObject> playerSlots = new();
    public List<PlayerStatus> ConnectedPlayers = new List<PlayerStatus>();
    private readonly Dictionary<ulong, GameObject> playerSlotInstances = new();
    private new bool IsHost => NetworkManager.Singleton.IsHost;

    private void Start()
    {
        
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public async Task<string> HostGameFromUI()
    {
        string joinCode = await RelayManager.Instance.CreateRelayHostAsync();
        if (!string.IsNullOrEmpty(joinCode))
        {
            lobbyPanel.SetActive(true);
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
                await Task.Delay(500); // wait for shutdown
            }

            var joinAllocation = await RelayManager.Instance.JoinRelayAsync(joinCode);

            Debug.Log("[LobbyManager] Starting client after successful join.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LobbyManager] Failed to join game: {ex.Message}");
            return false;
        }
    }


    public void ToggleReadyState()
    {
        Debug.Log("Toggling Ready State (simulate)");
        // TODO: Update your playerData, and UI visuals here.
    }

    public void StartGame()
    {
        if (!IsHost) return;
        HideLobbyPanelClientRpc();
        Debug.Log("[LobbyManager] Host starting game manually...");

        //Destroy(gameObject); // Clean transition

        //NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        lobbyPanel.SetActive(false);
        
        //GameObject instance = Instantiate(gameGridPrefab);
        //instance.GetComponent<NetworkObject>().Spawn(); // 👈 Important!
        

    }

    [ClientRpc]
    private void HideLobbyPanelClientRpc()
    {
        if (lobbyPanel != null && lobbyPanel.activeSelf)
        {
            Debug.Log("[LobbyManager] Hiding lobby panel via ClientRpc");
            lobbyPanel.SetActive(false);
        }
    }


    public void LeaveLobby()
    {
        Debug.Log("Leaving lobby...");
        lobbyPanel.SetActive(false);
        NetworkManager.Singleton.Shutdown();
    }

    public void AddPlayer(string playerId, bool isReady)
    {
        if (playerSlots.ContainsKey(playerId)) return;

        GameObject slot = Instantiate(playerSlotPrefab, playerListContent);
        slot.GetComponentInChildren<TMP_Text>().text = playerId + (isReady ? " ✅" : " ❌");
        playerSlots.Add(playerId, slot);
    }

    public void UpdateReadyState(string playerId, bool isReady)
    {
        if (playerSlots.TryGetValue(playerId, out var slot))
        {
            slot.GetComponentInChildren<TMP_Text>().text = playerId + (isReady ? " ✅" : " ❌");
        }
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
        AddPlayerSlotUI(player); // updates UI
    }

    public void UpdatePlayerReadyState(PlayerStatus player, bool isReady)
    {
        RefreshPlayerSlotUI(player);

        if (IsHost && AllPlayersReady())
        {
            StartCoroutine(BeginGameAfterDelay(1.5f));
        }
    }

    private bool AllPlayersReady()
    {
        return ConnectedPlayers.Count > 0 && ConnectedPlayers.All(p => p.IsReady.Value);
    }
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
        {
            UpdateReadyVisual(slot, player.IsReady.Value);
        }
    }

    private void UpdateReadyVisual(GameObject slot, bool isReady)
    {
        var img = slot.GetComponentInChildren<UnityEngine.UI.Image>();
        if (img != null)
            img.color = isReady ? Color.green : Color.red;
    }
    private IEnumerator BeginGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!IsHost)
        {
            Debug.LogWarning("[LobbyManager] Only host should initiate scene load.");
            yield break;
        }

        Debug.Log("[LobbyManager] All players ready. Cleaning up and loading GameScene...");

        Destroy(gameObject); // Cleanly remove LobbyManager and its UI
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }

}
