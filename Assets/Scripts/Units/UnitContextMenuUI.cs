using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitContextMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public Button dugoutButton;
    public Button sellButton;
    public TextMeshProUGUI sellButtonText;

    private HeroUnit targetUnit;

    /// <summary>
    /// Initializes the context menu for the given hero.
    /// </summary>
    /// <param name="unit">The HeroUnit this menu controls.</param>
    public void Init(HeroUnit unit)
    {
        targetUnit = unit;

        if (dugoutButton != null)
            dugoutButton.onClick.AddListener(OnDugoutClicked);

        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellClicked);

        int sellValue = unit.GetSellValue(); // This method should return star-based gold value
        if (sellButtonText != null)
            sellButtonText.text = $"Sell for {sellValue}G";
    }

    private void OnDugoutClicked()
    {
        if (targetUnit != null && targetUnit.IsOwner)
        {
            targetUnit.RequestReturnToDeckServerRpc();
        }
        Destroy(gameObject); // Clean up context menu
    }

    private void OnSellClicked()
    {
        if (targetUnit != null && targetUnit.IsOwner)
        {
            targetUnit.RequestSellFromGridServerRpc();
        }
        Destroy(gameObject); // Clean up context menu
    }
}
