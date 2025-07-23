using UnityEngine;

public class GridTile : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public ulong OwnerClientId { get; private set; }

    public bool IsOccupied => OccupyingUnit != null;

    public HeroUnit OccupyingUnit { get; private set; }

    private Renderer tileRenderer;
    private MaterialPropertyBlock propertyBlock;
    [SerializeField] private GameObject glowObject;
    private Material glowMaterial;

    public void Init(Vector2Int position, ulong ownerClientId, Color ownerColor)
    {
        GridPosition = position;
        OwnerClientId = ownerClientId;
        name = $"Tile_{position.x}_{position.y}";

        tileRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        SetTileColor(ownerColor);
    }
    private void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        if (glowObject != null)
            glowMaterial = glowObject.GetComponent<Renderer>()?.material;

        SetVisible(false); // start hidden
    }

    public void SetTileColor(Color color)
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
    public bool IsOwnedBy(ulong clientId)
    {
        return OwnerClientId == clientId;
    }
    public void SetVisible(bool visible)
    {
        if (tileRenderer != null)
            tileRenderer.enabled = visible;

        if (glowObject != null)
            glowObject.SetActive(visible);
    }

    public void EnableGlow(bool isPulsing = false)
    {
        SetVisible(true);
        if (glowMaterial != null)
            glowMaterial.SetFloat("_PulseSpeed", isPulsing ? 2f : 0f);
    }

    public void DisableGlow()
    {
        SetVisible(false);
    }
}
