using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerCardDeck : NetworkBehaviour
{
    [SerializeField] private int maxCapacity = 6;
    public List<HeroCardInstance> cards = new();

    public delegate void OnCardChanged();
    public event OnCardChanged DeckChanged;
    public int Capacity => maxCapacity;
    public bool TryAddCard(HeroData heroData, out bool didFuse)
    {
        didFuse = false;

        if (cards.Count >= maxCapacity)
            return false;

        cards.Add(new HeroCardInstance { baseHero = heroData, starLevel = 1 });
        didFuse = TryFusion(heroData);
        DeckChanged?.Invoke();
        return true;
    }


    public void SellCard(HeroCardInstance card)
    {
        if (cards.Contains(card))
        {
            cards.Remove(card);
            DeckChanged?.Invoke();
            // Gold will now be handled only via ServerRpc externally
            Debug.Log($"🗑️ Card removed: {card.baseHero.heroName} (★{card.starLevel})");
        }
    }

    private bool TryFusion(HeroData heroData)
    {
        var matches = cards.FindAll(c => c.baseHero == heroData && c.starLevel == 1);
        if (matches.Count >= 3)
        {
            for (int i = 0; i < 3; i++)
                cards.Remove(matches[i]);

            cards.Add(new HeroCardInstance { baseHero = heroData, starLevel = 2 });

            Debug.Log($"✨ Fusion complete: {heroData.heroName} upgraded to 2★");
            DeckChanged?.Invoke();
            return true;
        }

        return false;
    }



    public void SetCapacity(int newCap)
    {
        maxCapacity = newCap;
    }


    public void SyncDeckToClient(ulong targetClientId)
    {
        List<int> ids = new();
        List<int> stars = new();

        foreach (var card in cards)
        {
            ids.Add(card.baseHero.heroId);
            stars.Add(card.starLevel);
        }

        SyncDeckClientRpc(ids.ToArray(), stars.ToArray(), new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { targetClientId } }
        });
    }
    [ClientRpc]
    private void SyncDeckClientRpc(int[] heroIds, int[] starLevels, ClientRpcParams rpcParams = default)
    {
        if (!IsClient) return;

        cards.Clear();

        for (int i = 0; i < heroIds.Length; i++)
        {
            HeroData data = UnitDatabase.Instance.GetHeroById(heroIds[i]);
            int level = (i < starLevels.Length) ? starLevels[i] : 1;

            if (data != null)
            {
                cards.Add(new HeroCardInstance
                {
                    baseHero = data,
                    starLevel = level
                });
            }
        }

        DeckChanged?.Invoke();
    }
    public void RemoveCardInstance(HeroCardInstance cardToRemove)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].baseHero == cardToRemove.baseHero && cards[i].starLevel == cardToRemove.starLevel)
            {
                cards.RemoveAt(i);
                DeckChanged?.Invoke();
                Debug.Log($"🗑️ Removed {cardToRemove.baseHero.heroName} (★{cardToRemove.starLevel}) from deck.");
                return;
            }
        }

        Debug.LogWarning("⚠️ Tried to remove card but no exact match found.");
    }
    public void ClearDeck()
    {
        cards.Clear();
        DeckChanged?.Invoke();
        Debug.Log("🧹 Cleared player deck.");
    }

    public bool CanAddCard()
    {
        return cards.Count < Capacity;
    }
    public void AddCardFromUnit(HeroUnit unit)
    {
        if (unit == null || unit.heroData == null)
        {
            Debug.LogWarning("❌ Invalid unit or missing heroData.");
            return;
        }

        var newCard = new HeroCardInstance
        {
            baseHero = unit.heroData,
            starLevel = unit.starLevel
        };

        cards.Add(newCard);
        SyncDeckToClient(unit.OwnerClientId); // ✅ Use the hero's owner to refresh deck UI
        DeckChanged?.Invoke();
    }
}
