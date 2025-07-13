using Unity.Netcode;
using UnityEngine;

public class HeroCombatController : NetworkBehaviour
{
    [Header("Projectile Settings")]
    public GameObject arrowPrefab;
    public Transform projectileSpawnPoint;

    private HeroUnit owner;
    private HeroUnit currentTarget;
    private bool IsHost => NetworkManager.Singleton.IsHost;

    public void Init(HeroUnit hero)
    {
        owner = hero;
    }

    public void SetTarget(HeroUnit target)
    {
        currentTarget = target;
        Debug.Log($"[HeroCombat] 🎯 Target set: {target} (GO Name: {target?.gameObject.name}, Active: {target?.gameObject.activeSelf})");
    }

    public void TriggerAttackAnimation()
    {
        Debug.Log("[HeroCombat] 🔁 Attack animation triggered");
        GetComponent<Animator>()?.SetTrigger("Attack");
    }

    public void OnAttackEventTriggered()
    {
        if (currentTarget == null || !currentTarget.IsAlive)
        {
            Debug.LogWarning("[HeroCombat] ⚠️ No valid target — arrow not fired");
            return;
        }

        Vector3 start = projectileSpawnPoint.position;
        Vector3 end = currentTarget.GetHitPoint();

        if (IsHost)
        {
            GameObject arrow = Instantiate(arrowPrefab, start, Quaternion.identity);
            arrow.GetComponent<ArrowProjectile>()?.Launch(start, end, currentTarget);
        }

        SpawnArrowClientRpc(start, end);
    }

    [ClientRpc]
    private void SpawnArrowClientRpc(Vector3 start, Vector3 end)
    {
        if (IsHost) return; // Host already spawned it locally

        if (arrowPrefab == null)
        {
            Debug.LogError("[HeroCombatController] ❌ ArrowPrefab is not assigned");
            return;
        }

        Debug.Log("[HeroCombatController] ✅ Spawning arrow on CLIENT");
        GameObject arrow = Instantiate(arrowPrefab, start, Quaternion.identity);
        arrow.GetComponent<ArrowProjectile>()?.Launch(start, end, null);
    }
}
