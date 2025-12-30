using System.Collections.Generic;
using UnityEngine;

public interface ICardDropArea {
    void OnCardDrop(Card card);
    bool HasProtectUnits();
    bool IsFull();
    List<GameObject> GetCards();
    PlayerOwner Owner { get; }
}
