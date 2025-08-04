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

    [Header("Glow")]
    [SerializeField] private GameObject glowObject; // Assign the glow mesh child in prefab
    private Material glowMaterial;

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
        tileRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        // 🔁 Get glow material from child
        if (glowObject != null)
        {
            Renderer glowRenderer = glowObject.GetComponent<Renderer>();
            if (glowRenderer != null)
                glowMaterial = glowRenderer.material;

            glowObject.SetActive(false); // Hide glow initially
        }
        Show(false); // default: hidden
    }

    public void Show(bool visible)
    {
        if (meshRenderer != null)
            meshRenderer.enabled = visible;

        if (glowObject != null)
            glowObject.SetActive(visible);
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
    public void EnableGlow(bool pulse = false)
    {
        Show(true); // show both tile and glow

        if (glowMaterial != null)
        {
            glowMaterial.SetFloat("_PulseSpeed", pulse ? 2f : 0f); // assuming your shader uses _PulseSpeed
        }
    }

    public void DisableGlow()
    {
        Show(false); // hides both tile and glow
    }
}
