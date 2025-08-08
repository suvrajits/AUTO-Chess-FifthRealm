using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;

public class HeroDragHandler : MonoBehaviour
{
    private Camera mainCamera;
    private HeroUnit heroUnit;
    private Rigidbody rb;

    private Vector3 offset;
    private GridTile originalTile;
    private bool isDragging = false;
    private bool wasKinematic;

    private void Awake()
    {
        mainCamera = Camera.main;
        heroUnit = GetComponent<HeroUnit>();
        rb = GetComponent<Rigidbody>();
        if (rb != null) wasKinematic = rb.isKinematic;
    }

    void Update()
    {
        // Handle touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;

            Vector3 touchWorld = GetTouchWorldPosition(touch.position);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    TryBeginDrag(touchWorld);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isDragging)
                        UpdateDragPosition(touchWorld);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isDragging)
                        EndDrag();
                    break;
            }
        }
        // Also support mouse for editor
        else if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            TryBeginDrag(GetMouseWorldPosition());
        }
        else if (Input.GetMouseButton(0))
        {
            if (isDragging)
                UpdateDragPosition(GetMouseWorldPosition());
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
                EndDrag();
        }
    }

    private void TryBeginDrag(Vector3 startPos)
    {
        if (!heroUnit.IsOwner) return;

        originalTile = heroUnit.currentTile;
        offset = transform.position - startPos;
        isDragging = true;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        GridManager.Instance.ShowAllTiles(true, pulse: true);
    }

    private void UpdateDragPosition(Vector3 worldPos)
    {
        Vector3 newPos = worldPos + offset;
        newPos.y = transform.position.y; // Lock height
        transform.position = newPos;
    }

    private void EndDrag()
    {
        isDragging = false;

        if (rb != null)
            rb.isKinematic = wasKinematic;

        GridManager.Instance.ShowAllTiles(false);

        GridTile targetTile = GridManager.Instance.GetTileUnderWorldPosition(transform.position);
        if (targetTile == null || targetTile.IsOccupied || !targetTile.IsOwnedBy(NetworkManager.Singleton.LocalClientId))
        {
            heroUnit.SnapToTileY(originalTile);
            return;
        }

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
        return Physics.Raycast(ray, out var hit, 100f, LayerMask.GetMask("Tile")) ? hit.point : Vector3.zero;
    }

    private Vector3 GetTouchWorldPosition(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, out var hit, 100f, LayerMask.GetMask("Tile")) ? hit.point : Vector3.zero;
    }
}
