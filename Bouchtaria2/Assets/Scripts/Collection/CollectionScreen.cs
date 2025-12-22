using System.Collections.Generic;
using UnityEngine;

public class CollectionScreen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform gridRoot;
    [SerializeField] private CardView cardViewPrefab;

    [Header("Pagination")]
    [SerializeField] private int cardsPerPage = 4;
    [SerializeField] private int cardsPerRow = 2;
    [SerializeField] private float spacingX = 2.2f;
    [SerializeField] private float spacingY = 3.2f;

    private List<CardData> allCards;
    private int currentPage = 0;

    void Start()
    {
        if (CardDatabase.Instance == null || UserCollectionManager.Instance == null)
        {
            Debug.LogError("❌ CollectionScreen loaded without game being ready");
            return;
        }
        allCards = new List<CardData>(CardDatabase.Instance.Cards.Values);
        ShowPage(0);
        //PopulateAllCards();
    }

    private void ShowPage(int pageIndex)
    {
        ClearGrid();

        currentPage = Mathf.Clamp(
            pageIndex,
            0,
            GetMaxPageIndex()
        );

        int startIndex = currentPage * cardsPerPage;
        int endIndex = Mathf.Min(startIndex + cardsPerPage, allCards.Count);

        int visibleIndex = 0;

        for (int i = startIndex; i < endIndex; i++)
        {
            CardData card = allCards[i];

            Vector3 position = CalculateGridPosition(visibleIndex);

            CardView view = Instantiate(cardViewPrefab, gridRoot);
            view.transform.localPosition = position;
            view.Setup(card);

            visibleIndex++;
        }

        Debug.Log($"📄 Page {currentPage + 1} / {GetMaxPageIndex() + 1}");
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
    private void ClearGrid()
    {
        for (int i = gridRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(gridRoot.GetChild(i).gameObject);
        }
    }
    private int GetMaxPageIndex()
    {
        return Mathf.Max(0, Mathf.CeilToInt((float)allCards.Count / cardsPerPage) - 1);
    }
    public void NextPage()
    {
        Debug.Log("Next page");
        ShowPage(currentPage + 1);
    }

    public void PreviousPage()
    {
        Debug.Log("Previous page");
        ShowPage(currentPage - 1);
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

}
