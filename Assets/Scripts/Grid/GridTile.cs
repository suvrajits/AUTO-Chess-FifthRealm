using UnityEngine;

public class GridTile : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }

    /// <summary>
    /// The client ID of the player who owns this tile. Used to restrict placement.
    /// </summary>
    public ulong OwnerClientId { get; set; }

    private Renderer tileRenderer;
    private MaterialPropertyBlock propertyBlock;

    public void Init(Vector2Int position, ulong ownerClientId, Color ownerColor)
    {
        GridPosition = position;
        OwnerClientId = ownerClientId;
        name = $"Tile_{position.x}_{position.y}";

        tileRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        SetTileColor(ownerColor);
    }

    /// <summary>
    /// Sets the base color of the tile (for quadrant visualization).
    /// </summary>
    public void SetTileColor(Color color)
    {
        if (tileRenderer == null)
            tileRenderer = GetComponent<Renderer>();

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        propertyBlock.SetColor("_BaseColor", color);
        tileRenderer.SetPropertyBlock(propertyBlock);
    }

    /// <summary>
    /// Highlights the tile when hovered or selected (future extension).
    /// </summary>
    public void HighlightTile(Color highlightColor)
    {
        propertyBlock.SetColor("_BaseColor", highlightColor);
        tileRenderer.SetPropertyBlock(propertyBlock);
    }

    /// <summary>
    /// Resets the tile color to original ownership color.
    /// </summary>
    public void ResetTileColor(Color originalColor)
    {
        SetTileColor(originalColor);
    }
}
