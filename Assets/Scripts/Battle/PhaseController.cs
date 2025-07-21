using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PhaseController : NetworkBehaviour
{
    public static PhaseController Instance;

    [Header("Phase Timing")]
    public float placementDuration = 60f;
    public float battlePreparationTime = 3f;
    public float postBattleDelay = 4f;
    private float placementTimer = 0f;
    private Coroutine countdownCoroutine;
    private NetworkVariable<float> syncedPhaseTimer = new NetworkVariable<float>(
    0f,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);
    private void Awake()
    {
        Instance = this;
    }


    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            syncedPhaseTimer.OnValueChanged += (oldVal, newVal) =>
            {
                RoundHUDUI.Instance?.UpdatePhaseTimerText($"{newVal:F0}s");
            };
        }
    }

    public void StartPhaseLoop()
    {
         if (IsServer)
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(5f);
        Debug.Log("🔁 PhaseController: Starting round loop...");
        StartCoroutine(PhaseLoop());
    }

    private IEnumerator PhaseLoop()
    {
        while (true)
        {
            // 1️⃣ PLACEMENT PHASE
            BattleManager.Instance.SetPhase(GamePhase.Placement);
            Debug.Log("🧩 Phase: Placement");
            RoundManager.Instance.StartNewRound(); // builds matchups
            StartPhaseCountdown(placementDuration);
            yield return new WaitForSeconds(placementDuration);

            // 2️⃣ BATTLE PREP
            Debug.Log("⚙️ Preparing battle...");
            StartPhaseCountdown(battlePreparationTime);
            yield return new WaitForSeconds(battlePreparationTime);
            BattleGroundManager.Instance.StartBattleServerRpc();

            // 3️⃣ WAIT FOR COMBAT TO END
            yield return new WaitUntil(() => BattleManager.Instance.CurrentPhase == GamePhase.Results);
            Debug.Log("🏁 Combat ended");

            // 4️⃣ POST-BATTLE PHASE
            StartPhaseCountdown(postBattleDelay);
            yield return new WaitForSeconds(postBattleDelay);
        }
    }

    public void StartPhaseCountdown(float duration)
    {
        if (!IsServer) return;

        syncedPhaseTimer.Value = duration;

        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        while (syncedPhaseTimer.Value > 0f)
        {
            yield return new WaitForSeconds(1f);
            syncedPhaseTimer.Value -= 1f;
        }

        syncedPhaseTimer.Value = 0f;
        countdownCoroutine = null;
    }
}