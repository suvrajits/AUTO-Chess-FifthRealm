using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(HeroStateMachine), typeof(HeroAnimatorHandler))]
public class HeroUnit : NetworkBehaviour
{
    public HeroData heroData;
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

    public GameObject healthBarPrefab;             // Assign in Inspector
    public Transform healthBarAnchor;              // Assign in Inspector
    private HeroHealthBarUI healthBarUIInstance;   // Internal reference
    private Quaternion originalRotation;
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

            currentHealth.Value = heroData.maxHealth;
            moveSpeed = heroData.moveSpeed;

            if (currentTile != null)
                SnapToTileY(currentTile);

            Debug.Log($"🟢 [{Faction}] {heroData.heroName} spawned at {GridPosition} (Owner: {OwnerClientId}) with {CurrentHealth} HP");

            if (IsAlive)
            {
                GetComponent<AICombatController>()?.TickAI();
            }
        }

        hasSpawned = true;

        // ✅ Instantiate and initialize health bar
        if (healthBarPrefab != null && healthBarAnchor != null)
        {
            GameObject hb = Instantiate(healthBarPrefab, healthBarAnchor.position, Quaternion.identity, healthBarAnchor);
            healthBarUIInstance = hb.GetComponent<HeroHealthBarUI>();

            if (healthBarUIInstance != null)
            {
                healthBarUIInstance.Init(heroData.maxHealth);
                healthBarUIInstance.SetHealth(CurrentHealth);
                //SetHealthBarVisible(false);
            }
        }

        // ✅ Subscribe to health updates for local UI
        currentHealth.OnValueChanged += OnHealthChanged;
    }

    private void OnDestroy()
    {
        if (currentHealth != null)
        {
            currentHealth.OnValueChanged -= OnHealthChanged;
        }
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
        //SetHealthBarVisible(false);
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

        currentHealth.Value = heroData.maxHealth;
        Debug.Log($"❤️‍🩹 {heroData.heroName}'s health restored to {currentHealth.Value}.");
    }
    public void SetHealthBarVisible(bool visible)
    {
        if (healthBarUIInstance != null)
        {
            healthBarUIInstance.gameObject.SetActive(visible);
        }
    }
}
