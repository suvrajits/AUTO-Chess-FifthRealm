using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance;

    [Header("Settings")]
    public float roundCountdown = 3f;
    public float postBattleDelay = 2f;

    private int roundNumber = 0;
    private bool roundInProgress = false;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(RoundLoop());
        }
    }

    private IEnumerator RoundLoop()
    {
        while (true) // Later you can add match-end condition
        {
            yield return new WaitUntil(() => !roundInProgress);

            roundInProgress = true;
            roundNumber++;

            Debug.Log($" Starting Round {roundNumber}");

            yield return StartCoroutine(PreBattlePhase());

            BattleGroundManager.Instance.StartBattleServerRpc();

            yield return new WaitUntil(() => BattleManager.Instance.IsBattleOver());

            yield return StartCoroutine(PostBattlePhase());

            roundInProgress = false;
        }
    }

    private IEnumerator PreBattlePhase()
    {
        Debug.Log("Countdown before battle begins...");
        yield return new WaitForSeconds(roundCountdown);
    }

    private IEnumerator PostBattlePhase()
    {
        Debug.Log(" Battle ended. Distributing rewards...");
        PostBattleRewardSystem.Instance.GrantGold();
        yield return new WaitForSeconds(postBattleDelay);
    }
}
