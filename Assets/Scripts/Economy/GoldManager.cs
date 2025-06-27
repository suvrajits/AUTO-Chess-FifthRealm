using UnityEngine;
using Unity.Netcode;

public class GoldManager : NetworkBehaviour
{
    [SerializeField] private int maxGold = 50;

    public NetworkVariable<int> CurrentGold = new NetworkVariable<int>(
        10,
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
            Debug.LogWarning("❌ TrySpendGold called on non-server");
            return false;
        }

        if (CurrentGold.Value >= amount)
        {
            CurrentGold.Value -= amount;
            Debug.Log($"💸 Spent {amount} gold. Remaining: {CurrentGold.Value}");
            return true;
        }

        Debug.Log("❌ Not enough gold to spend.");
        return false;
    }
}
