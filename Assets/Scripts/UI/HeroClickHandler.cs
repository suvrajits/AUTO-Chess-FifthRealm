using UnityEngine;
using Unity.Netcode;

public class HeroClickHandler : MonoBehaviour
{
    [SerializeField] private LayerMask heroUnitLayer;
    [SerializeField] private Camera mainCamera;

    private bool clickedValidUnit = false;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (UIOverlayManager.Instance == null)
            Debug.LogWarning("⚠️ HeroClickHandler: UIOverlayManager.Instance is null. UI detection will be skipped.");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // ✅ If no hero hit or not your unit, hide all menus
            
            bool isOverUI = UIOverlayManager.Instance != null && UIOverlayManager.Instance.IsPointerOverUI();

            if (isOverUI)
                return;
            
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, heroUnitLayer))
            {
                HeroUnit unit = hit.collider.GetComponentInParent<HeroUnit>();
                if (unit != null && unit.IsOwner && unit.contextMenuInstance != null)
                {
                    unit.contextMenuInstance.ShowMenu();
                    Debug.Log("📜 Context menu shown.");
                    return;
                }
            }

           
        }
    }



    private void HideAllMenus()
    {
        foreach (var unit in FindObjectsOfType<HeroUnit>())
        {
            if (unit.IsOwner && unit.contextMenuInstance != null)
            {
                unit.contextMenuInstance.HideMenu();
            }
        }

        Debug.Log("❎ Clicked elsewhere — all menus hidden.");
    }
}
