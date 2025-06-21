using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance { get; private set; }

    private HeroData currentSelectedHero;

    public delegate void OnHeroSelected(HeroData hero);
    public event OnHeroSelected HeroSelected;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectHero(HeroData hero)
    {
        currentSelectedHero = hero;
        HeroSelected?.Invoke(hero);
        Debug.Log(" Hero selected: " + hero.heroName);
    }

    public HeroData GetSelectedHero()
    {
        return currentSelectedHero;
    }
}
