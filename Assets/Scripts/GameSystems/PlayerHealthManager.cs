using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerHealthManager : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int startingHealth = 20;

    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(
        20,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private PlayerNetworkState player;
    private PlayerHealthUI healthUI;

    public bool IsDead => CurrentHealth.Value <= 0;

    private void Awake()
    {
        player = GetComponent<PlayerNetworkState>();
    }

    private void Start()
    {
        if (IsOwner)
            healthUI = UIOverlayManager.Instance?.GetHealthUI();
        if (healthUI != null)
        {
            Debug.Log($"✅ Found health UI from overlay for player {OwnerClientId}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Could not find PlayerHealthUI via overlay for player {OwnerClientId}");
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentHealth.Value = startingHealth;
            Debug.Log($"❤️ [Server] Initialized Player {OwnerClientId} with {startingHealth} HP");
        }

        if (IsClient && IsOwner)
        {
            Debug.Log($"👀 Client {OwnerClientId} starting health sync check...");
            StartCoroutine(WaitForSyncedHealth());
        }

        CurrentHealth.OnValueChanged += OnHealthChanged;
    }


    private IEnumerator WaitForSyncedHealth()
    {
        yield return null; // Delay 1 frame for sync

        if (healthUI != null)
        {
            healthUI.UpdateHealth(CurrentHealth.Value, startingHealth);
            Debug.Log($"✅ Health synced: {CurrentHealth.Value} for player {OwnerClientId}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Health UI was still null for player {OwnerClientId}");
        }
    }


    private System.Collections.IEnumerator DelayedHealthBarInit()
    {
        yield return null; // Wait 1 frame

        if (healthUI != null)
            healthUI.UpdateHealth(CurrentHealth.Value, startingHealth);
    }

    private void OnHealthChanged(int oldVal, int newVal)
    {
        if (IsOwner && healthUI != null)
            healthUI.UpdateHealth(newVal, startingHealth);
    }

    /*[ServerRpc(RequireOwnership = false)]
    public void ApplyDamageServerRpc(int damage)
    {
        if (!IsServer) return;

        int newHP = Mathf.Max(CurrentHealth.Value - damage, 0);
        Debug.Log($"💥 [Server] Player {OwnerClientId} took {damage} damage. HP: {CurrentHealth.Value} → {newHP}");

        CurrentHealth.Value = newHP;

        if (newHP <= 0)
        {
            Debug.Log($"💀 [Server] Player {OwnerClientId} reached 0 HP. Attempting elimination...");
            EliminationManager.Instance?.EliminatePlayer(OwnerClientId);
        }
    }*/
    public void ApplyServerDamage(int amount)
    {
        if (!IsServer) return;

        int newHP = Mathf.Max(CurrentHealth.Value - amount, 0);
        Debug.Log($"💥 [Server] Player {OwnerClientId} took {amount} damage. HP: {CurrentHealth.Value} → {newHP}");

        CurrentHealth.Value = newHP;

        if (newHP <= 0 && !player.IsEliminated.Value)
        {
            Debug.Log($"💀 [Server] Player {OwnerClientId} reached 0 HP. Triggering elimination...");
            EliminationManager.Instance?.EliminatePlayer(OwnerClientId);
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(int healAmount)
    {
        if (!IsServer) return;

        int healed = Mathf.Min(CurrentHealth.Value + healAmount, startingHealth);
        Debug.Log($"➕ [Server] Player {OwnerClientId} healed {healAmount}. HP: {CurrentHealth.Value} → {healed}");

        CurrentHealth.Value = healed;
    }

    public override void OnDestroy()
    {
        CurrentHealth.OnValueChanged -= OnHealthChanged;
        base.OnDestroy();
    }
}
