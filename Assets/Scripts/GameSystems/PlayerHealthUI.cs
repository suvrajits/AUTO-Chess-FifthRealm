using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image healthFillImage;  // This should be a filled image with Fill Method: Horizontal
    [SerializeField] private TMP_Text healthText;

    public int maxHealth = 20;
    void Awake()
    {
        if (healthFillImage != null)
            healthFillImage.fillAmount = 1f;
    }
    public void UpdateHealth(int current, int max)
    {
        maxHealth = max;

        float fillAmount = Mathf.Clamp01((float)current / maxHealth);
        healthFillImage.fillAmount = fillAmount;

        if (healthText != null)
            healthText.text = $"HP: {current}";
    }
}
