using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HeroHealthBarUI : MonoBehaviour
{
    [Header("Fill Images")]
    public Image greenFillImage;
    public Image redFillImage;

    private float maxHealth;
    private float currentHealth;

    private Image activeFillImage;

    private void Start()
    {
        // Find NetworkObject in parent
        NetworkObject netObj = GetComponentInParent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogWarning("❗ HeroHealthBarUI: No NetworkObject found in parent.");
            return;
        }

        // Invert the logic: show red for own units, green for others
        bool isMine = netObj.OwnerClientId == NetworkManager.Singleton.LocalClientId;

        // 🔄 REVERSED
        greenFillImage.gameObject.SetActive(!isMine);
        redFillImage.gameObject.SetActive(isMine);

        activeFillImage = isMine ? redFillImage : greenFillImage;
    }

    public void Init(float max)
    {
        maxHealth = max;
        currentHealth = max;
        UpdateBar();
    }

    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (activeFillImage != null)
        {
            activeFillImage.fillAmount = currentHealth / maxHealth;
        }
    }
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
