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

    // ✅ New method for drag-and-drop placement
    public void TryPlaceUnitFromDeck(HeroCardInstance cardInstance, GridTile tile)
    {
        if (cardInstance == null || tile == null)
        {
            Debug.LogWarning("❌ Invalid card or tile.");
            return;
        }

        ulong clientId = NetworkManager.Singleton.LocalClientId;

        if (CameraSwitcherUI.CurrentTargetId != clientId)
        {
            Debug.LogWarning("🚫 Can't place while spectating.");
            return;
        }

        if (!tile.IsOwnedBy(clientId))
        {
            Debug.LogWarning("🚫 Tile not owned by this player.");
            return;
        }

        if (tile.IsOccupied)
        {
            Debug.LogWarning("🚫 Tile already occupied.");
            return;
        }

        SpawnUnitServerRpc(tile.GridPosition, cardInstance.baseHero.heroId, cardInstance.starLevel);
        UnitSelectionManager.Instance.ClearSelectedCard();
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
