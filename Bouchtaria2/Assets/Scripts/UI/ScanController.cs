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
        bool shouldShow =
            ScanInput.Instance != null &&
            ScanInput.Instance.IsScanActive &&
            hoveredCard != null;

        if (shouldShow)
        {
            panelInstance.Show(hoveredCard.CardData);
        }
        else if (!shouldShow)
        {
            panelInstance.Hide();
        }
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
