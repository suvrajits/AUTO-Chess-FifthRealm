using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(HeroStateMachine), typeof(HeroAnimatorHandler))]
public class HeroUnit : NetworkBehaviour
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

    private bool isInCombat;
    public bool IsInCombat => isInCombat;
    private bool hasSpawned = false;

    public GameObject healthBarPrefab;
    public Transform healthBarAnchor;
    private HeroHealthBarUI healthBarUIInstance;
    private Quaternion originalRotation;

    public Transform uiAnchor;
    public Transform GetUIAnchor() => uiAnchor;
    public GameObject contextMenuPrefab;
    public Transform contextMenuAnchor;
    private Coroutine visualCheckCoroutine;


    [HideInInspector] public UnitContextMenuUI contextMenuInstance;
    private float tapCooldown = 0.25f;
    private float lastTapTime = -1f;
    public BuffManager BuffManager { get; private set; }
    private bool hasLifestealAura = false;
    private float lifestealPercentage = 0f;
    private TraitEffectHandler traitEffectHandler;

    private float bonusAttack = 0f;
    private float bonusMaxHealth = 0f;
    public GameObject poisonStackUIPrefab;
    private void Awake()
    {
        AnimatorHandler = GetComponent<HeroAnimatorHandler>();
        stateMachine = GetComponent<HeroStateMachine>();
        BuffManager = GetComponent<BuffManager>();
        if (BuffManager == null)
            BuffManager = gameObject.AddComponent<BuffManager>();
    }
    private void Update()
    {
        // Skip if not owned by this player
        if (!IsOwner) return;

#if UNITY_EDITOR || UNITY_STANDALONE
        // Handle mouse click
        if (Input.GetMouseButtonDown(0) && Time.time - lastTapTime > tapCooldown)
        {
            HandleTapOrClick(Input.mousePosition);
            lastTapTime = Time.time;
        }
#elif UNITY_IOS || UNITY_ANDROID
    // Handle mobile tap
    if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began && Time.time - lastTapTime > tapCooldown)
    {
        HandleTapOrClick(Input.GetTouch(0).position);
        lastTapTime = Time.time;
    }
