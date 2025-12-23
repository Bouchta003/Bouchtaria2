using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;

public class CollectionScreen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform gridRoot;
    [SerializeField] private CardView cardViewPrefab;
    [SerializeField] private Transform layoutAnchor;
    [SerializeField] private TextMeshProUGUI ownedButtonLabel;

    [Header("Pagination")]
    [SerializeField] private int cardsPerPage = 4;
    [SerializeField] private int cardsPerRow = 2;
    [SerializeField] private float spacingX = 4f;
    [SerializeField] private float spacingY = 5f;

    private List<CardData> allCards;
    private int currentPage = 0;
    private CollectionFilter currentFilter = CollectionFilter.All;

    private enum CollectionFilter
    {
        All,
        OwnedOnly
    }
    private IEnumerator Start()
    {
        yield return new WaitUntil(() =>
            GameFlowController.Instance.IsGameReady
        );
        ClearGrid();
        ShowPage(0);
    }

    /*    void Start()
    {
        if (CardDatabase.Instance == null || UserCollectionManager.Instance == null)
        {
            Debug.LogError("❌ CollectionScreen loaded without game being ready");
            return;
        }
        allCards = new List<CardData>(CardDatabase.Instance.Cards.Values);
        ShowPage(0);
        //PopulateAllCards();
    }*/
    private void Update()
    {
        if (currentFilter == CollectionFilter.All) ownedButtonLabel.text = "All \nCards";
        else ownedButtonLabel.text = "Owned Cards";
    }
    private void RefreshCurrentPage()
    {
        foreach (Transform child in gridRoot)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null)
                view.Refresh();
        }
    }

    private void ShowPage(int pageIndex)
    {
        ClearGrid();

        // 🔹 Get cards based on current filter (ALL or OWNED)
        List<CardData> filteredCards = GetFilteredCards();

        currentPage = Mathf.Clamp(
            pageIndex,
            0,
            GetMaxPageIndex(filteredCards)
        );

        int startIndex = currentPage * cardsPerPage;
        int endIndex = Mathf.Min(startIndex + cardsPerPage, filteredCards.Count);

        Vector2 pageSize = GetFixedPageDimensions();

        Vector3 pageOffset = new Vector3(
            -pageSize.x / 2f,
            pageSize.y / 2f,
            0f
        );

        int visibleIndex = 0;

        for (int i = startIndex; i < endIndex; i++)
        {
            CardData card = filteredCards[i];

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

        Debug.Log(
            $"📄 Page {currentPage + 1} / {GetMaxPageIndex(filteredCards) + 1} | Filter: {currentFilter}"
        );
    }
    private int GetMaxPageIndex(List<CardData> cards)
    {
        return Mathf.Max(
            0,
            Mathf.CeilToInt((float)cards.Count / cardsPerPage) - 1
        );
    }

    private void ClearGrid()
    {
        for (int i = gridRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(gridRoot.GetChild(i).gameObject);
        }
    }
    private Vector2 GetFixedPageDimensions()
    {
        int totalRows = Mathf.CeilToInt((float)cardsPerPage / cardsPerRow);

        float width = (cardsPerRow - 1) * spacingX;
        float height = (totalRows - 1) * spacingY;

        return new Vector2(width, height);
    }
    private List<CardData> GetFilteredCards()
    {
        List<CardData> result = new List<CardData>();

        foreach (var card in CardDatabase.Instance.Cards.Values)
        {
            if (currentFilter == CollectionFilter.OwnedOnly &&
                !UserCollectionManager.Instance.IsOwned(card.id))
            {
                continue;
            }

            result.Add(card);
        }

        return result;
    }
    private void OnEnable()
    {
        UserCollectionManager.Instance.OnCollectionUpdated += RefreshCurrentPage;
    }
    private void OnDisable()
    {
        if (UserCollectionManager.Instance != null)
            UserCollectionManager.Instance.OnCollectionUpdated -= RefreshCurrentPage;
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
    public void ToggleOwnedFilter()
    {
        currentFilter = currentFilter == CollectionFilter.All
            ? CollectionFilter.OwnedOnly
            : CollectionFilter.All;

        currentPage = 0;
        ShowPage(0);
    }


}
