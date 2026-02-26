using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CardData
{
    public int id;
    public string name;
    public string cardType;

    public int manaCost;
    public int atkValue;
    public int hpValue;

    public List<string> traits;
    public List<int> relatedCards = new List<int>();

    public string effect;
    public string effectText;

    public string artPath;
    public string artCompactPath;

    public bool packable;
    public bool token;
    public bool signature;
    public CardData()
    {
        relatedCards = new List<int>();
    }
    public enum SpellTargetType
    {
        None,
        Unit,
        Core,
        Any
    }

    public SpellTargetType spellTargetType;

    public enum Trait
    {
        Neutral, Speedster, Gunner, Inazuma, Pokemon,Fighter, Faith, Avatar, SpellFocus, Combo, Healer,
        MonsterHunter, Chaos
    }
    public enum KeyWords
    {
        Protect, Quickstrike, Blessed, Thorns, Haste, Berserk, Charge, Requiem, Deploy
    }
    // Runtime-only
    [System.NonSerialized] public Sprite artSprite;
    [System.NonSerialized] public Sprite artSpriteCompact;
}
