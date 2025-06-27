using System.Collections.Generic;
using UnityEngine;

public class PlayerCardDeck : MonoBehaviour
{
    [SerializeField] private int maxCapacity = 3;
    public List<HeroCardInstance> cards = new();

    public delegate void OnCardChanged();
    public event OnCardChanged DeckChanged;

    public bool TryAddCard(HeroData heroData)
    {
        if (cards.Count >= maxCapacity)
            return false;

        var newCard = new HeroCardInstance { baseHero = heroData, starLevel = 1 };
        cards.Add(newCard);
        TryFusion(heroData);
        DeckChanged?.Invoke();
        return true;
    }

    public void SellCard(HeroCardInstance card)
    {
        if (cards.Contains(card))
        {
            cards.Remove(card);
            DeckChanged?.Invoke();
            // Refund gold logic will be elsewhere
        }
    }

    private void TryFusion(HeroData heroData)
    {
        var matches = cards.FindAll(c => c.baseHero == heroData && c.starLevel == 1);
        if (matches.Count >= 3)
        {
            for (int i = 0; i < 3; i++)
                cards.Remove(matches[i]);

            cards.Add(new HeroCardInstance { baseHero = heroData, starLevel = 2 });
            Debug.Log($"✨ Fusion complete: {heroData.heroName} upgraded to 2★");
        }
    }

    public void SetCapacity(int newCap)
    {
        maxCapacity = newCap;
    }

    public int Capacity => maxCapacity;
}
