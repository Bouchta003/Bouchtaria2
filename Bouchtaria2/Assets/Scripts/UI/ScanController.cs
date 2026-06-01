using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class ScanController : MonoBehaviour
{
    public static ScanController Instance;

    [SerializeField] private ScanPanelView scanPanelPrefab;

    public ScanPanelView panelInstance;
    private CardView hoveredCard;

    private void Awake()
    {
        Instance = this;

        panelInstance = Instantiate(scanPanelPrefab);
        //panelInstance.gameObject.SetActive(false);
        DontDestroyOnLoad(panelInstance.gameObject);
    }
    private void Update()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        bool pathOfPowerScene = activeScene == "PathofPower";
        bool cardScanActive = IsScanActive();

        if (!cardScanActive)
        {
            panelInstance.Hide();
            return;
        }

        CardView cardUnderMouse = GetCardUnderMouse();
        bool canShowCollectionCard = cardUnderMouse != null &&
                                     (activeScene != "Collection" ||
                                      (UserCollectionManager.Instance != null && (UserCollectionManager.Instance.IsOwned(cardUnderMouse.CardData.id)) || FindFirstObjectByType<CollectionScreen>().isDeck));

        if (canShowCollectionCard)
        {
            CardInstance cardInstance = cardUnderMouse.GetComponent<CardInstance>();
            if (cardInstance == null)
                return;

            panelInstance.owner = cardInstance.Owner;
            if(panelInstance.owner==PlayerOwner.Player || cardInstance.CurrentZone != CardZone.Hand)
                panelInstance.Show(cardUnderMouse);
        }
        else if (panelInstance != null && panelInstance.isShowingRelic)
        {
            // keep relic hover scans visible until the relic hover target clears them
        }
        else if (!CombatLogEntryView.IsAnyLogEntryHovered && !pathOfPowerScene && cardUnderMouse == null)
        {
            panelInstance.Hide();
        }
    }

    private bool IsScanActive()
    {
        if (ScanInput.Instance != null)
            return ScanInput.Instance.IsScanActive;

        return !UIInputFocusTracker.IsAnyTMPFocused && Input.GetKey(KeyCode.Space);
    }

    private CardView GetCardUnderMouse()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Collider2D[] hits = Physics2D.OverlapPointAll(mousePos);

        if (hits.Length == 0)
            return null;

        CardView bestCard = null;
        int bestSortingLayerValue = int.MinValue;
        int bestSortingOrder = int.MinValue;

        foreach (Collider2D col in hits)
        {
            CardView view = col.GetComponentInParent<CardView>();
            if (view == null)
                continue;

            int sortingLayerValue;
            int sortingOrder;
            if (!TryGetRenderSorting(view, out sortingLayerValue, out sortingOrder))
                continue;

            bool isHigherLayer = sortingLayerValue > bestSortingLayerValue;
            bool isSameLayerHigherOrder = sortingLayerValue == bestSortingLayerValue && sortingOrder > bestSortingOrder;
            if (isHigherLayer || isSameLayerHigherOrder)
            {
                bestSortingLayerValue = sortingLayerValue;
                bestSortingOrder = sortingOrder;
                bestCard = view;
            }
        }

        return bestCard;
    }

    private bool TryGetRenderSorting(CardView view, out int sortingLayerValue, out int sortingOrder)
    {
        sortingLayerValue = int.MinValue;
        sortingOrder = int.MinValue;

        SortingGroup sortingGroup = view.GetComponent<SortingGroup>();
        if (sortingGroup != null)
        {
            sortingLayerValue = SortingLayer.GetLayerValueFromID(sortingGroup.sortingLayerID);
            sortingOrder = sortingGroup.sortingOrder;
            return true;
        }

        SpriteRenderer topRenderer = null;
        foreach (SpriteRenderer renderer in view.GetComponentsInChildren<SpriteRenderer>())
        {
            if (topRenderer == null)
            {
                topRenderer = renderer;
                continue;
            }

            int rendererLayerValue = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);
            int topLayerValue = SortingLayer.GetLayerValueFromID(topRenderer.sortingLayerID);
            bool isHigherLayer = rendererLayerValue > topLayerValue;
            bool isSameLayerHigherOrder = rendererLayerValue == topLayerValue && renderer.sortingOrder > topRenderer.sortingOrder;
            if (isHigherLayer || isSameLayerHigherOrder)
                topRenderer = renderer;
        }

        if (topRenderer == null)
            return false;

        sortingLayerValue = SortingLayer.GetLayerValueFromID(topRenderer.sortingLayerID);
        sortingOrder = topRenderer.sortingOrder;
        return true;
    }

    public void OnCardHoverEnter(CardView card)
    {
        hoveredCard = card;
    }

    public void OnCardHoverExit(CardView card)
    {
        if (hoveredCard == card)
            hoveredCard = null;
    }
}
