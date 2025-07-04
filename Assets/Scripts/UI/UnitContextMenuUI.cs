using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class UnitContextMenuUI : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Button sendToDugoutButton;
    [SerializeField] private Button sellButton;

    private HeroUnit attachedUnit;
    private Camera cam;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void AttachToUnit(HeroUnit unit)
    {
        attachedUnit = unit;
        transform.position = unit.contextMenuAnchor.position;
    }

    public void Init(HeroUnit unit)
    {
        Debug.Log("✅ Init() called for UnitContextMenuUI");

        attachedUnit = unit;
        cam = PlayerNetworkState.LocalPlayer?.GetComponentInChildren<Camera>(true);

        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas.worldCamera = cam;
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
        StartCoroutine(DetectClickOutside());
    }

    public void HideMenu()
    {
        gameObject.SetActive(false);
    }

    private IEnumerator DetectClickOutside()
    {
        yield return null; // wait 1 frame to avoid self-click

        while (gameObject.activeSelf)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // Check if pointer is over any UI
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    PointerEventData pointerData = new PointerEventData(EventSystem.current)
                    {
                        position = Input.mousePosition
                    };

                    var results = new System.Collections.Generic.List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointerData, results);

                    bool clickedSelf = false;
                    foreach (var r in results)
                    {
                        if (r.gameObject.transform.IsChildOf(this.transform))
                        {
                            clickedSelf = true;
                            break;
                        }
                    }

                    if (!clickedSelf)
                    {
                        HideMenu();
                        yield break;
                    }
                }
                else
                {
                    // Clicked in world, not UI
                    HideMenu();
                    yield break;
                }
            }

            yield return null;
        }
    }
}
