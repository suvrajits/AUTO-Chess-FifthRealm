using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance;

    [Header("Settings")]
    public float roundCountdown = 3f;
    public float postBattleDelay = 2f;

    private int roundNumber = 0;
    private bool roundInProgress = false;
    private List<ulong> waitingPlayers = new();
    private List<List<ulong>> currentMatches = new();
    private int roundIndex = 0;

    public NetworkVariable<int> CurrentRound = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private ulong? idlePlayerThisRound = null;

    private void Awake()
    {
        Instance = this;
    }

    private bool CanStartNextBattle()
    {
        var alivePlayers = PlayerNetworkState.AllPlayers.Values.Count(p => p.IsAlive);
        return alivePlayers >= 2;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            Debug.Log("🧠 RoundManager OnNetworkSpawn → Starting RoundLoop");
            StartCoroutine(RoundLoop());
        }
    }

    private IEnumerator RoundLoop()
    {
        while (true)
        {
            yield return new WaitUntil(() =>
                BattleManager.Instance != null &&
                BattleManager.Instance.CurrentPhase == GamePhase.Waiting &&
                !BattleGroundManager.Instance.IsBattleInProgress() &&
                PlayerNetworkState.AllPlayers.Count(p => p.Value != null && p.Value.IsAlive) >= 2
            );

            roundInProgress = true;
            roundNumber++;
            CurrentRound.Value = roundNumber;

            Debug.Log($"🔁 Starting Round {roundNumber}");
            yield return StartCoroutine(PreBattlePhase());

            BuildMatchups();

            foreach (var match in currentMatches)
            {
                StartMatch(match);
            }

            yield return new WaitUntil(() =>
                BattleManager.Instance != null &&
                BattleManager.Instance.CurrentPhase == GamePhase.Results
            );

            
            yield return StartCoroutine(PostBattlePhase());
            ShopManager.Instance?.RerollAllShopsFree();
            // ✅ Reset phase to waiting before next round
            BattleManager.Instance.SetPhase(GamePhase.Waiting);
            roundInProgress = false;
        }
    }

    private IEnumerator PreBattlePhase()
    {
        Debug.Log("⏳ PreBattlePhase: Setting placement phase");
        BattleManager.Instance.SetPhase(GamePhase.Placement);
        yield return new WaitForSeconds(roundCountdown);
    }

    private IEnumerator PostBattlePhase()
    {
        Debug.Log("🪙 Battle ended. Distributing rewards...");
        PostBattleRewardSystem.Instance.GrantGold();
        
        yield return new WaitForSeconds(postBattleDelay);
    }

    private void BuildMatchups()
    {
        waitingPlayers = PlayerNetworkState.AllPlayers.Keys
            .Where(id => PlayerNetworkState.AllPlayers[id].IsAlive)
            .ToList();

        currentMatches.Clear();

        waitingPlayers = waitingPlayers.OrderBy(x => UnityEngine.Random.value).ToList();

        for (int i = 0; i + 1 < waitingPlayers.Count; i += 4)
        {
            var group = waitingPlayers.Skip(i).Take(4).ToList();
            if (group.Count == 4)
            {
                currentMatches.Add(group);
            }
        }

        int rem = waitingPlayers.Count % 4;
        if (rem == 3)
        {
            var lastThree = waitingPlayers.Skip(waitingPlayers.Count - 3).ToList();
            currentMatches.Add(lastThree.Take(2).ToList());
            idlePlayerThisRound = lastThree[2];
        }
        else if (rem == 2)
        {
            currentMatches.Add(waitingPlayers.Skip(waitingPlayers.Count - 2).ToList());
            idlePlayerThisRound = null;
        }
        else
        {
            idlePlayerThisRound = null;
        }
    }

    private void StartMatch(List<ulong> clientIds)
    {
        var allUnits = new List<HeroUnit>();

        foreach (var clientId in clientIds)
        {
            var player = PlayerNetworkState.GetPlayerByClientId(clientId);
            if (player != null)
            {
                allUnits.AddRange(player.GetAllAliveHeroUnits());
            }
        }

        var teamA = new List<HeroUnit>();
        var teamB = new List<HeroUnit>();

        if (clientIds.Count == 4)
        {
            var shuffled = clientIds.OrderBy(_ => UnityEngine.Random.value).ToList();
            var teamAPlayers = shuffled.Take(2).ToList();
            var teamBPlayers = shuffled.Skip(2).ToList();

            foreach (var unit in allUnits)
            {
                if (teamAPlayers.Contains(unit.OwnerClientId))
                    teamA.Add(unit);
                else
                    teamB.Add(unit);
            }
        }
        else if (clientIds.Count == 2)
        {
            foreach (var unit in allUnits)
            {
                if (unit.OwnerClientId == clientIds[0])
                    teamA.Add(unit);
                else
                    teamB.Add(unit);
            }
        }

        if (teamA.Count == 0 || teamB.Count == 0)
        {
            Debug.LogWarning("⚠️ Invalid match created. Skipping StartCustomBattle.");
            return;
        }

        Debug.Log($"🏟️ Match Starting: TeamA={teamA.Count}, TeamB={teamB.Count}");
        BattleGroundManager.Instance.StartCustomBattle(teamA, teamB);
    }

    public void ScheduleNextRoundWithDelay(float delaySeconds)
    {
        StartCoroutine(ScheduleNextRoundCoroutine(delaySeconds));
    }

    private IEnumerator ScheduleNextRoundCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        roundInProgress = false;
    }

    public void StartNewRound()
    {
        if (roundInProgress) return;
        roundInProgress = true;
        roundNumber++;
        CurrentRound.Value = roundNumber;

        Debug.Log($"🔁 Round {roundNumber} started");
        BuildMatchups();
    }
}
