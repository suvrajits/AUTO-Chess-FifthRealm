using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(HeroStateMachine), typeof(HeroAnimatorHandler))]
public class HeroUnit : NetworkBehaviour, IUnitInteractable
{
    public HeroData heroData;
    public int starLevel = 1;

    private float attack;
    public float Attack => attack;

    public Vector2Int GridPosition { get; private set; }

    private NetworkVariable<Faction> faction = new NetworkVariable<Faction>(
        Faction.Neutral,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public Faction Faction => faction.Value;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public float CurrentHealth => currentHealth.Value;
    private bool hasDied = false;
    public bool IsDead => hasDied || CurrentHealth <= 0;
    public bool IsAlive => !IsDead;

    public HeroAnimatorHandler AnimatorHandler { get; private set; }
    private HeroStateMachine stateMachine;

    [HideInInspector] public GridTile currentTile;
    public float moveSpeed = 2f;

    private bool isInCombat = false;
    private bool hasSpawned = false;

    public GameObject healthBarPrefab;
    public Transform healthBarAnchor;
    private HeroHealthBarUI healthBarUIInstance;
    private Quaternion originalRotation;
    [SerializeField] private GameObject contextMenuPrefab;
    private GameObject activeMenu;
    public bool IsInBattle = false;

    [SerializeField] private Transform contextAnchor; // Where the menu spawns
    private PlayerNetworkState ownerPlayerState;
  

    private void Awake()
    {
        AnimatorHandler = GetComponent<HeroAnimatorHandler>();
        stateMachine = GetComponent<HeroStateMachine>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            if (heroData == null)
                Debug.LogError("❌ HeroData missing on server instance!");

            ApplyFusionStats(); // ✅ Apply once here only

            moveSpeed = heroData.moveSpeed;

            if (currentTile != null)
                SnapToTileY(currentTile);

            Debug.Log($"🟢 [{Faction}] {heroData.heroName} {starLevel}★ spawned at {GridPosition} (Owner: {OwnerClientId}) with {CurrentHealth} HP, {attack} ATK");

            if (IsAlive)
            {
                GetComponent<AICombatController>()?.TickAI();
            }
            ownerPlayerState = PlayerNetworkState.GetPlayerByClientId(OwnerClientId);
        }

        hasSpawned = true;

        if (healthBarPrefab != null && healthBarAnchor != null)
        {
            GameObject hb = Instantiate(healthBarPrefab, healthBarAnchor.position, Quaternion.identity, healthBarAnchor);
            healthBarUIInstance = hb.GetComponent<HeroHealthBarUI>();

            if (healthBarUIInstance != null)
            {
                healthBarUIInstance.Init(CurrentHealth);
                healthBarUIInstance.SetHealth(CurrentHealth);
            }
        }

        currentHealth.OnValueChanged += OnHealthChanged;
    }

