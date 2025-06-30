using System.Collections.Generic;
using UnityEngine;

public class BattleResultHandler : MonoBehaviour
{
    public static BattleResultHandler Instance;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Applies post-battle damage based on surviving enemy units.
    /// </summary>
    public void ApplyPostBattleDamage(List<HeroUnit> winningTeam, List<ulong> losingClientIds)
    {
        int totalDamage = 0;

        // ✅ 1. Calculate total damage based on living units in winning team
        foreach (var unit in winningTeam)
        {
            if (unit == null || !unit.IsAlive) continue;

            totalDamage += unit.starLevel switch
            {
                1 => 1,
                2 => 2,
                3 => 4,
                _ => 1
            };

            // ✅ Return to original tile
            if (BattleGroundManager.Instance.originalTileMemory.TryGetValue(unit, out var homeTile) && homeTile != null)
            {
                unit.SnapToTileY(homeTile);
                unit.SetCombatState(false);
            }
        }

        // ✅ 2. Apply damage to losing players
        foreach (var clientId in losingClientIds)
        {
            var player = PlayerNetworkState.GetPlayerByClientId(clientId);
            player?.HealthManager?.ApplyDamageServerRpc(totalDamage);
        }

        Debug.Log($"📤 Losing players: {string.Join(", ", losingClientIds)}");
    }

}
