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

    public string effect;
    public string effectText;

    public string artPath;
    public string artCompactPath;

    public bool packable;
    public bool token;
    public bool signature;
    public enum Trait
    {
        Neutral, Speedster, Inazuma, Pokemon, Blizzard, Workout, Gunners, Faith, Ritual, Hater, SpellFocus, Combo, Healer, Meme
    }
    public enum KeyWords
    {
        Taunt, Quickstrike, Blessed, Thorns, Haste, Berserk
    }
    // Runtime-only
    [System.NonSerialized] public Sprite artSprite;
}
