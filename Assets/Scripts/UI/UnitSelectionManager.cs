using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance { get; private set; }

    private HeroCardInstance selectedCard;

    public delegate void OnHeroCardSelected(HeroCardInstance card);
    public event OnHeroCardSelected HeroSelected;

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

    public void SetSelectedCard(HeroCardInstance card)
    {
        selectedCard = card;
        HeroSelected?.Invoke(card);
        Debug.Log($"📦 HeroCard selected: {card?.baseHero?.heroName} (★{card?.starLevel})");
    }

    public HeroCardInstance GetSelectedCard()
    {
        return selectedCard;
    }

    public void ClearSelectedCard()
    {
        Debug.Log("🧹 Cleared selected card.");
        selectedCard = null;
        HeroSelected?.Invoke(null);
    }

    public bool HasCardSelected()
    {
        return selectedCard != null;
    }
}
