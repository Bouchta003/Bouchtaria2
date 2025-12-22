using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CardView : MonoBehaviour
{
    [Header("Sprite References")]
    [SerializeField] private SpriteRenderer cardSpriteRenderer;
    [SerializeField] private SpriteRenderer frameRenderer;
    [SerializeField] private SpriteRenderer frameRenderer2;
    [SerializeField] private SpriteRenderer manaFrameRenderer1;
    [SerializeField] private SpriteRenderer manaFrameRenderer2;
    [SerializeField] private SpriteRenderer atkFrameRenderer;
    [SerializeField] private SpriteRenderer hpFrameRenderer;
    [SerializeField] private GameObject lockOverlay;

    [Header("Text References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text manaText;
    [SerializeField] private TMP_Text atkText;
    [SerializeField] private TMP_Text hpText;

    private int cardId;

    /// <summary>
    /// Initial setup using static card data
    /// </summary>
    public void Setup(CardData card)
    {
        cardId = card.id;

        cardSpriteRenderer.sprite = card.artSprite;

        ApplyTraitFrameColor(card);

        nameText.text = card.name;
        manaText.text = card.manaCost.ToString();
        atkText.text = card.atkValue.ToString();
        hpText.text = card.hpValue.ToString();

        Refresh();
    }

    private void ApplyTraitFrameColor(CardData card)
    {
        // Default color
        frameRenderer.color = Color.white;
        frameRenderer2.color = Color.white;
        manaFrameRenderer1.color = Color.white;
        manaFrameRenderer2.color = Color.white;
        atkFrameRenderer.color = Color.white;
        hpFrameRenderer.color = Color.white;

        if (card.traits == null || card.traits.Count == 0)
            return;

        string trait1 = card.traits[0].ToLowerInvariant();

        if (TraitColors.TryGetValue(trait1, out Color color))
        {
            frameRenderer.color = color;
            frameRenderer2.color = color;
            manaFrameRenderer1.color = color;
            manaFrameRenderer2.color = color;
            atkFrameRenderer.color = color;
            hpFrameRenderer.color = color;
        }
        if (card.traits.Count > 1)
        {
            string trait2 = card.traits[1].ToLowerInvariant();
            if (TraitColors.TryGetValue(trait2, out Color color2))
            {
                frameRenderer2.color = color2;
                manaFrameRenderer2.color = color2;
                hpFrameRenderer.color = color2;
            }
        }
    }

    private static readonly Dictionary<string, Color> TraitColors =
    new Dictionary<string, Color>
    {
        { "speedster", new Color(0.4f, 0.7f, 1f) },   // blue
        { "tank",      new Color(0.6f, 0.9f, 0.6f) }, // green
        { "mage",      new Color(0.8f, 0.6f, 1f) }    // purple
        // Add more traits here
    };

    /// <summary>
    /// Refreshes owned / locked visual state
    /// </summary>
    public void Refresh()
    {
        bool owned = UserCollectionManager.Instance.IsOwned(cardId);

        // Lock overlay (can be another sprite or child object)
        lockOverlay.SetActive(!owned);

        // Optional: visually dim locked cards
        cardSpriteRenderer.color = owned
            ? Color.white
            : new Color(1f, 1f, 1f, 0.35f);
    }
}
