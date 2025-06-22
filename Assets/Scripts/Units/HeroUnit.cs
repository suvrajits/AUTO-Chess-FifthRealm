using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(HeroStateMachine), typeof(HeroAnimatorHandler), typeof(AICombatController))]
public class HeroUnit : NetworkBehaviour
{
    public HeroData heroData;
    public Vector2Int GridPosition { get; set; }

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
    private AICombatController aiController;

    public GridTile currentTile;
    public float moveSpeed = 2f;

    private bool isInCombat = false;

    private void Awake()
    {
        aiController = GetComponent<AICombatController>();
    }

    private void Start()
    {
        AnimatorHandler = GetComponent<HeroAnimatorHandler>();
        stateMachine = GetComponent<HeroStateMachine>();

        if (heroData == null)
        {
            Debug.LogError("❌ Missing HeroData!");
            return;
        }

        if (currentTile != null)
            SnapToTileY(currentTile);

        CurrentHealth = heroData.maxHealth;
        moveSpeed = heroData.moveSpeed;

        Debug.Log($"🟢 [{Faction}] {heroData.heroName} spawned at {GridPosition} with {CurrentHealth} HP");

        if (IsServer && IsAlive)
        {
            stateMachine.EnterCombat(); // Skip GamePhase for now, re-add later
        }

        Debug.Log($"[{heroData.heroName}] START on {(IsServer ? "Server" : "Client")} → Faction: {Faction}");
    }

    public void SetFaction(Faction f)
    {
        if (!IsServer)
        {
            Debug.LogWarning("⚠️ Only server can set faction");
            return;
        }

        faction.Value = f;
    }

    public void ApplyDamage(float amount)
    {
        if (IsDead) return;

        CurrentHealth -= amount;
        Debug.Log($"💥 {heroData.heroName} took {amount} damage → {CurrentHealth} HP");

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    public void TakeDamage(int amount)
    {
        ApplyDamage(amount); // Compatibility with AICombatController
    }

    public void Die()
    {
        CurrentHealth = 0;
        Debug.Log($"☠️ {heroData.heroName} has died.");

        if (stateMachine != null)
            stateMachine.Die();

        RemoveFromTile();

        if (BattleManager.Instance != null)
            BattleManager.Instance.UnregisterUnit(this);
    }

    public void SetCombatState(bool enabled)
    {
        isInCombat = enabled;

        if (enabled)
            stateMachine.EnterCombat();
    }

    public void PerformCombatTick()
    {
        if (!IsServer || !IsAlive || !isInCombat) return;
        aiController.TickAI();
    }

    public void SnapToTileY(GridTile tile)
    {
        if (tile == null) return;

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
}
