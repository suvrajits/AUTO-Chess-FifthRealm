using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class VictoryManager : NetworkBehaviour
{
    public static VictoryManager Instance;

    private void Awake() => Instance = this;

    public void CheckForVictory()
    {
        if (!IsServer) return;

        var alivePlayers = PlayerNetworkState.AllPlayers.Values
            .Where(p => !p.IsEliminated.Value && p.HealthManager != null && p.HealthManager.CurrentHealth.Value > 0)
            .ToList();

        Debug.Log($"🔍 [VictoryManager] Alive count: {alivePlayers.Count}");

        if (alivePlayers.Count == 1)
        {
            DeclareWinner(alivePlayers[0].OwnerClientId);
        }
        else if (alivePlayers.Count == 0)
        {
            Debug.LogWarning("⚠️ [VictoryManager] Draw or edge case — 0 players alive.");
        }
    }

    private void DeclareWinner(ulong winnerClientId)
    {
        Debug.Log($"🏆 [VictoryManager] Declaring Player {winnerClientId} as match winner");

        var winner = PlayerNetworkState.GetPlayerByClientId(winnerClientId);
        if (winner != null && RewardManager.Instance != null)
        {
            RewardManager.Instance.GrantMatchVictoryReward(winnerClientId);
        }
        else
        {
            Debug.LogWarning("❌ [VictoryManager] RewardManager.Instance or Winner is null.");
        }

        BroadcastVictoryClientRpc(winnerClientId);
    }


    [ClientRpc]
    private void BroadcastVictoryClientRpc(ulong winnerClientId)
    {
        bool isWinner = NetworkManager.Singleton.LocalClientId == winnerClientId;

        Debug.Log($"📣 Client {NetworkManager.Singleton.LocalClientId} received broadcast. Winner: {winnerClientId}");
        Debug.Log(isWinner ? "🎉 YOU WON!" : "❌ YOU LOST!");

        if (VictoryUIHandler.Instance != null)
            VictoryUIHandler.Instance.ShowResult(isWinner);
        else
            Debug.LogWarning("❌ VictoryUIHandler.Instance is null on client.");
    }

    // ✅ Hook from BattleResultHandler or BattleGroundManager
    public void DeclareRoundWin(ulong winnerClientId)
    {
        if (!IsServer) return;

        Debug.Log($"🟩 [VictoryManager] Player {winnerClientId} won the round");
        RewardManager.Instance.GrantRoundWinReward(winnerClientId);
    }
}
