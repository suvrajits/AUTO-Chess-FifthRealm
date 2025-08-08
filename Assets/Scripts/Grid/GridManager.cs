using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    public GameObject tilePrefab;
    public int gridSize = 8;
    public int gridRows = 4;
    public float spacing = 1.05f;

    // Offset to apply relative to each spawn anchor
    private readonly Vector3 gridOffset = new Vector3(-3.5f, 1f, -9f);

    public Dictionary<ulong, Dictionary<Vector2Int, GridTile>> playerTileMaps = new();
    public List<GameObject> arenaPrefabs; // assign in inspector, index = playerId
    private Dictionary<ulong, GameObject> playerArenaPrefabs = new();
    public Vector3 arenaLocalOffset = new Vector3(0, 0, 5f);
    public Vector3 arenaRotationEuler = new Vector3(0, 0, 0);


    // Fixed player colors per client ID

    private void Awake()
    {
        Instance = this;
        // Build the playerArenaPrefabs dictionary based on player ID index
        for (int i = 0; i < arenaPrefabs.Count && i < 8; i++)
        {
            playerArenaPrefabs[(ulong)i] = arenaPrefabs[i];
        }
    }

    private void Start()
    {
        GenerateGridsForAllPlayers();
    }

    private void GenerateGridsForAllPlayers()
    {
        foreach (var kvp in PlayerNetworkState.AllPlayers)
        {
            ulong playerId = kvp.Key;
            int index = (int)playerId;

            Transform anchor = SpawnAnchorRegistry.Instance.GetAnchor(index);
            if (anchor == null)
            {
                Debug.LogWarning($"⚠️ No anchor set for player {index} in SpawnAnchorRegistry.");
                continue;
            }

            Vector3 finalOffset = anchor.position + gridOffset;
            GeneratePlayerGrid(playerId, finalOffset);
        }
    }


    private void GeneratePlayerGrid(ulong playerId, Vector3 offset)
    {
        Dictionary<Vector2Int, GridTile> tileMap = new();

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridRows; z++)
            {
                Vector2Int coord = new Vector2Int(x, z);
                Vector3 worldPos = offset + new Vector3(x * spacing, 0, z * spacing);
                GameObject tileObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);

                GridTile gridTile = tileObj.GetComponent<GridTile>();
                gridTile.Init(coord, playerId);
                tileMap[coord] = gridTile;
            }
        }

        playerTileMaps[playerId] = tileMap;
        // ✅ Spawn arena prefab for this player
        if (playerArenaPrefabs.TryGetValue(playerId, out GameObject arenaPrefab) && arenaPrefab != null)
        {
            Vector3 arenaPos = offset + arenaLocalOffset; // e.g., behind or beside the grid
            Quaternion arenaRot = Quaternion.Euler(arenaRotationEuler);

            GameObject arenaInstance = Instantiate(arenaPrefab, arenaPos, arenaRot, transform);
            arenaInstance.name = $"Arena_Player_{playerId}";

            Debug.Log($"🎯 Spawned arena for player {playerId} at {arenaPos}");
        }
        else
        {
            Debug.LogWarning($"⚠️ No arena prefab assigned for player {playerId}.");
        }
    }

    public bool TryGetTile(ulong playerId, Vector2Int coord, out GridTile tile)
    {
        tile = null;
        return playerTileMaps.TryGetValue(playerId, out var map) && map.TryGetValue(coord, out tile);
    }
    public void ShowAllTiles(bool visible, bool pulse = false)
    {
        foreach (var kvp in playerTileMaps)
        {
            foreach (var tile in kvp.Value.Values)
            {
                bool showTile = visible && !tile.HasUnit();

                if (showTile)
                    tile.EnableGlow(pulse);
                else
                    tile.DisableGlow();
            }
        }

        Debug.Log($"🟦 Grid visibility set to: {(visible ? "ON" : "OFF")}");
    }
    public GridTile GetTileUnderWorldPosition(Vector3 position)
    {
        Ray ray = new Ray(position + Vector3.up * 10f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 20f, LayerMask.GetMask("Tile")))
        {
            return hit.collider.GetComponent<GridTile>();
        }
        return null;
    }
    public GridTile GetLocalPlayerTileAt(int x, int y)
    {
        ulong clientId = NetworkManager.Singleton.LocalClientId;

        if (playerTileMaps.TryGetValue(clientId, out var tileMap))
        {
            Vector2Int coord = new Vector2Int(x, y);
            if (tileMap.TryGetValue(coord, out GridTile tile))
            {
                return tile;
            }
        }

        Debug.LogWarning($"❌ Tile at ({x},{y}) not found for local player {clientId}");
        return null;
    }


}
