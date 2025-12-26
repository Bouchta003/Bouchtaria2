using UnityEngine;
using System.Collections.Generic;
using System;

public class TraitsDetection : MonoBehaviour
{
    Dictionary<CardData.Trait, int> traitsTiers;

    [Header("TraitSlot")]
    [SerializeField] GameObject traitSlotPrefab;
    [SerializeField] GameObject traitLayout;

    bool TryParseTrait(string traitString, out CardData.Trait trait)
    {
        return Enum.TryParse(traitString, true, out trait);
    }

    public Dictionary<CardData.Trait, int> RetrieveTraitTiersFromDeck(Queue<CardData> playerDeck)
    {
        // 1. Count fractional trait contributions
        Dictionary<CardData.Trait, float> traitCounts = new();

        foreach (CardData card in playerDeck)
        {
            if (card.traits == null || card.traits.Count == 0)
                continue;

            List<CardData.Trait> resolvedTraits = new();

            foreach (string traitString in card.traits)
            {
                if (TryParseTrait(traitString, out CardData.Trait trait))
                {
                    resolvedTraits.Add(trait);
                }
                else
                {
                    Debug.LogWarning($"Unknown trait '{traitString}' on card {card.name}");
                }
            }

            if (resolvedTraits.Count == 0)
                continue;

            float contribution = 1f / resolvedTraits.Count;

            foreach (CardData.Trait trait in resolvedTraits)
            {
                if (!traitCounts.ContainsKey(trait))
                    traitCounts[trait] = 0f;

                traitCounts[trait] += contribution;
            }
        }

        // 2. Define tier thresholds per trait
        Dictionary<CardData.Trait, int[]> tierThresholds = new()
        {
            { CardData.Trait.Speedster, new[] { 6, 9, 12 } },
            { CardData.Trait.Gunner, new[] { 6, 9, 12 } },
            { CardData.Trait.SpellFocus, new[] { 6, 9, 12 } },
            { CardData.Trait.Faith, new[] { 6, 9 } },
            { CardData.Trait.Healer, new[] { 6, 9 } },
            { CardData.Trait.Inazuma, new[] { 10 } },
            { CardData.Trait.Pokemon, new[] { 2,4,6 } },
            { CardData.Trait.Neutral, new[] { 0, 1 } }
        };

        // 3. Resolve tiers
        Dictionary<CardData.Trait, int> resolvedTiers = new();

        foreach (var kvp in traitCounts)
        {
            CardData.Trait trait = kvp.Key;
            float count = kvp.Value;

            if (!tierThresholds.ContainsKey(trait))
                continue;

            int tier = 0;
            int[] thresholds = tierThresholds[trait];

            for (int i = 0; i < thresholds.Length; i++)
            {
                if (count >= thresholds[i])
                    tier = i + 1;
            }

            if (tier > 0)
            {
                resolvedTiers[trait] = tier;
                Debug.Log($"Trait {trait} unlocked at Tier {tier} (count: {count})");
                GameObject traitPrefab = Instantiate(traitSlotPrefab, traitLayout.transform);
                TraitsDisplay traitDisplay = traitPrefab.GetComponentInChildren<TraitsDisplay>();
                traitDisplay.thisTrait = trait; traitDisplay.tier = tier;
                switch (trait)
                {
                    case CardData.Trait.Pokemon:
                        traitDisplay.gemSlot.color = new Color(0.95f, 0.25f, 0.25f);
                        traitDisplay.iconSlot.sprite = traitDisplay.pokemonIcon;
                        break;
                    case CardData.Trait.Neutral:
                        traitDisplay.gemSlot.color = new Color(0.60f, 0.60f, 0.60f);
                        traitDisplay.iconSlot.sprite = traitDisplay.neutralIcon;
                        break;
                    default:
                        traitDisplay.gemSlot.color = new Color(1.00f, 0.85f, 0.35f);
                        break;
                }
            }
        }

        return resolvedTiers;
    }

}
