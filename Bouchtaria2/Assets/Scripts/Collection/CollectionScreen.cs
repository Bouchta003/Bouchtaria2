using System.Collections.Generic;
using UnityEngine;

public class CollectionScreen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform gridRoot;
    [SerializeField] private CardView cardViewPrefab;
    [SerializeField] private Transform layoutAnchor;

    [Header("Pagination")]
    [SerializeField] private int cardsPerPage = 4;
    [SerializeField] private int cardsPerRow = 2;
    [SerializeField] private float spacingX = 4f;
    [SerializeField] private float spacingY = 5f;

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
        Vector2 pageSize = GetFixedPageDimensions();

        Vector3 pageOffset = new Vector3(
            -pageSize.x / 2f,
            pageSize.y / 2f,
            0f
        );

        int visibleIndex = 0;

        for (int i = startIndex; i < endIndex; i++)
        {
            CardData card = allCards[i];

            int row = visibleIndex / cardsPerRow;
            int col = visibleIndex % cardsPerRow;

            Vector3 localPos = new Vector3(
                col * spacingX,
                -row * spacingY,
                0f
            );

            CardView view = Instantiate(cardViewPrefab, gridRoot);
            view.transform.position =
                layoutAnchor.position + pageOffset + localPos;

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
    private Vector2 GetPageDimensions(int visibleCardCount)
    {
        int rows = Mathf.CeilToInt((float)visibleCardCount / cardsPerRow);

        float width = (cardsPerRow - 1) * spacingX;
        float height = (rows - 1) * spacingY;

        return new Vector2(width, height);
    }
    private Vector2 GetFixedPageDimensions()
    {
        int totalRows = Mathf.CeilToInt((float)cardsPerPage / cardsPerRow);

        float width = (cardsPerRow - 1) * spacingX;
        float height = (totalRows - 1) * spacingY;

        return new Vector2(width, height);
    }

}
