using UnityEngine;

public class AllyCardDropArea : MonoBehaviour, ICardDropArea
{
    public void OnCardDrop(Card card)
    {
        card.transform.position = transform.position;
        card.transform.rotation = Quaternion.identity;
        Debug.Log("Card dropped in ally slot");
    }
}
