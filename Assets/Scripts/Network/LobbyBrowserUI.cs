using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Core;

public class LobbyBrowserUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject lobbyListEntryPrefab;
    public Transform lobbyListContainer;

    private async void OnEnable()
    {
        await RefreshLobbies(); // Auto-refresh when shown
    }

    /// <summary>
    /// Public wrapper for Button OnClick in Unity Inspector.
    /// </summary>
    public void OnClickRefreshLobbies()
    {
        _ = RefreshLobbies(); // fire-and-forget for Inspector binding
    }

    /// <summary>
    /// Fetches and displays all joinable lobbies from Unity Lobby.
    /// </summary>
    public async Task RefreshLobbies()
    {
        Debug.Log("🔄 RefreshLobbies called.");

        try
        {
            await UnityServicesManager.InitUnityServicesIfNeeded();

            var options = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };

            var response = await LobbyService.Instance.QueryLobbiesAsync(options);
            Debug.Log($"📋 Found {response.Results.Count} lobbies.");
            ClearList();

            foreach (var lobby in response.Results)
            {
                if (!lobby.Data.ContainsKey("JoinCode"))
                {
                    Debug.LogWarning($"⚠️ Skipping lobby '{lobby.Name}' – No JoinCode.");
                    continue;
                }

                GameObject entry = Instantiate(lobbyListEntryPrefab, lobbyListContainer);
                entry.transform.localScale = Vector3.one;

                TMP_Text text = entry.GetComponentInChildren<TMP_Text>();
                if (text != null)
                    text.text = $"{lobby.Name} ({lobby.Players.Count}/{lobby.MaxPlayers})";

                string joinCode = lobby.Data["JoinCode"].Value;

                Button button = entry.GetComponentInChildren<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() =>
                    {
                        Debug.Log($"👉 Join clicked: {joinCode}");
                        _ = MultiplayerUI.Instance.JoinGameViaCode(joinCode); // async fire-and-forget
                    });
                }
                else
                {
                    Debug.LogWarning("⚠️ No Button found on LobbyListEntry prefab.");
                }
            }

            if (response.Results.Count == 0)
                Debug.Log("ℹ️ No active lobbies found.");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError("❌ LobbyServiceException: " + ex.Message);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ General error: " + ex.Message);
        }
    }

    private void ClearList()
    {
        foreach (Transform child in lobbyListContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
