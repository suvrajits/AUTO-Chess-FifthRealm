using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeroCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TMP_Text heroName;
    public Transform starContainer; // ⭐ Holds instantiated star icons
    public GameObject starPrefab;   // ⭐ A single star image prefab (e.g., gold star)

    private HeroData assignedHero;
    private UnitSelectionUI selectionUI;
    private int index;

    public void Setup(HeroData hero, UnitSelectionUI selectionUIRef, int cardIndex, int starLevel = 1)
    {
        assignedHero = hero;
        selectionUI = selectionUIRef;
        index = cardIndex;

        if (iconImage != null)
            iconImage.sprite = hero.heroIcon;

        if (heroName != null)
            heroName.text = hero.heroName;

        GenerateStars(starLevel);

        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(OnClick);
    }


    private void OnClick()
    {
        if (assignedHero != null)
        {
            selectionUI.SelectHero(assignedHero, index); // ✅ Enough!
        }
    }

    private void GenerateStars(int starLevel)
    {
        // Clear old stars
        foreach (Transform child in starContainer)
        {
            Destroy(child.gameObject);
        }

        // Clamp level
        int clampedStars = Mathf.Clamp(starLevel, 1, 5);

        // Instantiate and enable each star
        for (int i = 0; i < clampedStars; i++)
        {
            GameObject star = Instantiate(starPrefab, starContainer);
            star.SetActive(true);
        }

        // ✅ Force layout rebuild to apply spacing
        LayoutRebuilder.ForceRebuildLayoutImmediate(starContainer.GetComponent<RectTransform>());
    }

}
