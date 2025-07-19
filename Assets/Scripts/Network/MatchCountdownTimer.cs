using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MatchCountdownTimer : NetworkBehaviour
{
    [SerializeField] private float countdownDuration = 20f;

    private NetworkVariable<float> countdownTime = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Coroutine countdownRoutine;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCountdown();
        }

        CountdownUI.Instance?.BindCountdown(countdownTime);
    }

    private void StartCountdown()
    {
        if (countdownRoutine == null)
        {
            countdownRoutine = StartCoroutine(CountdownRoutine());
        }
    }

    private IEnumerator CountdownRoutine()
    {
        countdownTime.Value = countdownDuration;

        while (countdownTime.Value > 0f)
        {
            yield return new WaitForSeconds(1f);
            countdownTime.Value -= 1f;

            if (NetworkManager.Singleton.ConnectedClients.Count >= 4)
            {
                Debug.Log("🎯 Full lobby. Starting match early.");
                MatchStartManager.Instance.StartMatch();
                yield break;
            }
        }

        int realPlayers = NetworkManager.Singleton.ConnectedClients.Count;

        if (realPlayers >= 2)
        {
            Debug.Log("⏱ Countdown expired with 2+ players. Starting match.");
            MatchStartManager.Instance.StartMatch();
        }
        else
        {
            Debug.Log("⏱ Countdown expired with < 2 players. Injecting bot and starting match.");
            MatchStartManager.Instance.StartMatchWithBot();
        }
    }
}
