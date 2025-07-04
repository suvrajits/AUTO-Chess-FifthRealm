using UnityEngine;

public class HeroClickHandler : MonoBehaviour
{
    [SerializeField] private LayerMask heroUnitLayer;
    [SerializeField] private Camera mainCamera;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (UIOverlayManager.Instance == null)
            Debug.LogWarning("⚠️ HeroClickHandler: UIOverlayManager.Instance is null. UI detection will be skipped.");
    }

    private void Update()
    {
        bool isOverUI = UIOverlayManager.Instance != null && UIOverlayManager.Instance.IsPointerOverUI();

        if (Input.GetMouseButtonDown(0) && !isOverUI)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, heroUnitLayer))
            {
                Debug.Log($"🧠 Clicked on object: {hit.collider.gameObject.name}");

                HeroUnit unit = hit.collider.GetComponentInParent<HeroUnit>();
                if (unit != null)
                {
                    Debug.Log($"✅ Clicked on HeroUnit: {unit.name}");

                    if (unit.IsOwner && unit.contextMenuInstance != null)
                    {
                        unit.contextMenuInstance.ShowMenu();
                        Debug.Log("📜 Context menu shown.");
                    }
                    else if (!unit.IsOwner)
                    {
                        Debug.Log("🚫 Not the owner of the clicked unit.");
                    }
                    else
                    {
                        Debug.Log("❌ Context menu instance is null.");
                    }
                    return;
                }
            }

            // Hide all owned menus on empty space click
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
}
