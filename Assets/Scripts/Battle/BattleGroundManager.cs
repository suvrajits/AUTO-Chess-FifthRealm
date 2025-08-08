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

    [Header("Battle Arena")]
    public GameObject battleArenaPrefab;        // Assign in Inspector
    public Vector3 battleArenaOffset = new Vector3(-3.5f, 0f, -3.5f);
    public Vector3 battleArenaRotationEuler = new Vector3(0f, 0f, 0f);

    private GameObject spawnedBattleArena;      // Track instance

    [Header("Battle Grid Settings")]
    public int battleGridRows = 4;   // Match player rows
    public int battleGridCols = 8;   // Match player columns
    public float tileSpacing = 1.05f; // Match GridManager

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
        battleGrid = new GridTile[battleGridCols, battleGridRows];

        Vector3 origin = battleArenaAnchor.position - new Vector3((battleGridCols - 1) * tileSpacing / 2f, 0, (battleGridRows - 1) * tileSpacing / 2f);

        for (int x = 0; x < battleGridCols; x++)
        {
            for (int z = 0; z < battleGridRows; z++)
            {
                Vector3 pos = origin + new Vector3(x * tileSpacing, 0, z * tileSpacing);
                GameObject tileObj = Instantiate(battleTilePrefab, pos, Quaternion.identity);

                if (!tileObj.TryGetComponent(out GridTile tile))
                {
                    Debug.LogError($"❌ Battle tile prefab missing GridTile component at ({x},{z})");
                    continue;
                }

                tile.Init(new Vector2Int(x, z), 0);

                var tileNetObj = tile.GetComponent<NetworkObject>();
                if (tileNetObj != null)
                {
                    tileNetObj.Spawn(); // ✅ Network spawn tile
                }
                else
                {
                    Debug.LogError($"❌ Battle tile at ({x},{z}) missing NetworkObject component.");
                }

                battleGrid[x, z] = tile;
            }
        }

        // ✅ Spawn visual battle arena prefab for all clients
        if (battleArenaPrefab != null)
        {
            Vector3 arenaPos = battleArenaAnchor.position + battleArenaOffset;
            Quaternion arenaRot = Quaternion.Euler(battleArenaRotationEuler);

            spawnedBattleArena = Instantiate(battleArenaPrefab, arenaPos, arenaRot);
            spawnedBattleArena.name = "BattleArena_Visual";

            var arenaNetObj = spawnedBattleArena.GetComponent<NetworkObject>();
            if (arenaNetObj != null)
            {
                arenaNetObj.Spawn(); // ✅ Network spawn arena
                Debug.Log($"🏟️ Spawned 3D battle arena at {arenaPos}");
            }
            else
            {
                Debug.LogWarning("⚠️ BattleArena prefab missing NetworkObject component. It won't be visible to clients.");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No battleArenaPrefab assigned to BattleGroundManager.");
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void StartBattleServerRpc()
    {
        Debug.Log("🧪 StartBattleServerRpc triggered");
        PickTeams();
        InternalStartBattle(teamAUnits, teamBUnits);
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
        foreach (var hero in allBattleParticipants)
        {
            hero.BuffManager?.ClearAllPoison();
            hero.BuffManager?.ClearAllBuffs();
        }

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
        if (spawnedBattleArena != null)
        {
            Destroy(spawnedBattleArena);
            spawnedBattleArena = null;
        }

        RoundManager.Instance.ScheduleNextRoundWithDelay(3f);
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
    public void StartCustomBattle(List<HeroUnit> teamA, List<HeroUnit> teamB)
    {
        Debug.Log($"📦 StartCustomBattle received {teamA.Count} vs {teamB.Count} units");

        InternalStartBattle(teamA, teamB); // setup teleport and visuals

        // ✅ Trigger actual combat immediately — safer than delayed Invoke()
        BattleManager.Instance.BeginCombat(teamA, teamB);
    }

    private void InvokeBattleStart()
    {
        Debug.Log("⚔️ [BattleGroundManager] InvokeBattleStart()");
        if (!IsServer) return;

        Debug.Log("⚔️ Battle officially starting via InvokeBattleStart()");
        BattleManager.Instance.BeginCombat(teamAUnits, teamBUnits);
    }
    private void InternalStartBattle(List<HeroUnit> teamA, List<HeroUnit> teamB)
    {
        if (battleInProgress) return;

        if (spawnedBattleArena == null)
        {
            CreateBattleArena();
        }

        battleInProgress = true;

        teamAUnits = teamA;
        teamBUnits = teamB;

        foreach (var clientId in teamA.Concat(teamB).Select(u => u.OwnerClientId).Distinct())
        {
            var player = PlayerNetworkState.GetPlayerByClientId(clientId);
            if (player == null) continue;
            var aliveUnits = BattleManager.Instance.GetAliveUnitsForPlayer(clientId);
            player.TraitTracker?.RecalculateTraits(aliveUnits, player.PlayerLevel.Value);
        }

        allBattleParticipants.Clear();
        allBattleParticipants.AddRange(teamAUnits);
        allBattleParticipants.AddRange(teamBUnits);

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

        foreach (var unit in allBattleParticipants)
        {
            unit.GetComponent<TraitEffectHandler>()?.OnBattleStart();
        }

        Debug.Log("🧠 [BattleGroundManager] InternalStartBattle completed, invoking battle in 2s");
        Invoke(nameof(InvokeBattleStart), 2f);
    }
    public bool IsBattleInProgress()
    {
        return battleInProgress;
    }
}
