using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnitDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public HeroCardInstance cardInstance; // Must be assigned on init
    public Image iconImage; // Assigned in inspector for the card image

    private GameObject dragPreview;
    private RectTransform dragPreviewRect;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardInstance == null || iconImage == null)
        {
            Debug.LogWarning("❌ UnitDraggable: Missing cardInstance or iconImage");
            return;
        }

        canvasGroup.blocksRaycasts = false;

        // Create drag preview object
        dragPreview = new GameObject("DragPreview");
        dragPreview.transform.SetParent(canvas.transform);
        dragPreview.transform.SetAsLastSibling();

        dragPreviewRect = dragPreview.AddComponent<RectTransform>();
        dragPreviewRect.sizeDelta = iconImage.rectTransform.sizeDelta;

        Image previewImage = dragPreview.AddComponent<Image>();
        previewImage.sprite = cardInstance.baseHero.heroIcon;;
        previewImage.raycastTarget = false;

        // 🎨 Optional: Tint or reduce alpha for drag preview
        previewImage.color = new Color(1f, 1f, 1f, 0.6f); // 60% opacity

        // OR grayscale tint:
        // previewImage.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
    }


    public void OnDrag(PointerEventData eventData)
    {
        if (dragPreviewRect != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out Vector2 localPoint
            );
            dragPreviewRect.localPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragPreview != null)
        {
            Destroy(dragPreview);
        }

        // Restore raycast
        canvasGroup.blocksRaycasts = true;

        // Ask placement system to handle drop
        DragPlacementManager.Instance.TryPlaceDraggedUnit(cardInstance, eventData.position);
    }

    public void Init(HeroCardInstance instance)
    {
        cardInstance = instance;
        iconImage.sprite = instance.baseHero.heroIcon;;
    }
}

