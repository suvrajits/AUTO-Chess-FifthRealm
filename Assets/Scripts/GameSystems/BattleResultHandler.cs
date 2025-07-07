
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class BattleResultHandler : MonoBehaviour
{
    public static BattleResultHandler Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void ApplyPostBattleDamage(List<HeroUnit> winningTeam, List<ulong> losingClientIds)
    {
        int totalDamage = 0;

        foreach (var unit in winningTeam)
        {
            if (unit == null || !unit.IsAlive) continue;

            totalDamage += unit.starLevel switch
            {
                1 => 1,
                2 => 2,
                3 => 5,
                _ => 1
            };

            if (BattleGroundManager.Instance.originalTileMemory.TryGetValue(unit, out var homeTile) && homeTile != null)
            {
                unit.SnapToTileY(homeTile);
                unit.SetCombatState(false);
            }
        }

        foreach (var clientId in losingClientIds)
        {
            var player = PlayerNetworkState.GetPlayerByClientId(clientId);
            player?.HealthManager?.ApplyServerDamage(totalDamage);
        }

        Debug.Log($"📤 Losing players: {string.Join(", ", losingClientIds)}");

        StartCoroutine(DelayedVictoryCheckCoroutine());
    }

    private IEnumerator DelayedVictoryCheckCoroutine()
    {
        yield return new WaitForSeconds(0.2f);
        VictoryManager.Instance?.CheckForVictory();
    }
}
