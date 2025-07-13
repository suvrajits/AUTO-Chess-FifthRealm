using Unity.Netcode;
using UnityEngine;

public class HeroCombatController : NetworkBehaviour
{
    [Header("Projectile Settings")]
    public GameObject arrowPrefab;
    public GameObject mageFireballPrefab;
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
        Debug.Log("[HeroCombat] On Attack Event is triggered");
        /*if (currentTarget == null || !currentTarget.IsAlive)
        {
            Debug.LogWarning("[HeroCombat] ⚠️ No valid target — projectile not fired");
            return;
        }*/
        if (currentTarget == null)
        {
            Debug.LogWarning("[HeroCombat] ❌ currentTarget is NULL");
            return;
        }

        if (!currentTarget.IsAlive)
        {
            Debug.LogWarning($"[HeroCombat] ❌ currentTarget '{currentTarget.name}' is not alive");
            return;
        }

        Vector3 start = projectileSpawnPoint.position;
        Vector3 end = currentTarget.GetHitPoint();

        bool isMage = owner.heroData.heroClass == HeroClass.Mage;
        Debug.Log($"[HeroCombat] HeroClass = {owner.heroData.heroClass}, isMage={isMage}");
        GameObject projectilePrefab = isMage ? mageFireballPrefab : arrowPrefab;

        if (projectilePrefab == null)
        {
            Debug.LogError("[HeroCombat] ❌ Projectile prefab missing for hero class.");
            return;
        }

        if (IsHost)
        {
            GameObject proj = Instantiate(projectilePrefab, start, Quaternion.identity);

            if (isMage)
                proj.GetComponent<MagicProjectile>()?.Launch(start, end);
            else
                proj.GetComponent<ArrowProjectile>()?.Launch(start, end, currentTarget);
        }

        // 🔁 Sync across clients
        SpawnProjectileClientRpc(start, end, isMage);
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

    [ClientRpc]
    private void SpawnProjectileClientRpc(Vector3 start, Vector3 end, bool isMage)
    {
        Debug.Log("[HeroCombat] entered into projectile spawning");
        if (IsHost) return;

        Debug.Log("[HeroCombat] entered into spawning ball / arrow");

        GameObject prefab = isMage ? mageFireballPrefab : arrowPrefab;
        if (prefab == null)
        {
            Debug.LogError("[HeroCombat] ❌ Missing projectile prefab for client");
            return;
        }

        GameObject proj = Instantiate(prefab, start, Quaternion.identity);
        if (isMage)
            proj.GetComponent<MagicProjectile>()?.Launch(start, end);
        else
            proj.GetComponent<ArrowProjectile>()?.Launch(start, end, null);
    }
}
