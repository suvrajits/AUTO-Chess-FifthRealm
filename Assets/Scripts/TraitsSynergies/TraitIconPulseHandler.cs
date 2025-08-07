using UnityEngine;
using UnityEngine.UI;

public class TraitIconPulseHandler : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public float pulseScale = 1.3f;
    public bool isActive = false;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        if (!isActive) return;

        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.15f;
        transform.localScale = baseScale * scale;
    }

    public void SetActive(bool active)
    {
        isActive = active;
        if (!isActive)
        {
            transform.localScale = baseScale;
        }
    }
}
