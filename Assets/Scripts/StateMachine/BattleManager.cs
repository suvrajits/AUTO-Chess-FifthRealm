using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;


public enum GamePhase { Placement, Battle, Results }

public class BattleManager : NetworkBehaviour
{
    public static BattleManager Instance { get; private set; }

    public GamePhase CurrentPhase { get; private set; } = GamePhase.Placement;

    public List<HeroUnit> allUnits = new();
    public NetworkVariable<bool> battleStarted = new(false);
    public bool IsBattleOngoing => CurrentPhase == GamePhase.Battle;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterUnit(HeroUnit unit)
    {
        if (!IsServer) return;
        allUnits.Add(unit);
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartBattleServerRpc()
    {
        if (CurrentPhase != GamePhase.Placement) return;

        Debug.Log("⚔️ Battle Started!");

        CurrentPhase = GamePhase.Battle;
        battleStarted.Value = true;

        foreach (var unit in allUnits)
        {
            unit.BeginBattle(); // Calls logic in HeroUnit to start seeking targets
        }

        StartCoroutine(CheckForBattleEnd());
    }

    private IEnumerator<UnityEngine.WaitForSeconds> CheckForBattleEnd()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            bool p1Alive = allUnits.Exists(u => u.Faction == Faction.Player1 && u.IsAlive);
            bool p2Alive = allUnits.Exists(u => u.Faction == Faction.Player2 && u.IsAlive);

            if (!p1Alive || !p2Alive)
            {
                EndBattle(p1Alive, p2Alive);
                break;
            }
        }
    }

    private void EndBattle(bool p1Alive, bool p2Alive)
    {
        CurrentPhase = GamePhase.Results;

        if (p1Alive && !p2Alive)
            Debug.Log(" Player 1 Wins!");
        else if (p2Alive && !p1Alive)
            Debug.Log(" Player 2 Wins!");
        else
            Debug.Log(" Draw!");

        // Reset or restart logic can go here
    }
    public HeroUnit FindNearestEnemy(HeroUnit seeker)
    {
        var allHeroes = Object.FindObjectsByType<HeroUnit>(FindObjectsSortMode.None);

        return allHeroes
            .Where(unit => unit != seeker && unit.Faction != seeker.Faction && unit.IsAlive)
            .OrderBy(unit => Vector3.Distance(seeker.transform.position, unit.transform.position))
            .FirstOrDefault();
    }


}
