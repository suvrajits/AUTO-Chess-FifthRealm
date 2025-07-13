using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [Header("Flight Settings")]
    public float speed = 10f;

    [Header("Optional FX")]
    public ParticleSystem trailFX;
    public GameObject impactEffectPrefab;

    private Vector3 targetPos;
    private HeroUnit targetUnit;
    private bool isFlying = false;

    public void Launch(Vector3 start, Vector3 dest, HeroUnit target)
    {
        transform.position = start;
        targetPos = dest;
        targetUnit = target;
        isFlying = true;

        // 🔄 Align arrow to face travel direction
        Vector3 dir = (targetPos - start).normalized;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
        else
            Debug.LogWarning("[ArrowProjectile] 🧭 Launch direction was zero");

        // 🔥 Optional trail FX
        if (trailFX != null)
        {
            trailFX.Play();
        }
        else
        {
            Debug.LogWarning("[ArrowProjectile] ⚠️ trailFX not assigned (optional)");
        }

        Debug.Log($"[ArrowProjectile] 🚀 Launch called: start={start}, end={dest}, target={target?.name}");
    }

    void Update()
    {
        if (!isFlying) return;

        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

        // Keep looking toward travel direction
        Vector3 moveDir = (targetPos - transform.position).normalized;
        if (moveDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveDir);

        // Trigger impact when close enough
        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            Impact();
        }
    }

    private void Impact()
    {
        isFlying = false;

        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            Debug.Log("[ArrowProjectile] 💥 No impact VFX assigned (optional)");
        }

        Destroy(gameObject);
    }
}
