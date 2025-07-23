using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class UnitPlacer : NetworkBehaviour
{
    public LayerMask tileLayer;

    public static UnitPlacer Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
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

        // 🔥 Placement cap enforcement (client-side check)
        var player = PlayerNetworkState.GetPlayerByClientId(clientId);
        if (player != null)
        {
            int placed = player.PlacedUnitCount.Value;
            int allowed = player.MaxUnitsAllowed;

            if (placed >= allowed)
            {
                Debug.LogWarning($"⚠️ Cannot place unit: limit reached ({placed}/{allowed})");

                if (RoundHUDUI.Instance != null)
                    RoundHUDUI.Instance.FlashLimitReached();

                return;
            }
        }

        SpawnUnitServerRpc(tile.GridPosition, cardInstance.baseHero.heroId, cardInstance.starLevel);
        UnitSelectionManager.Instance.ClearSelectedCard();
    }


    [ServerRpc(RequireOwnership = false)]
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
            Debug.LogWarning($"🚫 Cannot place unit on occupied tile at {gridPos} — owned by Client {tile.OccupyingUnit?.OwnerClientId}");
            return;
        }


        // 🔒 Optional: server-side safety check on unit cap
        PlayerNetworkState player = PlayerNetworkState.GetPlayerByClientId(senderId);
        if (player != null && player.PlacedUnitCount.Value >= player.MaxUnitsAllowed)
        {
            Debug.LogWarning($"🚫 Server: Player {senderId} exceeded unit cap. Placement denied.");
            return;
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

        if (player != null)
        {
            player.PlayerDeck?.RemoveCardInstance(new HeroCardInstance { baseHero = heroData, starLevel = starLevel });
            player.PlayerDeck?.SyncDeckToClient(senderId);

            // ✅ Increment synced unit count
            player.PlacedUnitCount.Value++;
            Debug.Log($"📈 Player {senderId} placed unit. Count: {player.PlacedUnitCount.Value}");
        }
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
    public void OnDraggingOverTile(GridTile tile)
    {
        if (tile == null || !tile.IsOwnedBy(NetworkManager.Singleton.LocalClientId))
            return;
    }

}
