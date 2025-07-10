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

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(PhaseLoop());
        }
    }

    private IEnumerator PhaseLoop()
    {
        while (true)
        {
            // 1️⃣ PLACEMENT PHASE
            BattleManager.Instance.SetPhase(GamePhase.Placement);
            Debug.Log("🧩 Phase: Placement");
            RoundManager.Instance.StartNewRound(); // builds matchups
            yield return new WaitForSeconds(placementDuration);

            // 2️⃣ BATTLE PREP
            Debug.Log("⚙️ Preparing battle...");
            yield return new WaitForSeconds(battlePreparationTime);
            BattleGroundManager.Instance.StartBattleServerRpc();

            // 3️⃣ WAIT FOR COMBAT TO END
            yield return new WaitUntil(() => BattleManager.Instance.CurrentPhase == GamePhase.Results);
            Debug.Log("🏁 Combat ended");

            // 4️⃣ POST-BATTLE PHASE
            yield return new WaitForSeconds(postBattleDelay);
        }
    }
}