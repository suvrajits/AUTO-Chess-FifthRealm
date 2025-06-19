using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;

public enum GamePhase { Placement, Battle, Results }

public class BattleManager : NetworkBehaviour
{
    public static BattleManager Instance { get; private set; }

    public GamePhase CurrentPhase { get; private set; } = GamePhase.Placement;
    
    [System.NonSerialized]
    public List<HeroUnit> allUnits = new();
    public NetworkVariable<bool> battleStarted = new(false);
    public bool IsBattleOngoing => CurrentPhase == GamePhase.Battle;

    [Header("Battle Start Settings")]
    [SerializeField] private float battleStartDelay = 10f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log($"🕒 Starting Battle Countdown for {battleStartDelay} sec...");
            StartCoroutine(BattleStartCountdown());
        }
    }

    private IEnumerator BattleStartCountdown()
    {
        float currentTime = battleStartDelay;

        while (currentTime > 0f)
        {
            Debug.Log($"⏳ Battle begins in {Mathf.CeilToInt(currentTime)}s");
            yield return new WaitForSeconds(1f);
            currentTime -= 1f;
        }

        Debug.Log("🔥 Starting Battle!");
        StartBattle();  // ✅ Now just calls a local method
    }

    private void StartBattle()
    {
        if (CurrentPhase != GamePhase.Placement) return;

        Debug.Log("⚔️ Battle Started!");

        CurrentPhase = GamePhase.Battle;
        battleStarted.Value = true;

        foreach (var unit in FindObjectsByType<HeroUnit>(FindObjectsSortMode.None))
        {
            unit.BeginBattle();
        }

        StartCoroutine(CheckForBattleEnd());
    }

    private IEnumerator CheckForBattleEnd()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            int totalUnits = allUnits.Count;
            int p1Count = allUnits.Count(u => u.Faction == Faction.Player1);
            int p2Count = allUnits.Count(u => u.Faction == Faction.Player2);
            int neutralCount = allUnits.Count(u => u.Faction == Faction.Neutral);

            int p1Alive = allUnits.Count(u => u.Faction == Faction.Player1 && u.IsAlive);
            int p2Alive = allUnits.Count(u => u.Faction == Faction.Player2 && u.IsAlive);

            Debug.Log($" Unit Stats → Total: {totalUnits} | P1: {p1Alive}/{p1Count} | P2: {p2Alive}/{p2Count} | Neutral: {neutralCount}");

            if ((p1Alive == 0 || p2Alive == 0) && (p1Alive + p2Alive > 0)) // only trigger if someone is actually alive
            {
                EndBattle(p1Alive > 0, p2Alive > 0);
                break;
            }
        }
    }


    private void EndBattle(bool p1Alive, bool p2Alive)
    {
        CurrentPhase = GamePhase.Results;

        string result;
        if (p1Alive && !p2Alive)
            result = "Player 1 Wins!";
        else if (p2Alive && !p1Alive)
            result = "Player 2 Wins!";
        else
            result = "Draw!";

        Debug.Log(" Battle Over: " + result);

        // Freeze all units
        foreach (var unit in allUnits)
        {
            unit.StopAllCoroutines();
            unit.enabled = false;
            var rb = unit.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }

        // Show result on both clients
        ShowVictoryUIClientRpc(result);
    }

    [ClientRpc]
    private void ShowVictoryUIClientRpc(string result)
    {
        var ui = Object.FindFirstObjectByType<VictoryUIManager>();
        if (ui != null)
        {
            ui.ShowResult(result);
        }
    }

    public HeroUnit FindNearestEnemy(HeroUnit seeker)
    {
        return allUnits
            .Where(unit => unit != seeker && unit.Faction != seeker.Faction && unit.IsAlive)
            .OrderBy(unit => Vector3.Distance(seeker.transform.position, unit.transform.position))
            .FirstOrDefault();
    }

    public void RegisterUnit(HeroUnit unit, Faction faction)
    {
        if (!IsServer) return;

        unit.SetFaction(faction); // ensure it's server-side
        allUnits.Add(unit);
        Debug.Log($" Registered unit: {unit.name} for {unit.Faction}");
    }
    public void UnregisterUnit(HeroUnit unit)
    {
        if (allUnits.Contains(unit))
        {
            allUnits.Remove(unit);
            Debug.Log($" Unregistered unit: {unit.name}");
        }
    }
}
