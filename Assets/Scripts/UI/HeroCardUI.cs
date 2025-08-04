using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HeroCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public Image iconImage;
    public TMP_Text heroName;
    public Transform starContainer;
    public GameObject starPrefab;
    public Button sellButton;

    private HeroData assignedHero;
    private UnitSelectionUI selectionUI;
    private int index;
    private HeroCardInstance cardInstance;

    // 👇 Drag preview
    private GameObject dragPreview;
    public Transform traitIconContainer; // UI horizontal layout group
    public GameObject traitIconPrefab;
    public void Setup(HeroCardInstance instance, UnitSelectionUI selectionUIRef, int cardIndex)
    {
        cardInstance = instance;
        assignedHero = instance.baseHero;
        selectionUI = selectionUIRef;
        index = cardIndex;

        if (iconImage != null)
            iconImage.sprite = assignedHero.heroIcon;

        if (heroName != null)
            heroName.text = assignedHero.heroName;

        GenerateStars(instance.starLevel);

        SetTraits(assignedHero.traits);

        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(OnClick);

        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(OnSellClicked);
        }
    }

    private void OnClick()
    {
        if (assignedHero != null)
        {
            selectionUI.SelectHero(cardInstance, index);
        }
    }

    private void OnSellClicked()
    {
        if (cardInstance == null || cardInstance.baseHero == null)
        {
            Debug.LogWarning("❌ Cannot sell: No card assigned.");
            return;
        }

        var localPlayer = PlayerNetworkState.GetLocalPlayer();
        if (localPlayer != null)
        {
            Debug.Log($"💸 Selling heroId {cardInstance.baseHero.heroId} (★{cardInstance.starLevel})");
            localPlayer.SellHeroCardServerRpc(cardInstance.baseHero.heroId, cardInstance.starLevel);
        }
        else
        {
            Debug.LogWarning("❌ Local player not found. Cannot sell.");
        }
    }

    private void GenerateStars(int starLevel)
    {
        foreach (Transform child in starContainer)
            Destroy(child.gameObject);

        int clampedStars = Mathf.Clamp(starLevel, 1, 5);
        for (int i = 0; i < clampedStars; i++)
        {
            GameObject star = Instantiate(starPrefab, starContainer);
            star.SetActive(true);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(starContainer.GetComponent<RectTransform>());
    }

    // 🔽 DRAG & DROP INTERFACES

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardInstance == null || cardInstance.baseHero == null) return;

        dragPreview = new GameObject("DragPreview");
        dragPreview.transform.SetParent(transform.root, false);
        Image previewImage = dragPreview.AddComponent<Image>();
        previewImage.raycastTarget = false;
        previewImage.sprite = cardInstance.baseHero.heroIcon;
        previewImage.color = new Color(1f, 1f, 1f, 0.6f); // 60% opacity

        RectTransform rect = dragPreview.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 100);

        // ✅ Show tiles when dragging starts
        if (GridManager.Instance != null)
        {
            GridManager.Instance.ShowAllTiles(true, pulse: true);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragPreview != null)
        {
            dragPreview.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragPreview == null)
        {
            // This drag likely wasn't initialized, so exit early to avoid null errors.
            Debug.LogWarning("⚠️ OnEndDrag called but dragPreview was null. Possibly not an actual drag.");
            return;
        }

        Destroy(dragPreview);
        dragPreview = null;

        // ✅ Hide tiles when dragging ends
        if (GridManager.Instance != null)
        {
            GridManager.Instance.ShowAllTiles(false);
        }

        // Try to raycast onto the board
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Tile")))
        {
            GridTile tile = hit.collider.GetComponent<GridTile>();
            if (tile != null)
            {
                UnitPlacer placer = FindFirstObjectByType<UnitPlacer>();
                if (placer != null)
                {
                    placer.TryPlaceUnitFromDeck(cardInstance, tile);
                }
                else
                {
                    Debug.LogWarning("❌ UnitPlacer not found in scene.");
                }
            }
        }
    }
    public void SetTraits(List<TraitDefinition> traits)
    {
        // Clear existing icons
        foreach (Transform child in traitIconContainer)
            Destroy(child.gameObject);

        foreach (var trait in traits)
        {
            GameObject iconGO = Instantiate(traitIconPrefab, traitIconContainer);
            Image iconImage = iconGO.GetComponent<Image>();
            if (iconImage != null)
                iconImage.sprite = trait.traitIcon;
            // Optional: tooltip setup here
        }
    }


}
