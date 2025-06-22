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
                unit.PerformCombatTick(); // This should contain AI logic
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

        Debug.Log($"🏆 Battle ended! Winner: {(teamAAlive ? "Team A" : teamBAlive ? "Team B" : "Draw")}");

        BattleGroundManager.Instance.OnBattleEnded(); // Teleport survivors back
    }

    public bool IsBattleOver()
    {
        return !isBattleOngoing;
    }

    public List<HeroUnit> GetAllAliveUnits()
    {
        return teamAUnits.Concat(teamBUnits)
            .Where(u => u != null && u.IsAlive)
            .ToList();
    }
    public HeroUnit FindNearestEnemy(HeroUnit requester)
    {
        var enemies = GetAllAliveUnits()
            .Where(u => u != null && u != requester && u.OwnerClientId != requester.OwnerClientId)
            .ToList();

        if (enemies.Count == 0) return null;

        HeroUnit closest = null;
        float minDistance = float.MaxValue;

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(requester.transform.position, enemy.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = enemy;
            }
        }

        return closest;
    }
    public void UnregisterUnit(HeroUnit unit)
    {
        if (unit == null) return;

        if (teamAUnits.Contains(unit))
            teamAUnits.Remove(unit);

        if (teamBUnits.Contains(unit))
            teamBUnits.Remove(unit);
    }
    public void RegisterUnit(HeroUnit unit, Faction faction)
    {
        if (unit == null) return;
        unit.SetFaction(faction);

        if (unit.OwnerClientId % 2 == 0) // You can customize this logic
        {
            if (!teamAUnits.Contains(unit))
                teamAUnits.Add(unit);
        }
        else
        {
            if (!teamBUnits.Contains(unit))
                teamBUnits.Add(unit);
        }

        Debug.Log($"🟢 Registered unit: {unit.heroData.heroName} (ClientId: {unit.OwnerClientId})");
    }


}
