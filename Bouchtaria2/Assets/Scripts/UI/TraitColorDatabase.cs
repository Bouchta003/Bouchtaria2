using System.Collections.Generic;
using UnityEngine;

public static class TraitColorDatabase
{
    private static readonly Dictionary<CardData.Trait, Color> Colors =
        new Dictionary<CardData.Trait, Color>
    {
        { CardData.Trait.Speedster, new Color(0.10f, 0.60f, 1.00f) },
        { CardData.Trait.Gunner,    new Color(1.0f, 0.4f, 0.0f) },
        { CardData.Trait.Combo,     new Color(1.00f, 0.55f, 0.15f) },

        { CardData.Trait.SpellFocus,new Color(0.65f, 0.30f, 0.95f) },
        { CardData.Trait.Faith,     new Color(0.95f, 0.95f, 0.80f) },
        { CardData.Trait.Avatar,    new Color(0.50f, 0.05f, 0.55f) },

        { CardData.Trait.Hater,     new Color(0.22f, 0.22f, 0.22f) },
        { CardData.Trait.Healer,    new Color(0.25f, 0.75f, 0.45f) },

        { CardData.Trait.Fighter,   new Color(0.80f, 0.10f, 0.10f) },
        { CardData.Trait.Chaos,   new Color(0.62f, 0.12f, 0.94f) },
        { CardData.Trait.Neutral,   new Color(0.58f, 0.58f, 0.58f) },

        { CardData.Trait.Inazuma,   new Color(0.70f, 1.00f, 0.20f) },
        { CardData.Trait.MonsterHunter,   new Color(1f, 0.85f, 0.20f) },
        { CardData.Trait.Pokemon,   new Color(0.20f, 0.35f, 0.95f) },
        { CardData.Trait.Blizzard,  new Color(0.65f, 0.90f, 1.00f) }
    };

    public static Color Get(CardData.Trait trait)
    {
        if (Colors.TryGetValue(trait, out Color color))
            return color;

        return Color.white; // safe fallback
    }
}
