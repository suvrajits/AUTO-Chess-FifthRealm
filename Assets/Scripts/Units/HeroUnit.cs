// ✅ HeroUnit.cs - Final Clean Version
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(HeroAnimatorHandler), typeof(HeroStateMachine))]
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

    private void Start()
    {
        AnimatorHandler = GetComponent<HeroAnimatorHandler>();
        stateMachine = GetComponent<HeroStateMachine>();

        CurrentHealth = heroData.maxHealth;
        SnapToTileY();

        if (IsServer && !IsDead && BattleManager.Instance.CurrentPhase == GamePhase.Battle)
        {
            stateMachine.EnterCombat();
        }
    }

    public void SetFaction(Faction f)
    {
        if (IsServer)
            faction.Value = f;
    }

    public void ApplyDamage(float amount)
    {
        if (IsDead) return;

        CurrentHealth -= amount;

        if (CurrentHealth <= 0f)
            stateMachine.Die();
    }

    public void BeginBattle()
    {
        if (IsServer && !IsDead)
            stateMachine.EnterCombat();
    }

    private void SnapToTileY()
    {
        if (PlayerNetworkState.AllPlayerGrids.TryGetValue(OwnerClientId, out var gridManager))
        {
            if (gridManager.TryGetTile(GridPosition, out var tile))
            {
                var pos = transform.position;
                pos.y = tile.transform.position.y + 0.5f;
                transform.position = pos;
            }
        }
    }
}
