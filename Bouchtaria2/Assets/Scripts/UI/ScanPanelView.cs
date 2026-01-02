using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ScanPanelView : MonoBehaviour
{
    [Header("Animated Panel Root")]
    [SerializeField] private RectTransform panelRoot; // ASSIGN IN INSPECTOR

    [Header("UI Content")]
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Transform keywordContainer;
    [SerializeField] private Transform relatedContainer;
    [SerializeField] private GameObject keywordPrefab;
    [SerializeField] private GameObject relatedPrefab;
    List<int> relatedCardsId = new List<int>();

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private float hiddenX = -700f;
    [SerializeField] private float visibleX = 20f;

    private Coroutine slideRoutine;
    private bool isVisible;
    public PlayerOwner owner;

    void Awake()
    {
        if (panelRoot == null)
        {
            Debug.LogError("❌ ScanPanelView: panelRoot not assigned");
            enabled = false;
            return;
        }
        GetComponent<Canvas>().worldCamera = FindFirstObjectByType<Camera>();
        // Start hidden (off-screen)
        Vector3 pos = panelRoot.localPosition;
        pos.x = hiddenX;
        panelRoot.localPosition = pos;

        isVisible = false;
    }

    // =========================
    // Public API
    // =========================

    public void Show(CardView card)
    {
        if (card.GetComponent<CardInstance>().CurrentZone == CardZone.Board)
            PopulateBoard(card);
        else PopulateHand(card);
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

    private void PopulateHand(CardView cardView)
    {
        if (cardView == null)
            return;

        CardData card = cardView.CardData;


        nameText.text = card.name;
        effectText.text = card.effectText;

        foreach (Transform child in keywordContainer)
            Destroy(child.gameObject);
        //KeyWordCheck
        string[] keywordList = card.effect.Split(' ');
        UpdateTexts(keywordList);
        DisplayRelatedCards();
    }
    private void PopulateBoard(CardView cardView)
    {
        if (cardView == null)
            return;

        CardInstance card = cardView.GetComponent<CardInstance>();


        nameText.text = card.Data.name;
        effectText.text = card.CurrentEffectText;

        foreach (Transform child in keywordContainer)
            Destroy(child.gameObject);
        //KeyWordCheck
        string[] keywordList = card.CurrentEffect.Split(' ');

        UpdateTexts(keywordList);
        DisplayRelatedCards();
    }
    void DisplayRelatedCards()
    {
        foreach (int id in relatedCardsId)
        {
            GameObject entry = Instantiate(relatedPrefab, relatedContainer);
            CardData data = CardDatabase.Instance.GetCardById(id);
            entry.GetComponentInChildren<TextMeshProUGUI>().text = data.effectText;
            //CardFactory.Instance.CreateCardInPosition(data,owner, new Vector3(-150,0,0), new Vector3(20,20,20), entry.transform);
        }
        relatedCardsId.Clear();
    }
    private void UpdateTexts(string[] keywordList)
    {
        foreach (string raw in keywordList)
        {
            string keyword = raw.ToLowerInvariant();
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();

            if (keyword.Contains("protect"))
            {
                keyName.text = "Protect";
                keyDescription.text =
                    "Forces enemies to attack this unit.";
            }
            else if (keyword.Contains("morphto"))
            {
                keyName.text = "Morph";
                keyDescription.text =
                    "Transforms into another unit and keeps damage.";
                relatedCardsId.Add(GetEffectID(keyword));
            }
            else if (keyword.Contains("monsterpart"))
            {
                keyName.text = "Monster Parts";
                keyDescription.text =
                    "Cannot be used, get other monster parts to assemble into gear.";
                relatedCardsId.Add(GetEffectID(keyword));
            }
            else if (keyword.Contains("blessed"))
            {
                keyName.text = "Blessed";
                keyDescription.text =
                    "Has a divine shield that absorbs one instance of damage.";
                relatedCardsId.Add(GetEffectID(keyword));
            }
            else if (keyword.Contains("hidden"))
            {
                keyName.text = "Hidden";
                keyDescription.text =
                    "Cannot be target by attacks until this unit attacks.";
                relatedCardsId.Add(GetEffectID(keyword));
            }
            else if (keyword.Contains("quickstrike"))
            {
                keyName.text = "QuickStrike";
                keyDescription.text =
                    "Can attack the turn it is summoned.";
            }
            else if (keyword.Contains("haste"))
            {
                keyName.text = "Haste";
                keyDescription.text =
                    "Can attack twice per turn (not stackable).";
            }
            else if (keyword.Contains("summon"))
            {
                keyName.text = "Summon";
                keyDescription.text =
                    "Summons a unit without triggering its Deploy effect.";
                relatedCardsId.Add(GetEffectID(keyword));
            }
            else
            {
                Destroy(entry);
            }
        }
    }
    private int GetEffectID(string effect)
    {
        int start = effect.IndexOf('(');
        int end = effect.IndexOf(')');

        if (start < 0 || end < 0 || end <= start + 1)
        {
            Debug.LogError($"Malformed effect");
            return -1;
        }
        string valueStr = effect.Substring(start + 1, end - start - 1);
        if (int.TryParse(valueStr, out int value))
        {
            return value;
        }

        Debug.LogError(
            $"Invalid parameter '{valueStr}' on effect {effect}");
        return -1;
    }
}
