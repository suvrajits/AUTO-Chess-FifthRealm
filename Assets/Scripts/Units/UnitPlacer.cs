using Unity.Netcode;
using UnityEngine;

public class UnitPlacer : NetworkBehaviour
{
    [Header("Unit Config")]
    public HeroData heroData; // Assigned in Inspector
    public LayerMask tileLayer;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (!IsOwner || mainCamera == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceUnit();
        }
    }

    private void TryPlaceUnit()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayer))
        {
            GridTile tile = hit.collider.GetComponent<GridTile>();
            if (tile == null)
            {
                Debug.LogWarning("⚠️ Hit object has no GridTile component.");
                return;
            }

            // Only allow placing if you're viewing your own board AND the tile hit belongs to you
            ulong viewedClientId = CameraSwitcherUI.CurrentTargetId;
            ulong localClientId = NetworkManager.Singleton.LocalClientId;

            if (viewedClientId != localClientId)
            {
                Debug.LogWarning("🚫 Cannot place units while spectating other players.");
                return;
            }

            if (tile.OwnerClientId != localClientId)
            {
                Debug.LogWarning($"⚠️ This tile belongs to another player (tile.Owner = {tile.OwnerClientId}, local = {localClientId})");
                return;
            }

            Debug.Log($"🟢 Valid tile selected at {tile.GridPosition} by Player {localClientId}");
            SpawnUnitServerRpc(tile.GridPosition);
        }
        else
        {
            Debug.LogWarning("❌ Raycast missed. No tile clicked.");
        }
    }


    [ServerRpc]
    private void SpawnUnitServerRpc(Vector2Int gridPos, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (heroData == null || heroData.heroPrefab == null)
        {
            Debug.LogError("❌ HeroData or HeroPrefab not assigned!");
            return;
        }

        if (!GridManager.Instance.TryGetTile(senderId, gridPos, out GridTile tile))
        {
            Debug.LogWarning($"⚠️ Tile not found at {gridPos} for Player {senderId}");
            return;
        }

        Vector3 spawnPos = tile.transform.position + new Vector3(0, 0.55f, 0);
        Quaternion spawnRot = senderId % 2 == 0 ? Quaternion.identity : Quaternion.Euler(0, 180f, 0);

        GameObject unitObj = Instantiate(heroData.heroPrefab, spawnPos, spawnRot);

        HeroUnit heroUnit = unitObj.GetComponent<HeroUnit>();
        heroUnit.GridPosition = gridPos;
        heroUnit.heroData = heroData;
        heroUnit.SetFaction(FactionForClient(senderId));

        unitObj.GetComponent<NetworkObject>().SpawnWithOwnership(senderId);

        BattleManager.Instance.RegisterUnit(heroUnit, heroUnit.Faction);
    }

    private Faction FactionForClient(ulong clientId)
    {
        return clientId switch
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
}