#endif
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            if (heroData == null)
                Debug.LogError("❌ HeroData missing on server instance!");

            ApplyFusionStats();
            moveSpeed = heroData.moveSpeed;

            if (currentTile != null)
                SnapToTileY(currentTile);

            Debug.Log($"🟢 [{Faction}] {heroData.heroName} {starLevel}★ spawned at {GridPosition} (Owner: {OwnerClientId}) with {CurrentHealth} HP, {attack} ATK");

            if (IsAlive)
                GetComponent<AICombatController>()?.TickAI();
        }

        hasSpawned = true;
        if (!IsDead && AnimatorHandler != null)
        {
            AnimatorHandler.PlayIdle();
        }

        // 🔵 Setup Health Bar
        if (healthBarPrefab != null && healthBarAnchor != null)
        {
            GameObject hb = Instantiate(healthBarPrefab, healthBarAnchor.position, Quaternion.identity, healthBarAnchor);
            healthBarUIInstance = hb.GetComponent<HeroHealthBarUI>();

            if (healthBarUIInstance != null)
            {
                healthBarUIInstance.Init(CurrentHealth);
                healthBarUIInstance.SetHealth(CurrentHealth);
            }
            else
            {
                Debug.LogWarning("⚠️ HeroHealthBarUI component missing.");
            }
        }

        // 🔵 Setup Context Menu
        if (contextMenuPrefab != null && contextMenuAnchor != null)
        {
            GameObject ui = Instantiate(contextMenuPrefab, contextMenuAnchor.position, Quaternion.identity, contextMenuAnchor);
            contextMenuInstance = ui.GetComponent<UnitContextMenuUI>();

            if (contextMenuInstance != null)
            {
                contextMenuInstance.AttachToUnit(this);
                contextMenuInstance.Init(this); // Assigns camera and buttons
                contextMenuInstance.HideMenu();
            }
            else
            {
                Debug.LogWarning("⚠️ UnitContextMenuUI component missing from prefab.");
            }
        }

        currentHealth.OnValueChanged += OnHealthChanged;

        traitEffectHandler = GetComponent<TraitEffectHandler>();
        if (traitEffectHandler == null)
            traitEffectHandler = gameObject.AddComponent<TraitEffectHandler>();

        traitEffectHandler.Initialize(this, heroData.traits);
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
            currentTile.RemoveUnit();

        currentTile = tile;
        GridPosition = tile.GridPosition;

        Vector3 pos = tile.transform.position;
        pos.y += 0.1f;
        transform.position = pos;

        // ✅ Only capture this once, not overwrite it every round
        if (originalRotation == Quaternion.identity)
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

    public void TakeDamage(int amount, HeroUnit attacker = null)
    {
        if (!IsAlive || amount <= 0) return;

        if (BuffManager != null)
        {
            float absorbed = BuffManager.AbsorbDamage(amount);
            amount = Mathf.RoundToInt(absorbed); // Remaining after shield
        }

        Debug.Log($"🎯 {heroData.heroName} received {amount} damage.");
        ApplyDamage(amount);

        // 🛡 Trigger Raksha Reflect Trait (if applicable)
        GetComponent<TraitEffectHandler>()?.OnDamaged(attacker, amount);
    }


    public void Die()
    {
        if (hasDied) return;

        Debug.Log($"☠️ {heroData.heroName} has died from hero unit.");
        hasDied = true;
        currentHealth.Value = 0;
        SetCombatState(false);
        stateMachine?.Die();

    }


    public void SetCombatState(bool enabled)
    {
        isInCombat = enabled;

        var ai = GetComponent<AICombatController>();
        if (ai != null)
        {
            ai.SetBattleMode(enabled);
        }
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
    [ServerRpc(RequireOwnership = false)]
    public void RequestReturnToDeckServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (OwnerClientId != senderId)
        {
            Debug.LogWarning("🚫 Cannot return a unit you don't own.");
            return;
        }

        var player = PlayerNetworkState.GetPlayerByClientId(senderId);
        if (player == null)
        {
            Debug.LogWarning("⚠️ Player not found.");
            return;
        }

        // Check if deck has room BEFORE proceeding
        if (!player.PlayerDeck.HasRoom())
        {
            Debug.LogWarning($"⚠️ Cannot return {heroData.heroName} — deck is full.");
            return;
        }

        var cardInstance = new HeroCardInstance
        {
            baseHero = heroData,
            starLevel = starLevel
        };

        // Add to deck and sync
        player.PlayerDeck.AddCard(cardInstance);
        player.PlayerDeck.SyncDeckToClient(senderId);

        // Remove from board
        BattleManager.Instance.UnregisterUnit(this);
        GetComponent<NetworkObject>().Despawn(true);

        if (player.PlacedUnitCount.Value > 0)
        {
            player.PlacedUnitCount.Value--;
            Debug.Log($"📉 Returned to deck. New unit count: {player.PlacedUnitCount.Value}");
        }

        Debug.Log($"📦 Returned {heroData.heroName} to deck.");

    }


    [ServerRpc(RequireOwnership = false)]
    public void RequestSellFromGridServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("trying to sell");
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (OwnerClientId != senderId)
        {
            Debug.LogWarning("🚫 Cannot sell another player's unit.");
            return;
        }

        var player = PlayerNetworkState.GetPlayerByClientId(senderId);
        if (player == null)
        {
            Debug.LogWarning("⚠️ Player not found.");
            return;
        }

        int refund = starLevel switch
        {
            1 => heroData.cost,
            2 => heroData.cost * 3,
            3 => heroData.cost * 5,
            _ => heroData.cost
        };

        player.GoldManager.AddGold(refund);
        BattleManager.Instance.UnregisterUnit(this);
        GetComponent<NetworkObject>().Despawn(true);

        if (player.PlacedUnitCount.Value > 0)
        {
            player.PlacedUnitCount.Value--;
            Debug.Log($"📉 Unit sold. New unit count: {player.PlacedUnitCount.Value}");
        }

        Debug.Log($"💰 Player {senderId} sold {heroData.heroName} for {refund}g");
    }
    public void MarkAsDeadForBattle()
    {
        hasDied = true;

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        SetHealthBarVisible(false);
        SetCombatState(false);
    }

    public void ReviveAndReturnToTile(GridTile tile)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        AnimatorHandler?.ResetAllTriggers();
        AnimatorHandler?.SetTrigger("hasRecovered");

        SetHealthBarVisible(true);

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = true;

        SetCombatState(false);

        

        // ✅ Health bar restore (if null)
        if (healthBarUIInstance == null && healthBarPrefab != null && healthBarAnchor != null)
        {
            GameObject hb = Instantiate(healthBarPrefab, healthBarAnchor.position, Quaternion.identity, healthBarAnchor);
            healthBarUIInstance = hb.GetComponent<HeroHealthBarUI>();
            healthBarUIInstance?.Init(CurrentHealth);
        }

        if (healthBarUIInstance != null)
        {
            healthBarUIInstance.SetHealth(CurrentHealth);
            healthBarUIInstance.gameObject.SetActive(true);
        }

        // ✅ Snap and reset rotation
        SnapToTileY(tile);
        transform.rotation = Quaternion.Euler(0, 0, 0); // Or originalRotation if stored

        // ✅ Now safe to clear death flag AFTER damage has been applied
        hasDied = false;
        
        RestoreHealthToMax();
        if (IsServer)
        {
            ShowRevivedClientRpc();
            ResetAnimatorClientRpc();
        }
            
    }
    public void StopAllCombatCoroutines()
    {
        var ai = GetComponent<AICombatController>();
        if (ai != null)
        {
            ai.StopAllCoroutines(); // ✅ Ensure delayed attacks or repeated attacks stop
        }
    }


    [ClientRpc]
    public void HideCorpseClientRpc()
    {
        // ✅ Safeguard: only hide if we’re actively in a battle phase
        if (!BattleManager.Instance || BattleManager.Instance.CurrentPhase != GamePhase.Battle)
        {
            Debug.Log($"🛑 Skipping corpse hide — not in battle phase: {BattleManager.Instance?.CurrentPhase}");
            return;
        }

        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer != null)
                renderer.enabled = false;
        }

        if (healthBarUIInstance != null)
            healthBarUIInstance.gameObject.SetActive(false);

        Debug.Log($"👻 Corpse hidden for {heroData.heroName}");
    }


    [ClientRpc]
    private void ShowRevivedClientRpc()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = true;

        if (healthBarUIInstance != null)
            healthBarUIInstance.gameObject.SetActive(true);
    }
    [ClientRpc]
    private void ResetAnimatorClientRpc()
    {
        if (!AnimatorHandler) return;

        Debug.Log($"🎬 Resetting animator on client for {heroData.heroName}");

        AnimatorHandler.ResetAllTriggers();
        AnimatorHandler.SetTrigger("hasRecovered");
    }
    private void HandleTapOrClick(Vector2 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider != null && hit.collider.gameObject == this.gameObject)
            {
                if (contextMenuInstance != null)
                {
                    Debug.Log($"👆 Tapped {heroData.heroName} — showing context menu");
                    contextMenuInstance.ShowMenu();
                }
                else
                {
                    Debug.LogWarning("❌ contextMenuInstance is null");
                }
            }
        }
    }
    public void SetShieldVisual(bool enabled)
    {
        // OPTIONAL: You can assign a GameObject like a glowing particle ring
        // Example:
        Transform shieldFX = transform.Find("ShieldFX");
        if (shieldFX != null)
            shieldFX.gameObject.SetActive(enabled);
    }
    public void ApplyPoisonTest()
    {
        BuffManager?.ApplyBuff(BuffType.Poison, 5, 3f);
    }
    public void EnableLifesteal(float percentage)
    {
        lifestealPercentage = percentage;
        hasLifestealAura = true;
        Debug.Log($"🩸 {heroData.heroName} enabled lifesteal aura ({percentage * 100f}% of damage).");
    }

    public void DisableLifesteal()
    {
        lifestealPercentage = 0f;
        hasLifestealAura = false;
        Debug.Log($"🛑 {heroData.heroName} disabled lifesteal aura.");
    }
    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0) return;

        float prevHP = currentHealth.Value;
        currentHealth.Value = Mathf.Min(currentHealth.Value + amount, heroData.maxHealth * GetFusionMultiplier());
        float healed = currentHealth.Value - prevHP;


        Debug.Log($"💚 {heroData.heroName} healed {amount}. Current HP: {currentHealth.Value}");
    }
    public bool HasLifesteal() => hasLifestealAura;

    public float GetLifestealPercentage() => lifestealPercentage;
    public void AddBonusAttack(float value)
    {
        bonusAttack += value;
        attack += value;
    }

    public void AddBonusMaxHealth(float value)
    {
        bonusMaxHealth += value;
        currentHealth.Value += value;
    }
}
