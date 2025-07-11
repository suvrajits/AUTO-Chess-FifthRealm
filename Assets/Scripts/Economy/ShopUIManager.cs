using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ShopUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject heroCardPrefab;
    [SerializeField] private TMP_Text rerollCostText;
    [SerializeField] private Button rerollButton;

    private List<HeroCardShopUI> activeCards = new();
    
    private void Start()
    {
        rerollButton.onClick.RemoveAllListeners();
        rerollButton.onClick.AddListener(OnClickReroll);
        rerollCostText.text = $"{ShopManager.Instance.RerollCost}";

        ShopManager.Instance.RequestInitialShop();

        StartCoroutine(CheckForDeferredShopRender());
    }
    private IEnumerator CheckForDeferredShopRender()
    {
        yield return new WaitForSeconds(0.1f);

        if (ShopManager.TryConsumeDeferredReroll(out var ids))
        {
            Debug.Log("🟢 Deferred shop found, rendering immediately.");
            RenderShop(new List<int>(ids));
        }
    }

    private IEnumerator WaitForDeferredShopAndRender()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (ShopManager.TryConsumeDeferredReroll(out var ids))
            {
                Debug.Log("🟢 Deferred shop data found. Rendering...");
                RenderShop(new List<int>(ids));
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning("⚠️ No deferred shop data received in time.");
    }

    public void RenderShop(List<int> heroIds)
    {
        ClearShopUI();

        if (heroIds == null || heroIds.Count == 0)
        {
            Debug.LogWarning("⚠️ Empty shop list received.");
            return;
        }

        foreach (int id in heroIds)
        {
            HeroData hero = UnitDatabase.Instance.GetHeroById(id);
            if (hero == null)
            {
                Debug.LogWarning($"❌ HeroData not found for id {id}");
                continue;
            }

            GameObject cardGO = Instantiate(heroCardPrefab, cardContainer);
            HeroCardShopUI card = cardGO.GetComponent<HeroCardShopUI>();
            card.Setup(hero, OnCardBuyClicked);
            activeCards.Add(card);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)cardContainer);
        rerollButton.interactable = true;

        Debug.Log($"✅ RenderShop completed. Cards: {heroIds.Count}");
    }



    private void ClearShopUI()
    {
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }
        activeCards.Clear();
    }

    private void OnCardBuyClicked(int heroId)
    {
        rerollButton.interactable = false; // Optionally lock input until shop updates
        ShopManager.Instance.TryBuy(heroId);
    }

    private void OnClickReroll()
    {
        rerollButton.interactable = false;
        ShopManager.Instance.TryReroll();
    }

    public void OnClickFreeReroll()
    {
        ShopManager.Instance.TryFreeReroll();
    }

    public void Clear()
    {
        foreach (var card in activeCards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        activeCards.Clear();
    }

}
