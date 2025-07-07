using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class RewardManager : NetworkBehaviour
{
    public static RewardManager Instance { get; private set; }

    [SerializeField] private RewardDefinition rewardDefinition;

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

    public void GrantMatchVictoryReward(ulong winnerClientId)
    {
        var player = PlayerNetworkState.GetPlayerByClientId(winnerClientId);
        if (player == null) return;

        int reward = rewardDefinition.matchVictoryReward;
        player.GoldManager.AddGold(reward);

        Debug.Log($"[RewardManager] 🏆 Match Victory → {reward}g → Player {winnerClientId}");

        ShowRewardToClient(winnerClientId, "🏆 Match Victory", reward);
    }

    public void GrantRoundWinReward(ulong winnerClientId)
    {
        var player = PlayerNetworkState.GetPlayerByClientId(winnerClientId);
        if (player == null) return;

        int reward = rewardDefinition.roundWinReward;
        player.GoldManager.AddGold(reward);

        Debug.Log($"[RewardManager] 🌀 Round Win → {reward}g → Player {winnerClientId}");

        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new List<ulong> { winnerClientId }
            }
        };

        SendRewardToClientClientRpc("🌀 Round Victory", reward, rpcParams);
    }

    public void GrantRoundLossReward(ulong loserClientId)
    {
        var player = PlayerNetworkState.GetPlayerByClientId(loserClientId);
        if (player == null) return;

        int reward = rewardDefinition.unitSurvivalReward; // Use this as participation reward
        player.GoldManager.AddGold(reward);

        Debug.Log($"[RewardManager] 🎗️ Round Loss → {reward}g → Player {loserClientId}");

        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new List<ulong> { loserClientId }
            }
        };

        SendRewardToClientClientRpc("🎗️ Round Participation", reward, rpcParams);
    }

    [ClientRpc]
    public void SendRewardToClientClientRpc(string label, int amount, ClientRpcParams rpcParams = default)
    {
        if (RewardUI.Instance == null)
        {
            Debug.LogWarning($"⚠️ RewardUI.Instance is null on Client {NetworkManager.Singleton.LocalClientId}");
            return;
        }

        Debug.Log($"🪙 [Client {NetworkManager.Singleton.LocalClientId}] Queuing reward: {label} +{amount}g");
        RewardUI.Instance.QueueDelayedReward(label, amount, 2.6f); // 2.5s = round result delay
    }

    private void ShowRewardToClient(ulong clientId, string label, int amount)
    {
        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new List<ulong> { clientId }
            }
        };

        SendRewardToClientClientRpc(label, amount, rpcParams);
    }
}
