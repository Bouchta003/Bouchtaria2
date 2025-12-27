using UnityEngine;
using System.Collections.Generic;
using System;

public class TraitsDetection : MonoBehaviour
{
    Dictionary<CardData.Trait, int> traitsTiers;

    [Header("TraitSlot")]
    [SerializeField] GameObject traitSlotPrefab;
    [SerializeField] GameObject traitLayoutAlly;
    [SerializeField] GameObject traitLayoutEnemy;

    bool TryParseTrait(string traitString, out CardData.Trait trait)
    {
        return Enum.TryParse(traitString, true, out trait);
    }

    public Dictionary<CardData.Trait, int> RetrieveTraitTiersFromDeck(Queue<CardData> playerDeck, PlayerOwner owner)
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
            { CardData.Trait.Neutral, new[] { 4,8,15 } },
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
                GameObject traitPrefab;

                if (owner == PlayerOwner.Player)
                {
                    traitPrefab = Instantiate(traitSlotPrefab, traitLayoutAlly.transform);
                }
                else
                {
                    traitPrefab = Instantiate(traitSlotPrefab, traitLayoutEnemy.transform);
                }

                TraitsDisplay traitDisplay = traitPrefab.GetComponentInChildren<TraitsDisplay>();

                traitDisplay.thisTrait = trait; 
                traitDisplay.tier = tier;
                switch (tier)
                {
                    case 1:
                        traitDisplay.frameRaritySlot.sprite = traitDisplay.bronzeTraitSprite;break;
                    case 2:
                        traitDisplay.frameRaritySlot.sprite = traitDisplay.silverTraitSprite;break;
                    default:
                        traitDisplay.frameRaritySlot.sprite = traitDisplay.goldenTraitSprite;break;
                }
                traitDisplay.gemSlot.color = TraitColorDatabase.Get(trait);
                switch (trait)
                {
                    case CardData.Trait.Pokemon:
                        traitDisplay.iconSlot.sprite = traitDisplay.pokemonIcon;
                        break;
                    case CardData.Trait.Neutral:
                        traitDisplay.iconSlot.sprite = traitDisplay.neutralIcon;
                        break;
                    case CardData.Trait.Speedster:
                        traitDisplay.iconSlot.sprite = traitDisplay.speedsterIcon;
                        break;
                    case CardData.Trait.Gunner:
                        traitDisplay.iconSlot.sprite = traitDisplay.gunnerIcon;
                        break;
                    case CardData.Trait.SpellFocus:
                        traitDisplay.iconSlot.sprite = traitDisplay.spellFocusIcon;
                        break;
                    case CardData.Trait.Faith:
                        traitDisplay.iconSlot.sprite = traitDisplay.faithIcon;
                        break;
                    case CardData.Trait.Hater:
                        traitDisplay.iconSlot.sprite = traitDisplay.haterIcon;
                        break;
                    case CardData.Trait.Combo:
                        traitDisplay.iconSlot.sprite = traitDisplay.comboIcon;
                        break;
                    case CardData.Trait.Healer:
                        traitDisplay.iconSlot.sprite = traitDisplay.healIcon;
                        break;
                    case CardData.Trait.Workout:
                        traitDisplay.iconSlot.sprite = traitDisplay.workoutIcon;
                        break;
                    case CardData.Trait.Inazuma:
                        traitDisplay.iconSlot.sprite = traitDisplay.inazumaIcon;
                        break;
                    case CardData.Trait.Blizzard:
                        traitDisplay.iconSlot.sprite = traitDisplay.blizzardIcon;
                        break;
                    case CardData.Trait.Meme:
                        traitDisplay.iconSlot.sprite = traitDisplay.memeIcon;
                        break;
                    default:
                        Debug.Log("Unkown trait");
                        break;
                }
            }
        }

        return resolvedTiers;
    }

}
