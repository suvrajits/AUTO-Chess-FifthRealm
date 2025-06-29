using UnityEngine;
using UnityEngine.UI;

public class ToggleButtonUI : MonoBehaviour
{
    public GameObject enabledButtonVisual;  // UI visual for "on" state
    public GameObject disabledButtonVisual; // UI visual for "off" state
    public GameObject targetObject;         // The object to show/hide

    public bool isEnabled = false;
    public UIPopupType popupType = UIPopupType.Shop;
    void Start()
    {
        UpdateUI();

        // Optional: Add listeners if using Buttons
        Button enabledBtn = enabledButtonVisual.GetComponent<Button>();
        Button disabledBtn = disabledButtonVisual.GetComponent<Button>();

        if (enabledBtn != null) enabledBtn.onClick.AddListener(ToggleState);
        if (disabledBtn != null) disabledBtn.onClick.AddListener(ToggleState);
    }

    public void ToggleState()
    {
        isEnabled = !isEnabled;
        targetObject.SetActive(isEnabled);
        UpdateUI();

        if (isEnabled)
        {
            UIOverlayManager.Instance.OpenPopup(popupType);
        }
        else
        {
            UIOverlayManager.Instance.ClosePopup(popupType);
        }
    }

    private void UpdateUI()
    {
        enabledButtonVisual.SetActive(isEnabled);
        disabledButtonVisual.SetActive(!isEnabled);
    }
}
