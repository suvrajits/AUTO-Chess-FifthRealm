using UnityEngine;

public class UnitInputHandler : MonoBehaviour
{
    [Header("Long Press Settings")]
    [SerializeField] private float longPressThreshold = 0.6f;
    [SerializeField] private LayerMask heroUnitLayerMask; // Set this to "HeroUnit" in Inspector

    private IUnitInteractable currentInteractable = null;
    private float pressTimer = 0f;
    private bool longPressTriggered = false;

    void Update()
    {
       
#if UNITY_EDITOR || UNITY_STANDALONE
        HandlePCInput();
#else
        HandleMobileInput();
#endif
    }

    private void HandlePCInput()
    {
        /*if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("🖱️ Left-click detected");
            currentInteractable = GetHoveredUnit();
            currentInteractable?.OnRightClick();
        }*/
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("🖱️ Left-click detected");
            currentInteractable = GetHoveredUnit();
            currentInteractable?.OnRightClick();
        }

    }

    private void HandleMobileInput()
    {
        if (Input.touchCount != 1)
        {
            pressTimer = 0f;
            longPressTriggered = false;
            return;
        }

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            currentInteractable = GetTouchedUnit(touch.position);
            pressTimer = 0f;
            longPressTriggered = false;
        }

        if (touch.phase == TouchPhase.Stationary)
        {
            pressTimer += Time.deltaTime;
            if (!longPressTriggered && pressTimer >= longPressThreshold)
            {
                currentInteractable?.OnLongPress();
                longPressTriggered = true;
            }
        }

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            pressTimer = 0f;
            longPressTriggered = false;
        }
    }

    private IUnitInteractable GetHoveredUnit()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f); // 👈 draw it!

        if (Physics.Raycast(ray, out RaycastHit hit, 100f)) // ← remove LayerMask
        {
            Debug.Log($"🔴 Raycast (no mask) hit: {hit.collider.name}");
            return hit.collider.GetComponent<IUnitInteractable>();
        }

        Debug.Log("⚠️ Raycast missed everything");
        return null;
    }



    private IUnitInteractable GetTouchedUnit(Vector2 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, heroUnitLayerMask))
        {
            return hit.collider.GetComponent<IUnitInteractable>();
        }
        return null;
    }
}
