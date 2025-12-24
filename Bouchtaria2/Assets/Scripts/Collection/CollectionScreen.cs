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
    [SerializeField] private TMP_InputField nameResearchText;

    [Header("Pagination")]
    [SerializeField] private int cardsPerPage = 4;
    [SerializeField] private int cardsPerRow = 2;
    [SerializeField] private float spacingX = 4f;
    [SerializeField] private float spacingY = 5f;

    private List<CardData> allCards;
    private string filterText = "";
    private int filterValue = 0;
    private int currentPage = 0;
    public enum Filter { None, Ownership, Name, Mana, Attack, Health, Trait, Type}
    List<Filter> currentFilters = new List<Filter>();
    private IEnumerator Start()
    {
        yield return new WaitUntil(() =>
            GameFlowController.Instance.IsGameReady
        );
        currentFilters.Clear();currentFilters.Add(Filter.None);
        ClearGrid();
        ShowPage(0);
    }

    private void Update()
    {
        if (currentFilters.Contains(Filter.None)) ownedButtonLabel.text = "All \nCards";
        else ownedButtonLabel.text = "Owned Cards";
        //if input empty, remove text filter or add a cross to negate that one
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
        List<CardData> filteredCards = GetFilteredCards(currentFilters,filterText, filterValue);

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

            view.Init(card);
            visibleIndex++;
        }

        Debug.Log(
            $"📄 Page {currentPage + 1} / {GetMaxPageIndex(filteredCards) + 1} | Filter: {currentFilters}"
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
    private List<CardData> GetFilteredCards(
    List<Filter> filterTypes,
    string filterText,
    int value
)
    {
        List<CardData> result = new List<CardData>();

        foreach (var card in CardDatabase.Instance.Cards.Values)
        {
            // -------------------------
            // OWNERSHIP FILTER
            // -------------------------
            if (filterTypes.Contains(Filter.Ownership))
            {
                if (!UserCollectionManager.Instance.IsOwned(card.id))
                    continue; // reject card
            }

            // -------------------------
            // NAME / TEXT FILTER
            // -------------------------
            if (filterTypes.Contains(Filter.Name))
            {
                if (string.IsNullOrEmpty(filterText))
                    continue;

                bool match =
                    card.name.ToLower().Contains(filterText.ToLower()) ||
                    card.effectText.ToLower().Contains(filterText.ToLower());

                if (!match)
                    continue;
            }

            // -------------------------
            // MANA FILTER
            // -------------------------
            if (filterTypes.Contains(Filter.Mana))
            {
                if (card.manaCost >= value) // "< 2" logic
                    continue;
            }

            // -------------------------
            // ATTACK FILTER (example)
            // -------------------------
            if (filterTypes.Contains(Filter.Attack))
            {
                if (card.atkValue != value)
                    continue;
            }

            // -------------------------
            // HEALTH FILTER (example)
            // -------------------------
            if (filterTypes.Contains(Filter.Health))
            {
                if (card.hpValue != value)
                    continue;
            }

            // If we reach here → card passed ALL active filters
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
    private void NormalizeFilters()
    {
        if (currentFilters.Count > 1 && currentFilters.Contains(Filter.None))
            currentFilters.Remove(Filter.None);

        if (currentFilters.Count == 0)
            currentFilters.Add(Filter.None);
    }

    public void ToggleOwnedFilter()
    {
        if (currentFilters.Contains(Filter.Ownership))
            currentFilters.Remove(Filter.Ownership);
        else
            currentFilters.Add(Filter.Ownership);

        NormalizeFilters();
        currentPage = 0;
        ShowPage(0);
    }

    public void ToggleTextFilter()
    {
        currentFilters.Add(Filter.Name);
        filterText = nameResearchText.text;
        currentPage = 0;
        NormalizeFilters();
        ShowPage(0);
    }
    public void ToggleManaFilter(int manaValue)
    {
        if (!currentFilters.Contains(Filter.Mana)) { currentFilters.Add(Filter.Mana); filterValue = manaValue; }
        else { currentFilters.Remove(Filter.Mana); filterValue = -1; }
        
        NormalizeFilters();
        currentPage = 0;
        ShowPage(0);
    }

}
