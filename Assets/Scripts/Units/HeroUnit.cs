using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(HeroStateMachine), typeof(HeroAnimatorHandler))]
public class HeroUnit : NetworkBehaviour
{
    public HeroData heroData;
    public Vector2Int GridPosition { get; set; }

    private NetworkVariable<Faction> faction = new(
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

    private GridTile assignedTile;

    public void OnSpawnInitialized(GridTile tile, HeroData data, Faction? fact = null)
    {
        if (tile == null || data == null)
        {
            Debug.LogError("❌ tile or data is null in OnSpawnInitialized");
            return;
        }

        heroData = data;
        GridPosition = tile.GridPosition;
        assignedTile = tile;

        // Assign required components
        if (AnimatorHandler == null) AnimatorHandler = GetComponent<HeroAnimatorHandler>();
        if (stateMachine == null) stateMachine = GetComponent<HeroStateMachine>();

        if (AnimatorHandler == null || stateMachine == null)
        {
            Debug.LogError("❌ Missing HeroAnimatorHandler or HeroStateMachine");
            return;
        }

        if (IsServer)
            faction.Value = fact ?? Faction.Neutral;

        CurrentHealth = heroData.maxHealth;

        // Snap to tile Y position
        transform.position = tile.transform.position + Vector3.up * 0.01f;

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        Debug.Log($"✅ [{Faction}] {heroData.heroName} initialized at {GridPosition} with {CurrentHealth} HP");

        if (IsServer && BattleManager.Instance.CurrentPhase == GamePhase.Battle)
            stateMachine.EnterCombat();
    }



    public void ApplyDamage(float amount)
    {
        if (IsDead) return;

        CurrentHealth -= amount;
        Debug.Log($"💥 {heroData.heroName} took {amount} damage → {CurrentHealth} HP");

        if (CurrentHealth <= 0f)
            stateMachine.Die();
    }

    public void BeginBattle()
    {
        if (IsServer && IsAlive)
            stateMachine.EnterCombat();
    }

    public void SetFaction(Faction f)
    {
        if (IsServer) faction.Value = f;
    }
}
