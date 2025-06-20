using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GridManager : NetworkBehaviour
{
    [Header("Grid Settings")]
    public GameObject tilePrefab;
    public int gridSize = 6;
    public float spacing = 1.05f;

    private Dictionary<Vector2Int, GridTile> tileMap = new();
    public ulong OwnerId { get; private set; }

    public void Init(ulong ownerClientId)
    {
        OwnerId = ownerClientId;
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        Color color = GetPlayerColor(OwnerId);

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                Vector2Int coord = new Vector2Int(x, z);
                Vector3 localPos = new Vector3(x * spacing, 0, z * spacing);
                GameObject tileObj = Instantiate(tilePrefab, transform.position + localPos, Quaternion.identity, transform);

                var netObj = tileObj.GetComponent<NetworkObject>();
                if (netObj != null)
                    netObj.Spawn();

                GridTile tile = tileObj.GetComponent<GridTile>();
                tile.Init(coord, OwnerId, color);
                tileMap[coord] = tile;

                // 🟡 Sync color on clients
                SyncTileColorClientRpc(coord, color);
            }
        }
    }

    [ClientRpc]
    private void SyncTileColorClientRpc(Vector2Int coord, Color color)
    {
        if (tileMap.TryGetValue(coord, out var tile))
        {
            tile.SetTileColor(color);
        }
    }

    public bool TryGetTile(Vector2Int coord, out GridTile tile) => tileMap.TryGetValue(coord, out tile);

    public Vector3 GetGridCenter()
    {
        float halfSize = (gridSize - 1) * spacing / 2f;
        return transform.position + new Vector3(halfSize, 0, halfSize);
    }

    private Color GetPlayerColor(ulong id)
    {
        return id switch
        {
            0 => Color.red,
            1 => Color.cyan,
            2 => Color.green,
            3 => Color.yellow,
            4 => Color.magenta,
            5 => Color.blue,
            6 => new Color(1f, 0.5f, 0f),
            7 => new Color(0.5f, 0f, 1f),
            _ => Color.white
        };
    }
    public IEnumerable<GridTile> GetAllTiles()
    {
        return tileMap.Values;
    }
}
