using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public enum GamePhase
{
    Waiting,
    Placement,
    Battle,
    Results
}

public class BattleManager : NetworkBehaviour
{
    public static BattleManager Instance;

    private List<HeroUnit> teamAUnits = new();
    private List<HeroUnit> teamBUnits = new();
    private bool isBattleOngoing = false;

    public GamePhase CurrentPhase { get; private set; } = GamePhase.Waiting;

    private void Awake()
    {
        Instance = this;
    }

    public void BeginCombat(List<HeroUnit> teamA, List<HeroUnit> teamB)
    {
        teamAUnits = teamA.Where(u => u != null && u.IsAlive).ToList();
        teamBUnits = teamB.Where(u => u != null && u.IsAlive).ToList();

        isBattleOngoing = true;
        CurrentPhase = GamePhase.Battle;

        foreach (var unit in teamAUnits.Concat(teamBUnits))
        {
            var ai = unit.GetComponent<AICombatController>();
            ai?.SetBattleMode(true);
            
        }

        Debug.Log("⚔️ Battle started!");
    }

    private void Update()
    {
        if (!IsServer || !isBattleOngoing) return;

        RunCombatFrame();
        CheckVictoryCondition();
    }

    private void RunCombatFrame()
    {
        foreach (var unit in teamAUnits.Concat(teamBUnits))
        {
            if (unit != null && unit.IsAlive)
            {
                var ai = unit.GetComponent<AICombatController>();
                ai?.TickAI();
            }
        }
    }

    private void CheckVictoryCondition()
    {
        bool teamAAlive = teamAUnits.Any(u => u != null && u.IsAlive);
        bool teamBAlive = teamBUnits.Any(u => u != null && u.IsAlive);

        if (teamAAlive && teamBAlive) return;

        isBattleOngoing = false;
        CurrentPhase = GamePhase.Results;

        string winner = teamAAlive ? "Team A" : teamBAlive ? "Team B" : "Draw";
        Debug.Log($"🏆 Battle ended! Winner: {winner}");

        EndBattle();
    }

    private void EndBattle()
    {
        // ✅ Disable AI and animations for all tracked units (alive only — dead handled separately)
        foreach (var unit in teamAUnits.Concat(teamBUnits))
        {
            if (unit == null) continue;

            var ai = unit.GetComponent<AICombatController>();
            ai?.SetBattleMode(false);
            unit.StopAllCombatCoroutines(); 

            if (unit.IsAlive)
            {
                unit.AnimatorHandler?.SetRunning(false);
                unit.AnimatorHandler?.PlayIdle();
            }
        }

        // ✅ Delegate full post-battle recovery to BattleGroundManager
        BattleGroundManager.Instance.OnBattleEnded();
    }


    public bool IsBattleOver() => !isBattleOngoing;

    public List<HeroUnit> GetAllAliveUnits()
    {
        return teamAUnits.Concat(teamBUnits).Where(u => u != null && u.IsAlive).ToList();
    }

    public HeroUnit FindNearestEnemy(HeroUnit requester)
    {
        var enemies = GetAllAliveUnits().Where(u => u.OwnerClientId != requester.OwnerClientId).ToList();

        if (enemies.Count == 0) return null;

        return enemies.OrderBy(e => Vector3.Distance(requester.transform.position, e.transform.position)).FirstOrDefault();
    }

    public void UnregisterUnit(HeroUnit unit)
    {
        teamAUnits.Remove(unit);
        teamBUnits.Remove(unit);
    }

    public void RegisterUnit(HeroUnit unit, Faction faction)
    {
        if (unit == null) return;

        unit.SetFaction(faction);
        var list = unit.OwnerClientId % 2 == 0 ? teamAUnits : teamBUnits;
        if (!list.Contains(unit)) list.Add(unit);

        Debug.Log($"🟢 Registered unit: {unit.heroData.heroName} (ClientId: {unit.OwnerClientId})");
    }

    public void RemoveAllUnitsForClient(ulong clientId)
{
    var allUnits = teamAUnits.Concat(teamBUnits)
        .Where(u => u != null && u.OwnerClientId == clientId)
        .ToList();

    foreach (var unit in allUnits)
    {
        UnregisterUnit(unit);
        unit.GetComponent<NetworkObject>()?.Despawn(true);
        unit.currentTile?.RemoveUnit();
    }

    Debug.Log($"🗑️ All units removed for client {clientId}");
}

}
