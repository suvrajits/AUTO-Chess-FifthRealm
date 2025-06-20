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
            TryPlaceOrReplaceUnit();
    }

    private void TryPlaceOrReplaceUnit()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayer))
        {
            if (!hit.collider.TryGetComponent(out GridTile tile)) return;

            if (tile.TileOwnerClientId != NetworkManager.Singleton.LocalClientId)
            {
                Debug.LogWarning("🚫 Cannot place unit on another player’s tile.");
                return;
            }

            SpawnUnitServerRpc(tile.GridPosition);
        }
    }


    [ServerRpc]
    private void SpawnUnitServerRpc(Vector2Int gridPos, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (heroData?.heroPrefab == null)
        {
            Debug.LogError("❌ HeroData or HeroPrefab missing");
            return;
        }

        if (!PlayerNetworkState.AllPlayerGrids.TryGetValue(senderId, out var gridManager) ||
            !gridManager.TryGetTile(gridPos, out GridTile tile) || tile == null)
        {
            Debug.LogError("❌ Valid GridTile not found for placement.");
            return;
        }

        if (tile.IsOccupied)
        {
            var existing = tile.OccupyingUnit;
            if (existing != null && existing.OwnerClientId == senderId)
            {
                BattleManager.Instance.UnregisterUnit(existing);
                existing.GetComponent<NetworkObject>().Despawn(true);
                tile.RemoveUnit();
            }
            else
            {
                Debug.LogWarning("🚫 Cannot replace another player’s unit.");
                return;
            }
        }

        GameObject unitObj = Instantiate(heroData.heroPrefab);
        NetworkObject netObj = unitObj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("❌ Hero prefab missing NetworkObject");
            return;
        }

        netObj.SpawnWithOwnership(senderId);

        if (!unitObj.TryGetComponent(out HeroUnit heroUnit))
        {
            Debug.LogError("❌ HeroUnit component not found after spawn.");
            return;
        }

        // DEFER tile assign until OnSpawnInitialized
        heroUnit.OnSpawnInitialized(tile, heroData, FactionForClient(senderId));
        
        tile.AssignUnit(heroUnit);

        BattleManager.Instance.RegisterUnit(heroUnit, heroUnit.Faction);

        Debug.Log($"✅ {heroData.heroName} placed at {gridPos} for Player {senderId}");
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
