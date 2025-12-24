using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ScanPanelView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Main Sections")]
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private Transform keywordsContainer;
    [SerializeField] private GameObject keywordEntryPrefab;

    private CardData currentCard;

    private void Awake()
    {
        HideImmediate();
    }

    /// <summary>
    /// Display scan information for a card
    /// </summary>
    public void Show(CardData cardData)
    {
        if (cardData == null)
            return;

        currentCard = cardData;

        Debug.Log($"📖 ScanPanelView.Show → {cardData.name}");

        gameObject.SetActive(true);

        PopulateEffect(cardData);
        PopulateKeywords(cardData);
    }

    /// <summary>
    /// Hide panel (safe for animations later)
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Hide panel immediately (scene change / cancel)
    /// </summary>
    public void HideImmediate()
    {
        root.SetActive(false);
        currentCard = null;
    }

    // -------------------------
    // Population
    // -------------------------

    private void PopulateEffect(CardData cardData)
    {
        if (effectText == null)
            return;

        effectText.text = string.IsNullOrEmpty(cardData.effectText)
            ? "No effect."
            : cardData.effectText;
    }

    private void PopulateKeywords(CardData cardData)
    {
        if (keywordsContainer == null || keywordEntryPrefab == null)
            return;

        // Clear previous
        for (int i = keywordsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(keywordsContainer.GetChild(i).gameObject);
        }

        if (cardData.effect == null || cardData.effect.Length == 0)
            return;

        GameObject entry = Instantiate(keywordEntryPrefab, keywordsContainer);

        TMP_Text text = entry.GetComponentInChildren<TMP_Text>();
        text.text = "keyword";
    }

    private string FormatKeyword(string keyword)
    {
        // Placeholder – will later pull from a KeywordDatabase
        return $"<b>{keyword}</b>\nThis keyword effect description goes here.";
    }
}
