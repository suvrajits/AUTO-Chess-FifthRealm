using UnityEngine;
using Unity.Netcode;

public class GridTile : NetworkBehaviour
{
    public Vector2Int GridPosition { get; private set; }

    // ⚠️ Avoid naming conflict with NetworkBehaviour.OwnerClientId
    public ulong TileOwnerClientId { get; private set; }

    public bool IsOccupied => OccupyingUnit != null;
    public HeroUnit OccupyingUnit { get; private set; }

    private Renderer tileRenderer;
    private MaterialPropertyBlock propertyBlock;

    public void Init(Vector2Int position, ulong ownerClientId, Color ownerColor)
    {
        GridPosition = position;
        TileOwnerClientId = ownerClientId;
        name = $"Tile_{position.x}_{position.y}";

        tileRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        ApplyColorOverride(ownerColor);
    }

    public void ApplyColorOverride(Color color)
    {
        if (tileRenderer == null)
            tileRenderer = GetComponent<Renderer>();

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        propertyBlock.SetColor("_BaseColor", color);
        tileRenderer.SetPropertyBlock(propertyBlock);
    }

    public void AssignUnit(HeroUnit unit)
    {
        OccupyingUnit = unit;
    }

    public void RemoveUnit()
    {
        OccupyingUnit = null;
    }
}
