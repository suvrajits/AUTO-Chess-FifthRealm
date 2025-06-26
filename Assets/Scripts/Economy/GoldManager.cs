using UnityEngine;
using Unity.Netcode;

public class GoldManager : NetworkBehaviour
{
    [SerializeField] private int maxGold = 50;
    public NetworkVariable<int> CurrentGold = new NetworkVariable<int>(10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void AddGold(int amount)
    {
        if (!IsServer) return;

        int newGold = Mathf.Min(CurrentGold.Value + amount, maxGold);
        CurrentGold.Value = newGold;
    }

    public bool TrySpendGold(int amount)
    {
        if (!IsServer) return false;

        if (CurrentGold.Value >= amount)
        {
            CurrentGold.Value -= amount;
            return true;
        }
        return false;
    }
}
