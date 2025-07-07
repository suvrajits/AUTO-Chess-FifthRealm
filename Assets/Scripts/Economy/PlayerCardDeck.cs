using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerCardDeck : NetworkBehaviour
{
    [SerializeField] private int maxCapacity = 5;
    public List<HeroCardInstance> cards = new();

    public delegate void OnCardChanged();
    public event OnCardChanged DeckChanged;
    public int Capacity => maxCapacity;
    public bool TryAddCard(HeroData heroData, out bool didFuse)
    {
        didFuse = AddCard(new HeroCardInstance { baseHero = heroData, starLevel = 1 });
        return didFuse;
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
        bool didAnyFusion = false;

        while (true)
        {
            bool fused = false;

            for (int star = 1; star <= 2; star++) // allow up to ★3
            {
                var matches = cards.FindAll(c => c.baseHero == heroData && c.starLevel == star);

                if (matches.Count >= 3)
                {
                    // Remove 3 matching cards
                    for (int i = 0; i < 3; i++)
                        cards.Remove(matches[i]);

                    // Create next-star fusion card
                    var fusedCard = new HeroCardInstance
                    {
                        baseHero = heroData,
                        starLevel = star + 1
                    };

                    Debug.Log($"🌟 Fusion: {heroData.heroName} upgraded to ★{fusedCard.starLevel}");

                    // 🔁 Re-enter fusion via AddCard — key to solving your bug
                    AddCard(fusedCard);

                    didAnyFusion = true;
                    fused = true;
                    break; // restart from star=1
                }
            }

            if (!fused)
                break;
        }

        return didAnyFusion;
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
    public bool AddCard(HeroCardInstance card)
    {
        if (cards.Count >= maxCapacity)
        {
            Debug.LogWarning("⚠️ Cannot add card: deck is full.");
            return false;
        }

        cards.Add(card);
        Debug.Log($"➕ Added {card.baseHero.heroName} (★{card.starLevel}) to deck.");

        bool didFuse = TryFusion(card.baseHero); // ✅ critical
        DeckChanged?.Invoke();

        return true;
    }

    public bool HasRoom()
    {
        return cards.Count < maxCapacity;
    }


}
