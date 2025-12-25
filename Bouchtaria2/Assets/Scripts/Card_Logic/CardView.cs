using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class CardView : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public CardData CardData { get; private set; }

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
    // Called by CollectionScreen after instantiation
    public void Init(CardData data)
    {
        CardData = data;
        Setup(data);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        bool owned = UserCollectionManager.Instance.IsOwned(cardId);
        if (ScanController.Instance == null ||!owned)
            return;

        ScanController.Instance.OnCardHoverEnter(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ScanController.Instance == null)
            return;

        ScanController.Instance.OnCardHoverExit(this);
    }

    /// <summary>
    /// Initial setup using static card data
    /// </summary>
    private void Setup(CardData card)
    {
        cardId = card.id;

        cardSpriteRenderer.sprite = card.artSprite;
        nameText.text = card.name;
        manaText.text = card.manaCost.ToString();
        atkText.text = card.atkValue.ToString();
        hpText.text = card.hpValue.ToString();

        ApplyTraitFrameColor(card);
        Refresh();
    }

    private void ApplyTraitFrameColor(CardData card)
    {
        frameRenderer.color = Color.white;
        frameRenderer2.color = Color.white;
        manaFrameRenderer1.color = Color.white;
        manaFrameRenderer2.color = Color.white;
        atkFrameRenderer.color = Color.white;
        hpFrameRenderer.color = Color.white;

        if (card.traits == null || card.traits.Count == 0)
            return;
        if (card.cardType.ToLower() == "spell")
        {
            atkFrameRenderer.gameObject.SetActive(false) ;
            hpFrameRenderer.gameObject.SetActive(false) ;

        }
        if (TraitColors.TryGetValue(card.traits[0].ToLowerInvariant(), out Color color))
        {
            frameRenderer.color = color;
            frameRenderer2.color = color;
            manaFrameRenderer1.color = color;
            manaFrameRenderer2.color = color;
            atkFrameRenderer.color = color;
            hpFrameRenderer.color = color;
        }

        if (card.traits.Count > 1 &&
            TraitColors.TryGetValue(card.traits[1].ToLowerInvariant(), out Color color2))
        {
            frameRenderer2.color = color2;
            manaFrameRenderer2.color = color2;
            hpFrameRenderer.color = color2;
        }
    }

    private static readonly Dictionary<string, Color> TraitColors =
        new Dictionary<string, Color>
        {
            { "speedster", new Color(0.4f, 0.7f, 1f) },
            { "tank",      new Color(0.6f, 0.9f, 0.6f) },
            { "mage",      new Color(0.8f, 0.6f, 1f) }
        };

    /// <summary>
    /// Refresh owned / locked visual state
    /// </summary>
    public void Refresh()
    {
        bool owned = UserCollectionManager.Instance.IsOwned(cardId);
        //Only in collection
        lockOverlay.SetActive(!owned);

        cardSpriteRenderer.color = owned
            ? Color.white
            : new Color(1f, 1f, 1f, 0.35f);
    }
}
