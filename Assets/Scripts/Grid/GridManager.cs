using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    public GameObject tilePrefab;
    public int gridSize = 8;
    public float spacing = 1.05f;

    // Offset to apply relative to each spawn anchor
    private readonly Vector3 gridOffset = new Vector3(-2.7f, 1f, -7.5f);

    public Dictionary<ulong, Dictionary<Vector2Int, GridTile>> playerTileMaps = new();

    // Fixed player colors per client ID
    public static readonly Dictionary<ulong, Color> PlayerColors = new()
    {
        { 0, Color.red },
        { 1, Color.cyan },
        { 2, Color.green },
        { 3, Color.yellow },
        { 4, Color.magenta },
        { 5, Color.blue },
        { 6, new Color(1f, 0.5f, 0f) }, // Orange
        { 7, new Color(0.5f, 0f, 1f) }  // Purple
    };

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GenerateGridsForAllPlayers();
    }

    private void GenerateGridsForAllPlayers()
    {
        foreach (var kvp in PlayerColors)
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
        Color color = PlayerColors[playerId];

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                Vector2Int coord = new Vector2Int(x, z);
                Vector3 worldPos = offset + new Vector3(x * spacing, 0, z * spacing);
                GameObject tileObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);

                GridTile gridTile = tileObj.GetComponent<GridTile>();
                gridTile.Init(coord, playerId, color);
                tileMap[coord] = gridTile;
            }
        }

        playerTileMaps[playerId] = tileMap;
    }

    public bool TryGetTile(ulong playerId, Vector2Int coord, out GridTile tile)
    {
        tile = null;
        return playerTileMaps.TryGetValue(playerId, out var map) && map.TryGetValue(coord, out tile);
    }
}
