using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerCardDeck : NetworkBehaviour
{
    [SerializeField] private int maxCapacity = 3;
    public List<HeroCardInstance> cards = new();

    public delegate void OnCardChanged();
    public event OnCardChanged DeckChanged;
    public int Capacity => maxCapacity;
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


    public void SyncDeckToClient(ulong targetClientId)
    {
        var heroIds = cards.ConvertAll(c => c.baseHero.heroId);

        SyncDeckClientRpc(heroIds.ToArray(), new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { targetClientId }
            }
        });
    }
    [ClientRpc]
    private void SyncDeckClientRpc(int[] heroIds, ClientRpcParams rpcParams = default)
    {
        if (!IsClient) return;

        Debug.Log($"📨 SyncDeckClientRpc received: {heroIds.Length} cards");

        cards.Clear();
        foreach (int id in heroIds)
        {
            var data = UnitDatabase.Instance.GetHeroById(id);
            if (data != null)
                cards.Add(new HeroCardInstance { baseHero = data, starLevel = 1 });
        }

        DeckChanged?.Invoke();
    }


}
