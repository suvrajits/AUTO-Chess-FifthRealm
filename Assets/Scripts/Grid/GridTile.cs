using UnityEngine;

public class GridTile : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public ulong OwnerClientId { get; private set; }

    public bool IsOccupied => OccupyingUnit != null;

    public HeroUnit OccupyingUnit { get; private set; }

    private Renderer tileRenderer;
    private MaterialPropertyBlock propertyBlock;
    private MeshRenderer meshRenderer;

    public void Init(Vector2Int position, ulong ownerClientId)
    {
        GridPosition = position;
        OwnerClientId = ownerClientId;
        name = $"Tile_{position.x}_{position.y}";

        tileRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        // Color assignment removed — glow material is now used.
    }


    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        Show(false); // default: hidden
    }

    public void Show(bool visible)
    {
        if (meshRenderer != null)
            meshRenderer.enabled = visible;
    }

    public void AssignUnit(HeroUnit unit)
    {
        OccupyingUnit = unit;
    }

    public void RemoveUnit()
    {
        OccupyingUnit = null;
    }
    public bool IsOwnedBy(ulong clientId)
    {
        return OwnerClientId == clientId;
    }
    public bool HasUnit()
    {
        return OccupyingUnit != null;
    }
}
