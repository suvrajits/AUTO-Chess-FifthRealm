using UnityEngine;

public class MagicProjectile : MonoBehaviour
{
    public float speed = 25f;
    public float arcHeightFactor = 0.25f;

    public ParticleSystem trailFX;
    public GameObject impactEffectPrefab;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float flightTime;
    private float elapsed;
    private float distance;
    private bool isFlying;

    public void Launch(Vector3 start, Vector3 dest)
    {
        startPos = start;
        targetPos = dest;
        elapsed = 0f;
        isFlying = true;
        distance = Vector3.Distance(start, dest);
        flightTime = distance / speed;

        Vector3 dir = (dest - start).normalized;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        if (trailFX != null) trailFX.Play();
        else Debug.LogWarning("[MagicProjectile] ⚠️ Missing trail FX");

        Debug.Log($"[MagicProjectile] 🔮 Fired from {start} → {dest}");
    }

    void Update()
    {
        if (!isFlying) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / flightTime);

        Vector3 flatPos = Vector3.Lerp(startPos, targetPos, t);
        float arc = Mathf.Sin(t * Mathf.PI) * arcHeightFactor * distance;
        transform.position = flatPos + Vector3.up * arc;

        Vector3 next = Vector3.Lerp(startPos, targetPos, Mathf.Clamp01(t + 0.05f));
        transform.LookAt(next + Vector3.up * arc);

        if (t >= 1f)
            Impact();
    }

    private void Impact()
    {
        isFlying = false;
        if (impactEffectPrefab)
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
