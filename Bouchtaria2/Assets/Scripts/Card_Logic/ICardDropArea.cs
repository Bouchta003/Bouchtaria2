using System.Collections.Generic;
using UnityEngine;

public interface ICardDropArea {
    void OnCardDrop(Card card);
    bool HasProtectUnits();
    List<GameObject> GetCards();
    PlayerOwner Owner { get; }
}
