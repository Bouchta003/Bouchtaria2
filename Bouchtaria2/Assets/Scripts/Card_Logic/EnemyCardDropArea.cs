using UnityEngine;

public class EnemyCardDropArea : MonoBehaviour, ICardDropArea
{
    public void OnCardDrop(Card card)
    {
        card.transform.position = transform.position;
        card.transform.rotation = Quaternion.identity;
        Debug.Log("Card dropped in enemy slot");
    }
}
