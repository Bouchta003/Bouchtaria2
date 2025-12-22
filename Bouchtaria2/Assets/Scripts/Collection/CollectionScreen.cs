using UnityEngine;

public class CollectionScreen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform gridRoot;
    [SerializeField] private CardView cardViewPrefab;

    [Header("Layout")]
    [SerializeField] private int cardsPerRow = 5;
    [SerializeField] private float spacingX = 2.2f;
    [SerializeField] private float spacingY = 3.2f;

    void Start()
    {
        if (CardDatabase.Instance == null || UserCollectionManager.Instance == null)
        {
            Debug.LogError("❌ CollectionScreen loaded without game being ready");
            return;
        }

        PopulateAllCards();
    }


    private void PopulateAllCards()
    {
        int index = 0;

        foreach (var card in CardDatabase.Instance.Cards.Values)
        {
            Vector3 position = CalculateGridPosition(index);

            CardView view = Instantiate(cardViewPrefab, gridRoot);
            view.transform.localPosition = position;
            view.Setup(card);

            index++;
        }

        Debug.Log($"🃏 Collection loaded ({index} total cards)");
    }


    private Vector3 CalculateGridPosition(int index)
    {
        int row = index / cardsPerRow;
        int col = index % cardsPerRow;

        return new Vector3(
            col * spacingX,
            -row * spacingY,
            0f
        );
    }
}
