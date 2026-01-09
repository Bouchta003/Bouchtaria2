using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class CardPack
{
    public string packId;
    public int cost = 100;
    public int cardCount = 5;
    public List<int> possibleCardIds;
}
