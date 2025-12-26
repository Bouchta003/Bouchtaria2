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
            { CardData.Trait.Workout, new[] { 4, 7 } },
            { CardData.Trait.Gunner, new[] { 6, 9, 12 } },
            { CardData.Trait.Faith, new[] { 6, 9 } },
            { CardData.Trait.Ritual, new[] { 8, 12 } },
            { CardData.Trait.Hater, new[] { 6, 9 } },
            { CardData.Trait.SpellFocus, new[] { 6, 9, 12 } },
            { CardData.Trait.Combo, new[] { 5, 8 } },
            { CardData.Trait.Healer, new[] { 6, 9 } },
            { CardData.Trait.Inazuma, new[] { 11 } },
            { CardData.Trait.Pokemon, new[] { 10 } },
            { CardData.Trait.Neutral, new[] { 3, 6, 9, 20 } },
            { CardData.Trait.Blizzard, new[] { 5, 10 } },
            { CardData.Trait.Meme, new[] { 6, 9 } }
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
                    case CardData.Trait.Speedster:
                        traitDisplay.gemSlot.color = new Color(0.15f, 0.55f, 1.00f);
                        traitDisplay.iconSlot.sprite = traitDisplay.speedsterIcon;
                        break;
                    case CardData.Trait.Gunner:
                        traitDisplay.gemSlot.color = new Color(0.85f, 0.20f, 0.20f);
                        traitDisplay.iconSlot.sprite = traitDisplay.gunnerIcon;
                        break;
                    case CardData.Trait.SpellFocus:
                        traitDisplay.gemSlot.color = new Color(0.70f, 0.35f, 1.00f);
                        traitDisplay.iconSlot.sprite = traitDisplay.spellFocusIcon;
                        break;
                    case CardData.Trait.Faith:
                        traitDisplay.gemSlot.color = new Color(1.00f, 0.85f, 0.35f) ;
                        traitDisplay.iconSlot.sprite = traitDisplay.faithIcon;
                        break;
                    case CardData.Trait.Hater:
                        traitDisplay.gemSlot.color = new Color(0.25f, 0.25f, 0.25f);
                        traitDisplay.iconSlot.sprite = traitDisplay.haterIcon;
                        break;
                    case CardData.Trait.Combo:
                        traitDisplay.gemSlot.color = new Color(1.00f, 0.55f, 0.15f);
                        traitDisplay.iconSlot.sprite = traitDisplay.comboIcon;
                        break;
                    case CardData.Trait.Healer:
                        traitDisplay.gemSlot.color = new Color(0.35f, 0.85f, 0.60f) ;
                        traitDisplay.iconSlot.sprite = traitDisplay.healIcon;
                        break;
                    case CardData.Trait.Workout:
                        traitDisplay.gemSlot.color = new Color(0.75f, 0.75f, 0.75f);
                        traitDisplay.iconSlot.sprite = traitDisplay.workoutIcon;
                        break;
                    case CardData.Trait.Inazuma:
                        traitDisplay.gemSlot.color = new Color(1.00f, 0.92f, 0.20f);
                        traitDisplay.iconSlot.sprite = traitDisplay.inazumaIcon;
                        break;
                    case CardData.Trait.Blizzard:
                        traitDisplay.gemSlot.color = new Color(0.55f, 0.85f, 1.00f);
                        traitDisplay.iconSlot.sprite = traitDisplay.blizzardIcon;
                        break;
                    case CardData.Trait.Meme:
                        traitDisplay.gemSlot.color = new Color(1.00f, 0.35f, 0.85f);
                        traitDisplay.iconSlot.sprite = traitDisplay.memeIcon;
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
