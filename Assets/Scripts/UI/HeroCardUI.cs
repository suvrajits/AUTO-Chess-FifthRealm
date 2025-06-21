using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeroCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TMP_Text heroName;

    private HeroData assignedHero;
    private UnitSelectionUI selectionUI;
    private int index;

    public void Setup(HeroData hero, UnitSelectionUI selectionUIRef, int cardIndex)
    {
        assignedHero = hero;
        selectionUI = selectionUIRef;
        index = cardIndex;

        if (iconImage != null)
            iconImage.sprite = hero.heroIcon;

        if (heroName != null)
            heroName.text = hero.heroName;

        // Attach click handler
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (assignedHero != null)
        {
            selectionUI.SelectHero(assignedHero, index);
        }
    }
}
