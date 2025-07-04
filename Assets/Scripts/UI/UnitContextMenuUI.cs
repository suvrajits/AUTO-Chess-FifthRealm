using UnityEngine;
using UnityEngine.UI;

public class UnitContextMenuUI : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Button sendToDugoutButton;
    [SerializeField] private Button sellButton;

    private HeroUnit attachedUnit;
    private Camera cam;

    public void Init(HeroUnit unit)
    {
        Debug.Log("✅ Init() called for UnitContextMenuUI");
        attachedUnit = unit;

        cam = PlayerNetworkState.LocalPlayer?.GetComponentInChildren<Camera>(true);
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas.worldCamera = cam;
        }

        //sendToDugoutButton?.onClick.RemoveAllListeners();
        sendToDugoutButton?.onClick.AddListener(() =>
        {
            Debug.Log("dugout button clicked");
            attachedUnit?.RequestReturnToDeckServerRpc();
            HideMenu();
        });

        //sellButton?.onClick.RemoveAllListeners();
        sellButton?.onClick.AddListener(() =>
        {
            Debug.Log("sell button clicked");
            attachedUnit?.RequestSellFromGridServerRpc();
            HideMenu();
        });
    }

    public void ShowMenu()
    {
        if (attachedUnit == null || !attachedUnit.IsOwner) return;
        gameObject.SetActive(true);
    }

    public void HideMenu()
    {
        gameObject.SetActive(false);
    }
    public void AttachToUnit(HeroUnit unit)
    {
        attachedUnit = unit;
        transform.position = unit.contextMenuAnchor.position;
    }
}
