using Unity.Netcode;
using UnityEngine;

public class TileSelector : MonoBehaviour
{
    public LayerMask tileLayer;
    private Camera mainCamera;
    public static TileSelector Instance { get; private set; }
    private bool isDraggingCard = false;
    private GridTile currentHoveredTile;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple TileSelector instances detected.");
            Destroy(gameObject);
        }
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (isDraggingCard)
        {
            if (Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100f, tileLayer))
            {
                var tile = hit.collider.GetComponent<GridTile>();
                if (tile != null && tile.IsOwnedBy(NetworkManager.Singleton.LocalClientId))
                    UnitPlacer.Instance?.OnDraggingOverTile(tile);
            }
        }
    }

    public bool GetTileUnderCursor(out GridTile tile)
    {
        tile = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayer))
        {
            tile = hit.collider.GetComponent<GridTile>();
            return tile != null;
        }

        return false;
    }
    public void SetDraggingState(bool isDragging)
    {
        isDraggingCard = isDragging;

        if (!isDragging && currentHoveredTile != null)
        {
            currentHoveredTile.EnableGlow(false); // stop pulse
            currentHoveredTile = null;
        }
    }
    public void OnDraggingOverTile(GridTile tile)
    {
        if (!isDraggingCard || tile == null || !tile.IsOwnedBy(NetworkManager.Singleton.LocalClientId))
            return;

        if (currentHoveredTile == tile)
            return;

        // Stop pulse on previous
        if (currentHoveredTile != null)
            currentHoveredTile.EnableGlow(false);

        // Pulse on new tile
        currentHoveredTile = tile;
        currentHoveredTile.EnableGlow(true);
    }

}