    private void OnDestroy()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        if (healthBarUIInstance != null)
        {
            healthBarUIInstance.SetHealth(newValue);
        }
    }

    public void SetFaction(Faction f)
    {
        if (!IsServer)
        {
            Debug.LogWarning("⚠️ Only the server can assign factions.");
            return;
        }

        faction.Value = f;
    }

    public void SnapToTileY(GridTile tile)
    {
        if (tile == null) return;

        if (currentTile != null)
        {
            currentTile.RemoveUnit();
        }

        currentTile = tile;
        GridPosition = tile.GridPosition;

        Vector3 pos = tile.transform.position;
        pos.y += 0.1f;
        transform.position = pos;
        originalRotation = transform.rotation;

        tile.AssignUnit(this);
    }

    public void RemoveFromTile()
    {
        if (currentTile != null)
        {
            currentTile.RemoveUnit();
            currentTile = null;
        }
    }

    public void ApplyDamage(int amount)
    {
        if (!IsAlive || amount <= 0) return;

        currentHealth.Value = Mathf.Max(0, currentHealth.Value - amount);
        Debug.Log($"💥 {heroData.heroName} took {amount} damage → {currentHealth.Value} HP");

        if (currentHealth.Value <= 0)
            Die();
    }

    public void TakeDamage(int amount)
    {
        Debug.Log($"🎯 {heroData.heroName} received {amount} damage.");
        ApplyDamage(amount);
    }

    public void Die()
    {
        if (hasDied) return;

        Debug.Log($"☠️ {heroData.heroName} has died from hero unit.");
        hasDied = true;
        currentHealth.Value = 0;

        stateMachine?.Die();
        RemoveFromTile();

        if (IsServer)
        {
            BattleManager.Instance?.UnregisterUnit(this);
        }
    }

    public void SetCombatState(bool enabled)
    {
        isInCombat = enabled;
    }

    public IEnumerator TeleportBackToHomeTile()
    {
        RestoreHealthToMax();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (currentTile == null)
        {
            Debug.LogWarning($"⚠️ {name} has no CurrentTile to return to.");
            yield break;
        }

        transform.position = currentTile.transform.position + Vector3.up * 0.5f;
        transform.rotation = originalRotation;

        AnimatorHandler?.PlayIdle();
        yield return null;
    }

    public void SnapToGroundedTile()
    {
        if (currentTile == null) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    public void RestoreHealthToMax()
    {
        if (!IsServer) return;

        currentHealth.Value = heroData.maxHealth * GetFusionMultiplier();
        Debug.Log($"❤️‍🩹 {heroData.heroName}'s health restored to {currentHealth.Value}.");
    }

    public void SetHealthBarVisible(bool visible)
    {
        if (healthBarUIInstance != null)
        {
            healthBarUIInstance.gameObject.SetActive(visible);
        }
    }

    public void InitFromDeck(HeroCardInstance cardInstance)
    {
        heroData = cardInstance.baseHero;
        starLevel = cardInstance.starLevel;
        // Fusion stats are applied in OnNetworkSpawn
    }

    private void ApplyFusionStats()
    {
        float multiplier = GetFusionMultiplier();
        currentHealth.Value = heroData.maxHealth * multiplier;
        attack = heroData.attackDamage * multiplier;
    }

    private float GetFusionMultiplier()
    {
        return starLevel switch
        {
            1 => 1.0f,
            2 => 2.0f,
            3 => 4.0f,
            _ => 1.0f
        };
    }
    public int GetSellValue()
    {
        return starLevel switch
        {
            1 => 1,
            2 => 3,
            3 => 5,
            _ => 1
        };
    }

    public void SetInBattle(bool value)
    {
        IsInBattle = value;
    }
    public void OnRightClick()
    {
        if (!IsOwner || IsInBattle || activeMenu != null) return;
        Debug.Log("🟢 Right-click registered on hero");
        ShowContextMenu();
    }

    public void OnLongPress()
    {
        if (!IsOwner || IsInBattle || activeMenu != null) return;
        ShowContextMenu();
    }

    private void ShowContextMenu()
    {
        if (contextMenuPrefab == null || contextAnchor == null)
        {
            Debug.LogWarning($"❌ Missing context menu references on {gameObject.name}");
            return;
        }
        Debug.Log("Coming to show context menu");
        Vector3 spawnPos = contextAnchor.position + new Vector3(0, 0.5f, 0);
        activeMenu = Instantiate(contextMenuPrefab, contextAnchor.position, Quaternion.identity, contextAnchor);
        var menu = activeMenu.GetComponent<UnitContextMenuUI>();
        menu.Init(this);
    }
    [ServerRpc(RequireOwnership = false)]
    public void RequestReturnToDeckServerRpc()
    {
        if (IsInBattle || ownerPlayerState == null)
            return;

        if (!ownerPlayerState.PlayerDeck.CanAddCard())
            return;

        ownerPlayerState.PlayerDeck.AddCardFromUnit(this);

        currentTile?.RemoveUnit();
        NetworkObject.Despawn();
    }
    [ServerRpc(RequireOwnership = false)]
    public void RequestSellFromGridServerRpc()
    {
        if (IsInBattle || ownerPlayerState == null)
            return;

        int refund = GetSellValue(); // This calculates value based on starLevel
        ownerPlayerState.GoldManager.AddGold(refund);

        currentTile?.RemoveUnit();
        NetworkObject.Despawn();
    }

}
