using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [Header("Flight Settings")]
    public float speed = 30f;
    public float arcHeightFactor = 0.15f; // how high the arc rises based on distance

    [Header("Optional FX")]
    public ParticleSystem trailFX;
    public GameObject impactEffectPrefab;

    private Vector3 startPos;
    private Vector3 targetPos;
    private HeroUnit targetUnit;
    private bool isFlying = false;
    private float distance;

    private float totalFlightTime;
    private float elapsedTime;

    public void Launch(Vector3 start, Vector3 dest, HeroUnit target)
    {
        startPos = start;
        targetPos = dest;
        targetUnit = target;
        isFlying = true;

        distance = Vector3.Distance(start, dest);
        totalFlightTime = distance / speed;
        elapsedTime = 0f;

        // 🔄 Align at launch
        Vector3 dir = (targetPos - startPos).normalized;
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

        Debug.Log($"[ArrowProjectile] 🚀 Launch → {startPos} → {targetPos} | Distance: {distance:F2}, Time: {totalFlightTime:F2}s");
    }

    void Update()
    {
        if (!isFlying) return;

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / totalFlightTime);

        // Horizontal (XZ) movement
        Vector3 linearPos = Vector3.Lerp(startPos, targetPos, t);

        // Vertical arc (using sine wave)
        float arc = Mathf.Sin(t * Mathf.PI) * arcHeightFactor * distance;
        Vector3 curvedPos = new Vector3(linearPos.x, linearPos.y + arc, linearPos.z);

        transform.position = curvedPos;

        // Face next position
        if (t < 1f)
        {
            Vector3 next = Vector3.Lerp(startPos, targetPos, Mathf.Clamp01(t + 0.05f));
            transform.LookAt(new Vector3(next.x, next.y + Mathf.Sin((t + 0.05f) * Mathf.PI) * arcHeightFactor * distance, next.z));
        }

        // Hit check
        if (t >= 1f || Vector3.Distance(transform.position, targetPos) < 0.1f)
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
