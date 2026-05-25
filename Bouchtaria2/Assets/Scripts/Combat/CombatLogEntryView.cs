using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CombatLogEntryView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text logText;
    [SerializeField] private Image scannableImage;
    [SerializeField] private Image bgOwner;

    private CardInstance cardInstance;
    private CardData fallbackCardData;
    private PlayerOwner owner;

    public CardInstance CardInstance => cardInstance;

    private void Awake()
    {
        if (logText == null)
            logText = GetComponentInChildren<TMP_Text>(true);

        if (scannableImage == null)
        {
            foreach (Image img in GetComponentsInChildren<Image>(true))
            {
                if (img.CompareTag("Scannable_log"))
                {
                    scannableImage = img;
                    break;
                }
            }
        }

        if (bgOwner == null)
        {
            Transform bgOwnerTransform = transform.Find("bgOwner");
            if (bgOwnerTransform != null)
                bgOwner = bgOwnerTransform.GetComponent<Image>();
        }
    }

    public void Initialize(CardInstance instance, CardData cardData, PlayerOwner entryOwner, string initialText)
    {
        cardInstance = instance;
        fallbackCardData = cardData != null ? cardData : instance != null ? instance.Data : null;
        owner = entryOwner;

        SetText(initialText);
        RefreshVisuals();
    }

    public void AppendText(string extraText)
    {
        if (string.IsNullOrWhiteSpace(extraText) || logText == null)
            return;

        if (string.IsNullOrWhiteSpace(logText.text))
            logText.text = extraText;
        else
            logText.text += "\n" + extraText;
    }

    public void SetText(string text)
    {
        if (logText != null)
            logText.text = text;
    }

    private void RefreshVisuals()
    {
        if (bgOwner != null)
            bgOwner.color = owner == PlayerOwner.Enemy ? Color.red : Color.blue;

        if (scannableImage != null)
        {
            CardData source = cardInstance != null ? cardInstance.Data : fallbackCardData;
            if (source != null)
                scannableImage.sprite = source.artSpriteCompact;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ScanController.Instance == null || ScanController.Instance.panelInstance == null)
            return;

        if (ScanInput.Instance != null && !ScanInput.Instance.IsScanActive)
            return;

        if (ScanInput.Instance == null && !Input.GetKey(KeyCode.Space))
            return;

        if (cardInstance != null && cardInstance.Owner == PlayerOwner.Enemy && cardInstance.CurrentZone == CardZone.Hand)
            return;

        CardData source = cardInstance != null ? cardInstance.Data : fallbackCardData;
        if (source == null)
            return;

        ScanController.Instance.panelInstance.owner = owner;
        ScanController.Instance.panelInstance.Show(source, owner);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ScanController.Instance == null || ScanController.Instance.panelInstance == null)
            return;

        ScanController.Instance.panelInstance.Hide();
    }
}
