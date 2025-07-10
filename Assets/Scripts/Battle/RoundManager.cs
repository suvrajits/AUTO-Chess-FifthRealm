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
    private List<List<ulong>> currentMatches = new(); // Each match = list of clientIds
    private int roundIndex = 0;
    public NetworkVariable<int> CurrentRound = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private ulong? idlePlayerThisRound = null;
    private void Awake()
    {
        Instance = this;
    }
    
    private bool CanStartNextBattle()
    {
        // ✅ You may customize this based on your game state
        var alivePlayers = PlayerNetworkState.AllPlayers.Values.Count(p => p.IsAlive);
        return alivePlayers >= 2;
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
        while (true)
        {
            yield return new WaitUntil(() => !roundInProgress);

            roundInProgress = true;
            roundNumber++;
            CurrentRound.Value = roundNumber;

            // 🔥 ADD THIS LINE HERE
            BattleManager.Instance.SetPhase(GamePhase.Placement);

            Debug.Log($"🔁 Starting Round {roundNumber}");
            yield return StartCoroutine(PreBattlePhase());

            BuildMatchups();

            foreach (var match in currentMatches)
            {
                StartMatch(match);
            }

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
    
    private void BuildMatchups()
    {
        waitingPlayers = PlayerNetworkState.AllPlayers.Keys
            .Where(id => PlayerNetworkState.AllPlayers[id].IsAlive)
            .ToList();

        currentMatches.Clear();

        // Shuffle player list
        waitingPlayers = waitingPlayers.OrderBy(x => UnityEngine.Random.value).ToList();

        for (int i = 0; i + 1 < waitingPlayers.Count; i += 4)
        {
            var group = waitingPlayers.Skip(i).Take(4).ToList();
            if (group.Count == 4)
            {
                currentMatches.Add(group);
            }
        }

        // If 3 remaining, make a 1v1 + 1 idle
        int rem = waitingPlayers.Count % 4;
        if (rem == 3)
        {
            var lastThree = waitingPlayers.Skip(waitingPlayers.Count - 3).ToList();
            currentMatches.Add(lastThree.Take(2).ToList());
            idlePlayerThisRound = lastThree[2]; // ✅ track who is sitting out
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

        // Assign teams: if 4 players, 2v2; if 2 players, 1v1
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

        BattleGroundManager.Instance.StartCustomBattle(teamA, teamB);
    }
    public void ScheduleNextRoundWithDelay(float delaySeconds)
    {
        StartCoroutine(ScheduleNextRoundCoroutine(delaySeconds));
    }

    private IEnumerator ScheduleNextRoundCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        roundInProgress = false; // allows the main loop to start the next round
    }
    public void StartNewRound()
    {
        roundInProgress = true;
        roundNumber++;
        CurrentRound.Value = roundNumber;

        Debug.Log($"🔁 Round {roundNumber} started");
        BuildMatchups();
    }


}
