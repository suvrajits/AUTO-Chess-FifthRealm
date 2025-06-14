using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(HeroStateMachine), typeof(HeroAnimatorHandler))]
public class HeroUnit : NetworkBehaviour
{
    public HeroData heroData;

    public Vector2Int GridPosition { get; set; }

    [SerializeField]
    private Faction faction;
    public Faction Faction => faction;

    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    public HeroAnimatorHandler AnimatorHandler { get; private set; }
    private HeroStateMachine stateMachine;

    public bool IsBattleActive => BattleManager.Instance != null && BattleManager.Instance.IsBattleOngoing;

    public bool IsAlive => CurrentHealth > 0;

    private void Start()
    {
        if (heroData == null)
        {
            Debug.LogError("Missing HeroData!");
            return;
        }

        CurrentHealth = heroData.maxHealth;
        AnimatorHandler = GetComponent<HeroAnimatorHandler>();
        stateMachine = GetComponent<HeroStateMachine>();

        Debug.Log($"[{faction}] {heroData.heroName} spawned at {GridPosition} with {CurrentHealth} HP");
    }

    /// <summary>
    /// Server-only method to assign faction ownership
    /// </summary>
    public void SetFaction(Faction f)
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only the server can set faction!");
            return;
        }

        faction = f;
    }

    /// <summary>
    /// Applies damage and triggers death if HP <= 0
    /// </summary>
    public void ApplyDamage(float amount)
    {
        if (IsDead) return;

        CurrentHealth -= amount;
        Debug.Log($"{heroData.heroName} took {amount} damage → {CurrentHealth} HP left");

        if (CurrentHealth <= 0f)
        {
            stateMachine.Die();
        }
    }


    public void BeginBattle()
    {
        if (!IsServer || !IsAlive) return;

        // Initialize state machine here
        GetComponent<HeroStateMachine>()?.EnterCombat();
    }
}
