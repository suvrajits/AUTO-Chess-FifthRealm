using UnityEngine;

public class UIOverlayManager : MonoBehaviour
{
    public static UIOverlayManager Instance;

    public UIPopupType ActivePopup { get; private set; } = UIPopupType.None;

    private GamePhase previousPhase;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void OpenPopup(UIPopupType popup)
    {
        if (ActivePopup != UIPopupType.None)
        {
            Debug.LogWarning($"❗ A popup is already open: {ActivePopup}");
            return;
        }

        previousPhase = BattleManager.Instance.CurrentPhase;
        ActivePopup = popup;

      
        Debug.Log($"📥 Opened popup: {popup}");
    }

    public void ClosePopup(UIPopupType popup)
    {
        if (ActivePopup != popup)
        {
            Debug.LogWarning($"❌ Tried to close popup {popup}, but active is {ActivePopup}");
            return;
        }

        ActivePopup = UIPopupType.None;

        Debug.Log($"📤 Closed popup: {popup}");
    }

    public bool IsPopupOpen() => ActivePopup != UIPopupType.None;
}
