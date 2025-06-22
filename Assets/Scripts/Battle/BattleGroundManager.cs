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

    [Header("Battle Units")]
    private List<HeroUnit> teamAUnits = new();
    private List<HeroUnit> teamBUnits = new();
    private Dictionary<HeroUnit, GridTile> originalTileMemory = new();

    [Header("State")]
    private bool battleInProgress = false;

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

                tile.Init(new Vector2Int(x, y), 0, Color.gray); // Neutral tile, dummy owner
                tile.GetComponent<NetworkObject>().Spawn();

                battleGrid[x, y] = tile;
            }
        }
    }
    
    // Called by RoundManager or MatchManager
    [ServerRpc(RequireOwnership = false)]
    public void StartBattleServerRpc()
    {
        if (battleInProgress) return;

        battleInProgress = true;
        PickTeams();

        TeleportToBattleGrid(teamAUnits, true);
        TeleportToBattleGrid(teamBUnits, false);

        Invoke(nameof(BeginCombat), 2f); // Optional delay for visual prep
    }

    private void PickTeams()
    {
        List<HeroUnit> allUnits = BattleManager.Instance.GetAllAliveUnits();

        var players = allUnits.GroupBy(u => u.OwnerClientId).ToList();

        if (players.Count >= 4)
        {
            teamAUnits = players[0].Concat(players[1]).ToList();
            teamBUnits = players[2].Concat(players[3]).ToList();
        }
        else
        {
            teamAUnits = players[0].ToList();
            teamBUnits = players.Count > 1 ? players[1].ToList() : new List<HeroUnit>();
        }
    }

    private void TeleportToBattleGrid(List<HeroUnit> units, bool isTeamA)
    {
        int row = isTeamA ? 1 : 6;

        for (int i = 0; i < units.Count; i++)
        {
            HeroUnit unit = units[i];
            if (unit == null) continue;

            GridTile originalTile = unit.currentTile;
            if (originalTile != null)
                originalTileMemory[unit] = originalTile;

            int col = Mathf.Clamp(i, 0, gridSizeX - 1);

            GridTile targetTile = battleGrid[col, row];
            if (targetTile == null)
            {
                Debug.LogError($"❌ Missing battle tile at ({col},{row})");
                continue;
            }

            unit.SnapToTileY(targetTile);
            unit.SetCombatState(true);
        }
    }

    private void BeginCombat()
    {
        BattleManager.Instance.BeginCombat(teamAUnits, teamBUnits);
    }

    public void OnBattleEnded()
    {
        if (!IsServer) return;

        battleInProgress = false;

        foreach (var unit in teamAUnits.Concat(teamBUnits))
        {
            if (unit == null || !unit.IsAlive) continue;

            if (originalTileMemory.TryGetValue(unit, out var homeTile) && homeTile != null)
            {
                unit.SnapToTileY(homeTile);
                unit.SetCombatState(false);
            }
            else
            {
                Debug.LogWarning($" Could not find original tile for unit {unit.name}");
            }
        }

        CleanupArena();

        originalTileMemory.Clear();
        teamAUnits.Clear();
        teamBUnits.Clear();
    }

    private void CleanupArena()
    {
        foreach (var tile in battleGrid)
        {
            if (tile != null)
                tile.RemoveUnit();
        }
    }
}
