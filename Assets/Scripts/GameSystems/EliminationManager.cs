using UnityEngine;

public class EliminationManager : MonoBehaviour
{
    public static EliminationManager Instance;

    private void Awake() => Instance = this;

    public void EliminatePlayer(ulong clientId)
    {
        Debug.Log($"💀 Eliminating player {clientId}");

        if (!PlayerNetworkState.AllPlayers.TryGetValue(clientId, out var player))
            return;

        // Despawn all units
        BattleManager.Instance.RemoveAllUnitsForClient(clientId);

        // Clear deck
        player.PlayerDeck?.ClearDeck();

        // Disable shop
        player.ShopState?.DisableShop();

        // Mark eliminated
        player.IsEliminated.Value = true;

        // Spectator mode toggle
        player.SetSpectatorMode(true);

        VictoryManager.Instance.CheckForVictory();
    }
}
