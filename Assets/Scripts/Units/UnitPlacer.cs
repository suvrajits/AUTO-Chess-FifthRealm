using Unity.Netcode;
using UnityEngine;

public class UnitPlacer : NetworkBehaviour
{
    [Header("Unit Config")]
    public HeroData heroData;
    public LayerMask tileLayer;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!IsOwner || mainCamera == null) return;

        if (Input.GetMouseButtonDown(0))
            TryPlaceUnitClientSide();
    }

    private void TryPlaceUnitClientSide()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayer))
        {
            if (!hit.collider.TryGetComponent(out GridTile tile)) return;

            // Don't trust ownership here — let server validate
            PlaceUnitAtServerRpc(tile.GridPosition);
        }
    }

    [ServerRpc]
    private void PlaceUnitAtServerRpc(Vector2Int gridPos, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!PlayerNetworkState.AllPlayerGrids.TryGetValue(clientId, out var gridManager))
        {
            Debug.LogError($"❌ GridManager not found for Client {clientId}");
            return;
        }

        if (!gridManager.TryGetTile(gridPos, out var tile) || tile == null)
        {
            Debug.LogError($"❌ Tile {gridPos} not found in grid for Client {clientId}");
            return;
        }

        if (tile.OwnerClientId != clientId)
        {
            Debug.LogWarning($"🚫 Client {clientId} attempted to place on tile owned by {tile.OwnerClientId}");
            return;
        }

        // Clean replace logic
        if (tile.IsOccupied)
        {
            var existing = tile.OccupyingUnit;
            if (existing != null && existing.OwnerClientId == clientId)
            {
                BattleManager.Instance.UnregisterUnit(existing);
                existing.GetComponent<NetworkObject>().Despawn(true);
                tile.RemoveUnit();
            }
            else
            {
                Debug.LogWarning("🚫 Cannot replace another player's unit.");
                return;
            }
        }

        GameObject unitObj = Instantiate(heroData.heroPrefab, tile.transform.position + Vector3.up * 0.5f, Quaternion.identity);
        var netObj = unitObj.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(clientId);

        var heroUnit = unitObj.GetComponent<HeroUnit>();
        heroUnit.GridPosition = gridPos;
        heroUnit.heroData = heroData;
        heroUnit.SetFaction(FactionForClient(clientId));

        tile.AssignUnit(heroUnit);
        BattleManager.Instance.RegisterUnit(heroUnit, heroUnit.Faction);

        Debug.Log($"✅ Hero placed at {gridPos} for Player {clientId}");
    }

    private Faction FactionForClient(ulong clientId) => clientId switch
    {
        0 => Faction.Player1,
        1 => Faction.Player2,
        2 => Faction.Player3,
        3 => Faction.Player4,
        4 => Faction.Player5,
        5 => Faction.Player6,
        6 => Faction.Player7,
        7 => Faction.Player8,
        _ => Faction.Neutral
    };
}
