using UnityEngine;
using UnityEngine.UI;

public class TraitIconPulseHandler : MonoBehaviour
{
    public float pulseSpeed = 3f;
    public float minAlpha = 0.3f;
    public float maxAlpha = 1f;

    private Image image;
    private bool isActive = false;
    private Color originalColor;

    private void Awake()
    {
        image = GetComponent<Image>();
        if (image != null)
        {
            originalColor = image.color;
        }
    }

    private void Update()
    {
        if (!isActive || image == null)
            return;

        float t = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f; // oscillate between 0 and 1
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
        image.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
    }

    public void SetActive(bool active)
    {
        isActive = active;

        if (!isActive && image != null)
        {
            // Restore full alpha when disabled
            image.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
        }
    }
}
