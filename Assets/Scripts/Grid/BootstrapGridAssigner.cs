using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class BootstrapGridAssigner : MonoBehaviour
{
    [SerializeField] private Transform[] gridSpawnAnchors;

    private IEnumerator Start()
    {
        // Wait for server and players to spawn
        yield return new WaitUntil(() => NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsListening);

        var players = Object.FindObjectsByType<PlayerNetworkState>(FindObjectsSortMode.None);

        foreach (var player in players)
        {
            player.gridSpawnAnchors = gridSpawnAnchors;
        }

        Debug.Log("✅ Grid anchors assigned to all PlayerNetworkState objects.");
    }
}
