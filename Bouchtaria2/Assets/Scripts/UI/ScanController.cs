using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class ScanController : MonoBehaviour
{
    public static ScanController Instance;

    [SerializeField] private ScanPanelView scanPanelPrefab;

    private ScanPanelView panelInstance;
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
        if (ScanInput.Instance == null || !ScanInput.Instance.IsScanActive)
        {
            panelInstance.Hide();
            return;
        }

        CardView cardUnderMouse = GetCardUnderMouse();

        if (cardUnderMouse != null && (UserCollectionManager.Instance.IsOwned(cardUnderMouse.CardData.id) || SceneManager.GetActiveScene().name != "Collection"))
        {
            panelInstance.owner = cardUnderMouse.GetComponent<CardInstance>().Owner;
            if(panelInstance.owner==PlayerOwner.Player || cardUnderMouse.GetComponent<CardInstance>().CurrentZone != CardZone.Hand)
                panelInstance.Show(cardUnderMouse);
        }
        else
        {
            panelInstance.Hide();
        }
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
