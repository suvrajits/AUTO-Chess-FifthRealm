using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using Unity.Services.Core;

public class MatchmakingManager : MonoBehaviour
{
    public async Task TryAutoMatchPlayerAsync()
    {
        // ✅ Initialize Unity Services
        if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // ✅ Query options with filter for available lobbies
        QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
        {
            Filters = new List<QueryFilter>
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            }
        };

        QueryResponse response;
        try
        {
            response = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"❌ Failed to query lobbies: {e.Message}");
            return;
        }

        List<Lobby> openLobbies = response.Results;

        // ✅ Sort manually if needed (fallback logic only)
        Lobby targetLobby = openLobbies
            .Where(lobby => lobby.AvailableSlots >= 1 && lobby.Data.ContainsKey("joinCode"))
            .FirstOrDefault(); // Already filtered and sorted by Unity

        if (targetLobby != null)
        {
            Debug.Log($"✅ Joining lobby: {targetLobby.Id}");
            await LobbyManager.Instance.JoinLobbyAsync(targetLobby);

            string joinCode = targetLobby.Data["joinCode"].Value;
            await RelayManager.Instance.JoinRelayAsync(joinCode);
        }
        else
        {
            Debug.Log("🆕 No suitable lobby found. Creating one...");
            await LobbyManager.Instance.CreateLobbyAsync(); // Includes Relay allocation
            await RelayManager.Instance.HostRelayAsync();
        }

        MultiplayerUI.Instance.ShowLobbyPanel();
    }
}
