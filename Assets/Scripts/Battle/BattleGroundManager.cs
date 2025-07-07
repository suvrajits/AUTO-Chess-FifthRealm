using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BattleGroundManager : NetworkBehaviour
{
    public static BattleGroundManager Instance;

    [Header("Grid Setup")]
    public GameObject battleTilePrefab;
    public Transform battleArenaAnchor;
    public int gridSizeX = 8;
    public int gridSizeY = 8;
    private GridTile[,] battleGrid;

    [Header("Unified Player View")]
    public Transform sharedSpectatorAnchor;

    private Dictionary<ulong, Vector3> originalPlayerPositions = new();
    private Dictionary<ulong, Quaternion> originalPlayerRotations = new();

    [Header("Battle Units")]
    private List<HeroUnit> teamAUnits = new();
    private List<HeroUnit> teamBUnits = new();
    public Dictionary<HeroUnit, GridTile> originalTileMemory = new();

    [Header("State")]
    private bool battleInProgress = false;
    private List<HeroUnit> allBattleParticipants = new();


    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CreateBattleArena();
        }
    }

    private void CreateBattleArena()
    {
        battleGrid = new GridTile[gridSizeX, gridSizeY];

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 pos = battleArenaAnchor.position + new Vector3(x, 0, y);
                GameObject tileObj = Instantiate(battleTilePrefab, pos, Quaternion.identity);

                if (!tileObj.TryGetComponent(out GridTile tile))
                {
                    Debug.LogError($"❌ Battle tile prefab missing GridTile component at ({x},{y})");
                    continue;
                }

                tile.Init(new Vector2Int(x, y), 0, Color.gray);
                tile.GetComponent<NetworkObject>().Spawn();
                battleGrid[x, y] = tile;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartBattleServerRpc()
    {
        if (battleInProgress) return;

        battleInProgress = true;

        PickTeams(); // Split units into teamAUnits and teamBUnits

        // ✅ Track ALL participants for post-battle revival
        allBattleParticipants.Clear();
        allBattleParticipants.AddRange(teamAUnits);
        allBattleParticipants.AddRange(teamBUnits);

        // ✅ Save their original tiles for later teleport
        foreach (var unit in allBattleParticipants)
        {
            if (unit != null && unit.currentTile != null)
            {
                originalTileMemory[unit] = unit.currentTile;
            }
        }

        TeleportToBattleGrid(teamAUnits, true);
        TeleportToBattleGrid(teamBUnits, false);
        TeleportPlayersToSpectatorSpot();

        Invoke(nameof(InvokeBattleStart), 2f);
    }

    private void InvokeBattleStart()
    {
        BattleManager.Instance.BeginCombat(teamAUnits, teamBUnits);
    }


    private void PickTeams()
    {
        List<HeroUnit> allUnits = BattleManager.Instance.GetAllAliveUnits();
        var players = allUnits.GroupBy(u => u.OwnerClientId).ToList();

        // Fallback safety
        if (players.Count < 1)
        {
            Debug.LogWarning("⚠️ No players found in PickTeams!");
            teamAUnits.Clear();
            teamBUnits.Clear();
            return;
        }

        if (players.Count >= 4)
        {
            teamAUnits = players[0].Concat(players[1]).ToList();
            teamBUnits = players[2].Concat(players[3]).ToList();
        }
        else if (players.Count >= 2)
        {
            teamAUnits = players[0].ToList();
            teamBUnits = players[1].ToList();
        }
        else
        {
            // Single player fallback
            teamAUnits = players[0].ToList();
            teamBUnits.Clear();
        }
    }


    private void TeleportToBattleGrid(List<HeroUnit> units, bool isTeamA)
    {
        if (units == null || units.Count == 0) return;

        var assignments = FormationPlanner.GenerateFormation(units, battleGrid, isTeamA);

        foreach (var (unit, tile) in assignments)
        {
            if (unit == null || tile == null) continue;

            if (unit.currentTile != null)
                originalTileMemory[unit] = unit.currentTile;

            unit.SnapToTileY(tile);
            unit.SetCombatState(true);
        }
    }


    private void TeleportPlayersToSpectatorSpot()
    {
        HashSet<ulong> battleParticipants = new();
        teamAUnits.ForEach(u => battleParticipants.Add(u.OwnerClientId));
        teamBUnits.ForEach(u => battleParticipants.Add(u.OwnerClientId));

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            ulong clientId = client.ClientId;
            if (!battleParticipants.Contains(clientId)) continue;

            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;

            var player = playerObj.GetComponent<PlayerNetworkState>();
            if (player == null) continue;

            originalPlayerPositions[clientId] = player.transform.position;
            originalPlayerRotations[clientId] = player.transform.rotation;

            player.TeleportClientRpc(sharedSpectatorAnchor.position, sharedSpectatorAnchor.rotation);

        }
    }


    public void OnBattleEnded()
    {
        if (!IsServer) return;

        battleInProgress = false;

        Debug.Log("🛠️ OnBattleEnded: Restoring all battle participants...");

        // Determine winning and losing teams
        List<HeroUnit> teamAAlive = teamAUnits.Where(u => u != null && u.IsAlive).ToList();
        List<HeroUnit> teamBAlive = teamBUnits.Where(u => u != null && u.IsAlive).ToList();

        bool teamAWin = teamAAlive.Count > 0 && teamBAlive.Count == 0;
        bool teamBWin = teamBAlive.Count > 0 && teamAAlive.Count == 0;

        List<HeroUnit> winningTeam = teamAWin ? teamAAlive : teamBWin ? teamBAlive : new List<HeroUnit>();
        List<ulong> losingClientIds = teamAWin
            ? teamBUnits.Select(u => u.OwnerClientId).Distinct().ToList()
            : teamBWin
                ? teamAUnits.Select(u => u.OwnerClientId).Distinct().ToList()
                : new List<ulong>(); // draw

        // 💥 Damage first
        BattleResultHandler.Instance.ApplyPostBattleDamage(winningTeam, losingClientIds);

        // ✅ Restore all units (alive or dead)
        foreach (var unit in allBattleParticipants)
        {
            if (unit == null) continue;

            if (originalTileMemory.TryGetValue(unit, out var homeTile) && homeTile != null)
            {
                if (unit.IsAlive)
                {
                    unit.SnapToTileY(homeTile);
                    unit.RestoreHealthToMax();
                    unit.SetCombatState(false);
                    unit.transform.rotation = Quaternion.Euler(0, 0, 0);
                }
                else
                {
                    unit.ReviveAndReturnToTile(homeTile);
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Missing home tile for unit: {unit.name}");
            }
        }

        // 🧍 Return players
        TeleportPlayersBackToOriginalPositions();

        // ✅ Give gold to winners
        foreach (var clientId in winningTeam.Select(u => u.OwnerClientId).Distinct())
        {
            Debug.Log($"🌀 Granting round win reward to Client {clientId}");
            RewardManager.Instance?.GrantRoundWinReward(clientId);
        }

        // ✅ Give participation reward to losers
        foreach (var clientId in losingClientIds)
        {
            Debug.Log($"🎗️ Granting round loss reward to Client {clientId}");
            RewardManager.Instance?.GrantRoundLossReward(clientId);
        }

        // 🧾 Show round result UI
        ShowRoundResultClientRpc(winningTeam.Select(u => u.OwnerClientId).Distinct().ToArray());

        // 🧼 Cleanup
        CleanupArena();
        originalTileMemory.Clear();
        teamAUnits.Clear();
        teamBUnits.Clear();
        allBattleParticipants.Clear();

        foreach (var player in PlayerNetworkState.AllPlayers.Values)
        {
            player.CurrentRound.Value++;
            Debug.Log($"📈 Player {player.OwnerClientId} now at Round {player.CurrentRound.Value}");
        }
    }

    private void TeleportPlayersBackToOriginalPositions()
    {
        foreach (var kvp in originalPlayerPositions)
        {
            ulong clientId = kvp.Key;
            var player = PlayerNetworkState.GetPlayerByClientId(clientId);
            if (player == null) continue;

            Vector3 pos = originalPlayerPositions[clientId];
            Quaternion rot = originalPlayerRotations[clientId];
            player.TeleportClientRpc(pos, rot);
        }

        originalPlayerPositions.Clear();
        originalPlayerRotations.Clear();
    }

    private void CleanupArena()
    {
        foreach (var tile in battleGrid)
        {
            if (tile != null)
                tile.RemoveUnit();
        }
    }
    [ClientRpc]
    private void ShowRoundResultClientRpc(ulong[] winningClientIds)
    {
        bool isWinner = winningClientIds.Contains(NetworkManager.Singleton.LocalClientId);
        RewardUI.Instance?.ShowRoundResult(isWinner);
    }

}
