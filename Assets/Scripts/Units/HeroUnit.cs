using UnityEngine;
using Unity.Netcode;

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

    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;
    public bool IsAlive => !IsDead;

    public HeroAnimatorHandler AnimatorHandler { get; private set; }
    private HeroStateMachine stateMachine;

    [HideInInspector] public GridTile currentTile;
    public float moveSpeed = 2f;

    private bool isInCombat = false;
    private bool hasSpawned = false;

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

            CurrentHealth = heroData.maxHealth;
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

    public void ApplyDamage(float amount)
    {
        if (!IsAlive || amount <= 0f) return;

        CurrentHealth -= amount;
        Debug.Log($"💥 {heroData.heroName} took {amount} damage → {CurrentHealth} HP");

        if (CurrentHealth <= 0f)
            Die();
    }

    public void TakeDamage(int amount)
    {
        ApplyDamage(amount);
    }

    public void Die()
    {
        if (IsDead) return;

        CurrentHealth = 0;
        Debug.Log($"☠️ {heroData.heroName} has died.");

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

        /*if (enabled)
            stateMachine.EnterCombat();*/
    }
}
