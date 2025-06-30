using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class EliminationManager : NetworkBehaviour
{
    public static EliminationManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void EliminatePlayer(ulong clientId)
    {
        if (!IsServer)
        {
            Debug.LogWarning($"⚠️ [EliminationManager] Not server — aborting EliminatePlayer({clientId})");
            return;
        }

        if (!PlayerNetworkState.AllPlayers.TryGetValue(clientId, out var player))
        {
            Debug.LogWarning($"❌ [EliminationManager] Player {clientId} not found.");
            return;
        }

        if (player.IsEliminated.Value)
        {
            Debug.Log($"ℹ️ [EliminationManager] Player {clientId} already eliminated.");
            return;
        }

        Debug.Log($"💀 [EliminationManager] Eliminating Player {clientId}");

        // Cleanup
        BattleManager.Instance?.RemoveAllUnitsForClient(clientId);
        player.PlayerDeck?.ClearDeck();
        player.ShopState?.DisableShop();

        // State
        player.IsEliminated.Value = true;
        player.SetSpectatorMode(true);

        // 🔁 Notify only that client
        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };
        player.NotifyEliminatedClientRpc(rpcParams);

        // 🏁 Check victory after short delay
        StartCoroutine(DelayedVictoryCheck());
    }

    private IEnumerator DelayedVictoryCheck()
    {
        yield return new WaitForSeconds(0.3f);
        VictoryManager.Instance?.CheckForVictory();
    }
}
