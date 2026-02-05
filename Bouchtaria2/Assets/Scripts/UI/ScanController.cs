using UnityEngine;
using UnityEngine.EventSystems;
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

        if (cardUnderMouse != null)
        {
            panelInstance.owner = cardUnderMouse.GetComponent<CardInstance>().Owner;
            if(panelInstance.owner==PlayerOwner.Player)
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
        int bestSortingOrder = int.MinValue;

        foreach (Collider2D col in hits)
        {
            CardView view = col.GetComponent<CardView>();
            if (view == null)
                continue;

            SpriteRenderer sr = view.GetComponentInChildren<SpriteRenderer>();
            if (sr == null)
                continue;

            if (sr.sortingOrder > bestSortingOrder)
            {
                bestSortingOrder = sr.sortingOrder;
                bestCard = view;
            }
        }

        return bestCard;
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
