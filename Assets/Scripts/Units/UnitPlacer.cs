using Unity.Netcode;
using UnityEngine;

public class UnitPlacer : NetworkBehaviour
{
    public LayerMask tileLayer;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (!IsOwner || mainCamera == null) return;

        if (UIOverlayManager.Instance != null && UIOverlayManager.Instance.IsPopupOpen())
        {
            Debug.Log($"🚫 Input blocked due to active popup: {UIOverlayManager.Instance.ActivePopup}");
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceOrReplaceUnit();
        }
    }

    private void TryPlaceOrReplaceUnit()
    {
        HeroCardInstance selectedHero = UnitSelectionManager.Instance.GetSelectedCard();

        if (selectedHero == null || selectedHero.baseHero == null)
        {
            Debug.LogWarning("❌ No hero selected for placement.");
            return;
        }

        var playerDeck = PlayerNetworkState.GetLocalPlayer()?.PlayerDeck;

        if (playerDeck == null || !playerDeck.cards.Contains(selectedHero))
        {
            Debug.LogWarning("⚠️ Selected card is no longer in the deck. Cancelling placement.");
            UnitSelectionManager.Instance.ClearSelectedCard();
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayer))
        {
            GridTile tile = hit.collider.GetComponent<GridTile>();
            if (tile == null) return;

            ulong viewedClientId = CameraSwitcherUI.CurrentTargetId;
            ulong localClientId = NetworkManager.Singleton.LocalClientId;

            if (viewedClientId != localClientId)
            {
                Debug.LogWarning("🚫 Can't place units while spectating.");
                return;
            }

            if (tile.OwnerClientId != localClientId)
            {
                Debug.LogWarning("🚫 This tile belongs to another player.");
                return;
            }

            SpawnUnitServerRpc(tile.GridPosition, selectedHero.baseHero.heroId, selectedHero.starLevel);

            // ✅ Locally clear selection only (deck update happens via ServerRpc + ClientRpc)
            UnitSelectionManager.Instance.ClearSelectedCard();
        }
    }

    [ServerRpc]
    private void SpawnUnitServerRpc(Vector2Int gridPos, int heroId, int starLevel, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        Debug.Log($"⚔️ [Server] Spawning unit with heroId: {heroId}");

        HeroData heroData = UnitDatabase.Instance.GetHeroById(heroId);
        if (heroData == null || heroData.heroPrefab == null)
        {
            Debug.LogError($"❌ HeroData not found or invalid prefab for heroId: {heroId}");
            return;
        }

        if (!GridManager.Instance.TryGetTile(senderId, gridPos, out GridTile tile))
        {
            Debug.LogWarning($"⚠️ Tile not found at {gridPos} for Player {senderId}");
            return;
        }

        if (tile.IsOccupied)
        {
            var existingUnit = tile.OccupyingUnit;
            if (existingUnit != null && existingUnit.OwnerClientId == senderId)
            {
                BattleManager.Instance.UnregisterUnit(existingUnit);
                existingUnit.GetComponent<NetworkObject>()?.Despawn(true);
                tile.RemoveUnit();
            }
            else
            {
                Debug.LogWarning("🚫 Cannot replace another player's unit.");
                return;
            }
        }

        Vector3 spawnPos = tile.transform.position + new Vector3(0, 0.55f, 0);
        GameObject unitObj = Instantiate(heroData.heroPrefab, spawnPos, Quaternion.identity);

        HeroUnit heroUnit = unitObj.GetComponent<HeroUnit>();
        heroUnit.SnapToTileY(tile);
        heroUnit.SetFaction(FactionForClient(senderId));

        heroUnit.InitFromDeck(new HeroCardInstance
        {
            baseHero = heroData,
            starLevel = starLevel
        });

        unitObj.GetComponent<NetworkObject>().SpawnWithOwnership(senderId);

        BattleManager.Instance.RegisterUnit(heroUnit, heroUnit.Faction);

        // ✅ Remove card from deck and sync it to client
        PlayerNetworkState player = PlayerNetworkState.GetPlayerByClientId(senderId);
        player?.PlayerDeck?.RemoveCardInstance(new HeroCardInstance { baseHero = heroData, starLevel = starLevel });
        player?.PlayerDeck?.SyncDeckToClient(senderId);
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
