using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;

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
    private Camera mainCamera;
    private GridTile currentHoveredTile;
    private List<GridTile> eligibleTiles = new();
    private bool isDraggingCard = false;
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

        mainCamera = Camera.main;

        // 🔥 Activate glow mode for eligible tiles
        eligibleTiles.Clear();
        ulong clientId = NetworkManager.Singleton.LocalClientId;

        if (GridManager.Instance.playerTileMaps.TryGetValue(clientId, out var tiles))
        {
            foreach (var tile in tiles.Values)
            {
                if (!tile.IsOccupied)
                {
                    tile.EnableGlow(); // Show static golden glow
                    eligibleTiles.Add(tile);
                }
            }
        }

        // ✅ Notify TileSelector for hover pulse
        TileSelector.Instance?.SetDraggingState(true);

        // 🧩 Show drag preview icon under finger/cursor
        dragPreview = new GameObject("DragPreview");
        dragPreview.transform.SetParent(transform.root, false);
        Image previewImage = dragPreview.AddComponent<Image>();
        previewImage.raycastTarget = false;
        previewImage.sprite = cardInstance.baseHero.heroIcon;
        previewImage.color = new Color(1f, 1f, 1f, 0.6f); // Slightly transparent

        RectTransform rect = dragPreview.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 100);
    }


    public void OnDrag(PointerEventData eventData)
    {
        if (dragPreview != null)
            dragPreview.transform.position = eventData.position;

        if (mainCamera == null) return;

        if (Physics.Raycast(mainCamera.ScreenPointToRay(eventData.position), out RaycastHit hit, 100f, LayerMask.GetMask("Tile")))
        {
            GridTile tile = hit.collider.GetComponent<GridTile>();
            if (tile != null)
            {
                TileSelector.Instance?.OnDraggingOverTile(tile);
            }
        }
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragPreview != null)
            Destroy(dragPreview);

        dragPreview = null;

        // ❌ Clear hover pulse effect
        TileSelector.Instance?.SetDraggingState(false);

        // ❌ Disable glow for all previously eligible tiles
        foreach (var tile in eligibleTiles)
            tile.DisableGlow();

        eligibleTiles.Clear();
        currentHoveredTile = null;

        // 🧠 Raycast from pointer to find tile under cursor
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Tile")))
        {
            GridTile tile = hit.collider.GetComponent<GridTile>();
            if (tile != null)
            {
                UnitPlacer placer = FindFirstObjectByType<UnitPlacer>();
                placer?.TryPlaceUnitFromDeck(cardInstance, tile);
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

    public void SetDraggingState(bool isDragging)
    {
        isDraggingCard = isDragging;
    }

}
