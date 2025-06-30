using UnityEngine;
using System.Linq;

public class VictoryManager : MonoBehaviour
{
    public static VictoryManager Instance;

    private void Awake() => Instance = this;

    public void CheckForVictory()
    {
        var alivePlayers = PlayerNetworkState.AllPlayers.Values
            .Where(p => !p.IsEliminated.Value && p.GetComponent<PlayerHealthManager>().CurrentHealth.Value > 0)
            .ToList();

        if (alivePlayers.Count == 1)
        {
            DeclareWinner(alivePlayers[0]);
        }
    }

    private void DeclareWinner(PlayerNetworkState winner)
    {
        Debug.Log($"🏆 Player {winner.OwnerClientId} is the winner!");
        // TODO: Trigger end screen + summary UI
    }
}
