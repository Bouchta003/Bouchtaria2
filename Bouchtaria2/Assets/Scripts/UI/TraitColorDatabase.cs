using System.Collections.Generic;
using UnityEngine;

public static class TraitColorDatabase
{
    private static readonly Dictionary<CardData.Trait, Color> Colors =
        new Dictionary<CardData.Trait, Color>
    {
        { CardData.Trait.Fighter,       new Color(0.85f, 0.12f, 0.12f) }, // strong crimson
        { CardData.Trait.Gunner,        new Color(0.95f, 0.55f, 0.10f) }, // muzzle-flash orange
        { CardData.Trait.Combo,     new Color(0.00f, 0.55f, 0.50f) }, // rogue teal (cunning/combo/thief)

        { CardData.Trait.SpellFocus,    new Color(0.65f, 0.30f, 0.95f) },
        { CardData.Trait.Faith,     new Color(0.95f, 0.95f, 0.80f) },
        { CardData.Trait.Avatar,        new Color(0.75f, 0.20f, 0.85f) }, // divine magenta

        { CardData.Trait.Healer,    new Color(0.20f, 0.85f, 0.45f) }, // vivid healing green
        { CardData.Trait.Speedster,     new Color(0.10f, 0.60f, 1.00f) }, // electric blue

        { CardData.Trait.Chaos,   new Color(0.62f, 0.12f, 0.94f) },
        { CardData.Trait.Neutral,   new Color(0.45f, 0.45f, 0.45f) },

        { CardData.Trait.Inazuma,   new Color(1, 0.90f, 0.10f) },
        { CardData.Trait.MonsterHunter,   new Color(0.5f, 0.65f, 0.40f) },
        { CardData.Trait.Pokemon,   new Color(0.90f, 0.4f, 0.4f) },
        { CardData.Trait.SoulForce, new Color(0.42f, 0.22f, 0.72f) },
        { CardData.Trait.Cozy, new Color(0.96f, 0.72f, 0.82f) },
        { CardData.Trait.Swordsman, new Color(0.78f, 0.82f, 0.88f) },
    };

    public static Color Get(CardData.Trait trait)
    {
        if (Colors.TryGetValue(trait, out Color color))
            return color;

        return Color.white; // safe fallback
    }
}
