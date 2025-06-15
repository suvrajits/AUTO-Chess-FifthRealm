using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(HeroStateMachine), typeof(HeroAnimatorHandler))]
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

    private void Start()
    {
        AnimatorHandler = GetComponent<HeroAnimatorHandler>();
        stateMachine = GetComponent<HeroStateMachine>();

        if (heroData == null)
        {
            Debug.LogError("❌ Missing HeroData!");
            return;
        }

        // 🟡 Snap to tile height
        SnapToTileY();

        CurrentHealth = heroData.maxHealth;
        Debug.Log($"🟢 [{Faction}] {heroData.heroName} spawned at {GridPosition} with {CurrentHealth} HP");

        if (IsServer && IsAlive && BattleManager.Instance.CurrentPhase == GamePhase.Battle)
        {
            stateMachine.EnterCombat();
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
            stateMachine.Die();
        }
    }

    public void BeginBattle()
    {
        if (IsServer && IsAlive)
        {
            Debug.Log($"[] {heroData.heroName} BeginBattle() called");
            stateMachine.EnterCombat();
        }
    }

    private void SnapToTileY()
    {
        if (GridManager.Instance != null && GridManager.Instance.tileMap.TryGetValue(GridPosition, out var tile))
        {
            Vector3 pos = transform.position;
            pos.y = tile.transform.position.y;
            transform.position = pos;
        }
    }
}
