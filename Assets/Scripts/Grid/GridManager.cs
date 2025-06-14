using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    public GameObject tilePrefab;
    public int width = 8;
    public int height = 8;
    public float spacing = 1.05f;

    public Dictionary<Vector2Int, GridTile> tileMap = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = new Vector3(x * spacing, 0, z * spacing);
                GameObject tileObj = Instantiate(tilePrefab, pos, Quaternion.identity, transform);

                GridTile gridTile = tileObj.GetComponent<GridTile>();
                Vector2Int coord = new Vector2Int(x, z);
                gridTile.Init(coord);

                tileMap[coord] = gridTile;

                Renderer renderer = tileObj.GetComponent<Renderer>();
                Color color = (z < height / 2) ? Color.cyan : Color.red;
                propertyBlock.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(propertyBlock);

                Debug.Log($"Tile {coord} → {(z < height / 2 ? "CYAN" : "RED")}");
            }
        }
    }
}
