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
    public TMP_Text starLevelText;

    public void Setup(HeroData hero, UnitSelectionUI selectionUIRef, int cardIndex, int starLevel = 1)
    {
        assignedHero = hero;
        selectionUI = selectionUIRef;
        index = cardIndex;

        if (iconImage != null)
            iconImage.sprite = hero.heroIcon;

        if (heroName != null)
            heroName.text = hero.heroName;

        if (starLevelText != null)
            starLevelText.text = new string('★', starLevel); // e.g., "★★"

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
