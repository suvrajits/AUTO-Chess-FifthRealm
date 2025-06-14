using Unity.Netcode;
using UnityEngine;

public class UnitPlacer : NetworkBehaviour
{
    public HeroData heroData; // Assigned in Inspector
    public LayerMask tileLayer;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("👆 Mouse click detected");

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogWarning("⚠️ Main camera is still null");
                    return;
                }
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayer))
            {
                Debug.Log("✅ Raycast hit: " + hit.collider.name);

                GridTile tile = hit.collider.GetComponent<GridTile>();
                if (tile == null)
                {
                    Debug.LogWarning("⚠️ Hit object has no GridTile component");
                    return;
                }

                Debug.Log("🧱 Tile clicked: " + tile.GridPosition);

                if (IsTileOnMySide(tile.GridPosition))
                {
                    Debug.Log("🟢 Tile is on MY side. Calling ServerRpc.");
                    SpawnUnitServerRpc(tile.GridPosition);
                }
                else
                {
                    Debug.Log("🔴 Tile is on opponent's side. Ignored.");
                }
            }
            else
            {
                Debug.LogWarning("❌ Raycast missed");
            }
        }
    }



    private bool IsTileOnMySide(Vector2Int pos)
    {
        // 8x8 board: rows 0–3 = client, 4–7 = host
        return (IsHost && pos.y < 4) || (!IsHost && pos.y >= 4);
    }

    [ServerRpc]
    private void SpawnUnitServerRpc(Vector2Int gridPos, ServerRpcParams rpcParams = default)
    {
        if (heroData == null || heroData.heroPrefab == null)
        {
            Debug.LogError("HeroData or HeroPrefab not assigned!");
            return;
        }

        if (!GridManager.Instance.tileMap.TryGetValue(gridPos, out GridTile tile))
        {
            Debug.LogWarning("Tile not found at: " + gridPos);
            return;
        }

        Vector3 spawnPos = tile.transform.position + new Vector3(0, 0.55f, 0);

        // Rotate 180° for Player 2
        bool isPlayer2 = rpcParams.Receive.SenderClientId != 0;
        Quaternion spawnRot = isPlayer2 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

        GameObject unitObj = Instantiate(heroData.heroPrefab, spawnPos, spawnRot);


        HeroUnit heroUnit = unitObj.GetComponent<HeroUnit>();
        heroUnit.GridPosition = gridPos;
        heroUnit.heroData = heroData;

        // Determine correct faction
        Faction assignedFaction = (rpcParams.Receive.SenderClientId == 0) ? Faction.Player1 : Faction.Player2;

        // Set faction BEFORE spawning (✅ guarantees sync)
        heroUnit.SetFaction(assignedFaction);

        // Network spawn (now the NetworkVariable syncs with correct value)
        NetworkObject netObj = unitObj.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(rpcParams.Receive.SenderClientId);

        // Register unit (after it's fully spawned)
        BattleManager.Instance.RegisterUnit(heroUnit, assignedFaction);

    }
}
