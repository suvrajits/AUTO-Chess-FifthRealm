using UnityEngine;
using Unity.Netcode;

public class GoldManager : NetworkBehaviour
{
    [SerializeField] private int maxGold = 90;

    public NetworkVariable<int> CurrentGold = new NetworkVariable<int>(
        30,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public void AddGold(int amount)
    {
        if (!IsServer) return;

        int newGold = Mathf.Min(CurrentGold.Value + amount, maxGold);
        CurrentGold.Value = newGold;

        Debug.Log($"🪙 Gold added: {amount}. New total: {CurrentGold.Value}");
    }

    public bool TrySpendGold(int amount)
    {
        if (!IsServer)
        {
            Debug.LogWarning($"❌ [GoldManager] TrySpendGold called on non-server by client {OwnerClientId}");
            return false;
        }

        Debug.Log($"[GoldManager] TrySpendGold requested: {amount} | Current: {CurrentGold.Value} (ClientId: {OwnerClientId})");

        if (CurrentGold.Value >= amount)
        {
            CurrentGold.Value -= amount;
            Debug.Log($"💸 [GoldManager] Spent {amount} gold. Remaining: {CurrentGold.Value} (ClientId: {OwnerClientId})");
            return true;
        }

        Debug.Log($"❌ [GoldManager] Not enough gold to spend {amount}. Current: {CurrentGold.Value} (ClientId: {OwnerClientId})");
        return false;
    }

}
