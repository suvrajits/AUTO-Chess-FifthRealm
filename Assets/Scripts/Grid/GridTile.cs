using UnityEngine;

public class GridTile : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }

    public void Init(Vector2Int position)
    {
        GridPosition = position;
        name = $"Tile_{position.x}_{position.y}";
    }
}

