using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class HeroDragHandler : MonoBehaviour
{
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 offset;
    private HeroUnit heroUnit;
    private GridTile originalTile;
    private Rigidbody rb;
    private bool wasKinematic;

    private void Awake()
    {
        mainCamera = Camera.main;
        heroUnit = GetComponent<HeroUnit>();
        rb = GetComponent<Rigidbody>();
        if (rb != null)
            wasKinematic = rb.isKinematic;
    }

    void OnMouseDown()
    {
        if (!heroUnit.IsOwner || EventSystem.current.IsPointerOverGameObject())
            return;

        isDragging = true;
        originalTile = heroUnit.currentTile;
        offset = transform.position - GetMouseWorldPosition();

        // ✅ Disable physics during drag
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // ✅ Show valid grid tiles
        GridManager.Instance.ShowAllTiles(true, pulse: true);
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 newPos = GetMouseWorldPosition() + offset;
        newPos.y = transform.position.y; // Lock Y axis
        transform.position = newPos;
    }

    void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        // ✅ Re-enable physics
        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
        }

        // ✅ Hide grid tiles
        GridManager.Instance.ShowAllTiles(false);

        // 👇 Validate drop
        GridTile targetTile = GridManager.Instance.GetTileUnderWorldPosition(transform.position);
        if (targetTile == null || targetTile.IsOccupied || !targetTile.IsOwnedBy(NetworkManager.Singleton.LocalClientId))
        {
            // ❌ Invalid → snap back
            heroUnit.SnapToTileY(originalTile);
            return;
        }

        // ✅ Valid → send reposition request
        var player = PlayerNetworkState.GetLocalPlayer();
        if (player != null)
        {
            player.RequestRepositionHeroServerRpc(
                heroUnit.NetworkObjectId,
                targetTile.GridPosition.x,
                targetTile.GridPosition.y
            );
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Tile")))
        {
            return hit.point;
        }
        return Vector3.zero;
    }
}
