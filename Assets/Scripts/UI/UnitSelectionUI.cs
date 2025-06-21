using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject cardTemplate;
    public Transform buttonContainer;

    [Header("Optional")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    private List<Button> instantiatedButtons = new();

    private void Start()
    {
        cardTemplate.SetActive(false);

        var allHeroes = UnitDatabase.Instance.allHeroes;

        for (int i = 0; i < allHeroes.Count; i++)
        {
            HeroData hero = allHeroes[i];

            GameObject cardObj = Instantiate(cardTemplate, buttonContainer);
            cardObj.SetActive(true);

            HeroCardUI cardUI = cardObj.GetComponent<HeroCardUI>();
            cardUI.Setup(hero, this, i); // 👈 Pass reference to self for callback

            Button btn = cardObj.GetComponent<Button>();
            instantiatedButtons.Add(btn);
        }

        if (allHeroes.Count > 0)
        {
            OnHeroClicked(allHeroes[0], 0);
        }
    }

    private void OnHeroClicked(HeroData hero, int index)
    {
        SelectHero(hero, index);
    }

    public void SelectHero(HeroData hero, int selectedIndex)
    {
        UnitSelectionManager.Instance.SelectHero(hero);

        for (int i = 0; i < instantiatedButtons.Count; i++)
        {
            var colors = instantiatedButtons[i].colors;
            colors.normalColor = (i == selectedIndex) ? selectedColor : normalColor;
            instantiatedButtons[i].colors = colors;
        }
    }
}
