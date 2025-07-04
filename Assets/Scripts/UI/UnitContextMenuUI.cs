using UnityEngine;
using UnityEngine.UI;

public class UnitContextMenuUI : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Button sendToDugoutButton;
    [SerializeField] private Button sellButton;

    private HeroUnit attachedUnit;
    private Camera cam;

    public void AttachToUnit(HeroUnit unit)
    {
        attachedUnit = unit;
        transform.position = unit.contextMenuAnchor.position;
    }

    public void Init(HeroUnit unit)
    {
        Debug.Log("✅ Init() called for UnitContextMenuUI");

        attachedUnit = unit;

        // Grab main cam from local player prefab (should exist even for host)
        cam = PlayerNetworkState.LocalPlayer?.GetComponentInChildren<Camera>(true);

        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            //canvas.worldCamera = cam;
            canvas.worldCamera = PlayerNetworkState.LocalPlayer?.GetComponentInChildren<Camera>(true);

        }

        var scaler = GetComponent<ConstantScreenSize>();
        if (scaler != null)
        {
            scaler.SetCamera(cam);
        }

        sendToDugoutButton?.onClick.AddListener(() =>
        {
            Debug.Log("📦 Dugout button clicked");
            attachedUnit?.RequestReturnToDeckServerRpc();
            HideMenu();
        });

        sellButton?.onClick.AddListener(() =>
        {
            Debug.Log("💰 Sell button clicked");
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
}
