using UnityEngine;
using TMPro;
using System.Collections;

public class ScanPanelView : MonoBehaviour
{
    [Header("Animated Panel Root")]
    [SerializeField] private RectTransform panelRoot; // ASSIGN IN INSPECTOR

    [Header("UI Content")]
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private Transform keywordContainer;
    [SerializeField] private GameObject keywordPrefab;

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private float hiddenX = -700f;
    [SerializeField] private float visibleX = 20f;

    private Coroutine slideRoutine;
    private bool isVisible;

    void Awake()
    {
        if (panelRoot == null)
        {
            Debug.LogError("❌ ScanPanelView: panelRoot not assigned");
            enabled = false;
            return;
        }

        // Start hidden (off-screen)
        Vector3 pos = panelRoot.localPosition;
        pos.x = hiddenX;
        panelRoot.localPosition = pos;

        isVisible = false;
    }

    // =========================
    // Public API
    // =========================

    public void Show(CardData card)
    {
        Populate(card);
        Slide(true);
    }

    public void Hide()
    {
        Slide(false);
    }

    // =========================
    // Animation
    // =========================

    private void Slide(bool show)
    {
        if (isVisible == show)
            return;

        isVisible = show;

        if (slideRoutine != null)
            StopCoroutine(slideRoutine);
        Debug.Log("Sliiiding"+show);
        slideRoutine = StartCoroutine(SlideRoutine(show));
    }

    private IEnumerator SlideRoutine(bool show)
    {
        Vector3 start = panelRoot.localPosition;
        Vector3 target = start;
        target.x = show ? visibleX : hiddenX;

        float t = 0f;

        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / slideDuration);
            panelRoot.localPosition = Vector3.Lerp(start, target, k);
            yield return null;
        }

        panelRoot.localPosition = target;
        slideRoutine = null;
    }

    // =========================
    // Content
    // =========================

    private void Populate(CardData card)
    {
        if (card == null)
            return;

        effectText.text = card.effectText;

        foreach (Transform child in keywordContainer)
            Destroy(child.gameObject);

        //KeyWordCheck
        string[] keywordList = card.effect.Split('/');
        foreach(string keyword in keywordList)
        {
            if (CardHasKeyword(card, keyword))
            {
                GameObject entry = Instantiate(keywordPrefab, keywordContainer);
                GameObject keyName = entry.transform.GetChild(0).gameObject;
                GameObject keyDescription = entry.transform.GetChild(1).gameObject;
                switch (keyword.ToLower())
                {
                    case "protect":
                        keyName.GetComponent<TextMeshProUGUI>().text = "Protect";
                        keyDescription.GetComponent<TextMeshProUGUI>().text = "Forces the enemies to attack this unit. \nSpells aren't affected by Taunt.";
                        break;
                    case "deploy":
                        keyName.GetComponent<TextMeshProUGUI>().text = "Deploy";
                        keyDescription.GetComponent<TextMeshProUGUI>().text = "Does something when summonned";
                        break;
                    case "quickstrike":
                        keyName.GetComponent<TextMeshProUGUI>().text = "QuickStrike";
                        keyDescription.GetComponent<TextMeshProUGUI>().text = "Can attack minions the turn it was summonned.";
                        break;
                    case "haste":
                        keyName.GetComponent<TextMeshProUGUI>().text = "Haste";
                        keyDescription.GetComponent<TextMeshProUGUI>().text = "Can attack twice per turn \n(non stackable).";
                        break;
                    default:
                        Debug.Log("UnknownKeyword : " + keyword);
                        break;
                }
            }
        }
    }
    public bool CardHasKeyword(CardData card, string keywordString)
    {
        if (card == null || card.effect == null)
            return false;

        if (System.Enum.TryParse(keywordString, true, out CardData.KeyWords parsedKeyword))
        {
            return true;
        }

        return false;
    }

}
