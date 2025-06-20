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
    private NetworkVariable<Color> tileColor = new NetworkVariable<Color>(
    writePerm: NetworkVariableWritePermission.Server);

    public void Init(Vector2Int position, ulong ownerClientId, Color ownerColor)
    {
        GridPosition = position;
        TileOwnerClientId = ownerClientId;
        name = $"Tile_{position.x}_{position.y}";

        tileRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        if (IsServer)
            tileColor.Value = ownerColor;

        ApplyColorOverride(tileColor.Value);
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
    private void OnEnable()
    {
        tileColor.OnValueChanged += (_, newColor) => ApplyColorOverride(newColor);
    }
}
