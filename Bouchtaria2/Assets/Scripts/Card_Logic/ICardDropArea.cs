using System.Collections.Generic;
using UnityEngine;

public interface ICardDropArea {
    void OnCardDrop(Card card);
    bool HasProtectUnits();
    bool IsFull();
    Transform CardContainer { get; set; }
    List<GameObject> GetCards();
    CardInstance BoardHasEffect(string effect);
    CardInstance BoardHasID(int id);
    PlayerOwner Owner { get; }
}
