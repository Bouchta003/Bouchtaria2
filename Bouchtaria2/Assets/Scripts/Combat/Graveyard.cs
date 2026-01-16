using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Graveyard
{
    private readonly List<CardData> cards = new();

    public IReadOnlyList<CardData> Cards => cards;

    public void Add(CardData data)
    {
        if (data != null)
            cards.Add(data);
    }

    public CardData PopLastExcluding(CardData excluded)
    {
        for (int i = cards.Count - 1; i >= 0; i--)
        {
            if (cards[i] != excluded)
            {
                CardData picked = cards[i];
                cards.RemoveAt(i);
                return picked;
            }
        }

        return null;
    }
    public CardData PopRandomExcluding(CardData excluded)
    {
        if (cards.Count == 0)
            return null;

        // Collect valid indices
        List<int> validIndices = new();

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != excluded)
                validIndices.Add(i);
        }

        if (validIndices.Count == 0)
            return null;

        int pickIndex = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];
        CardData picked = cards[pickIndex];
        cards.RemoveAt(pickIndex);

        return picked;
    }

}
