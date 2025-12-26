using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class CardView : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public CardData CardData { get; private set; }
    CardInstance inst;

    [Header("Hand Mode")]
    [SerializeField] private GameObject handVisual;
    [SerializeField] private SpriteRenderer cardSpriteRenderer;
    [SerializeField] private SpriteRenderer frameRenderer;
    [SerializeField] private SpriteRenderer frameRenderer2;
    [SerializeField] private SpriteRenderer manaFrameRenderer1;
    [SerializeField] private SpriteRenderer manaFrameRenderer2;
    [SerializeField] private SpriteRenderer atkFrameRenderer;
    [SerializeField] private SpriteRenderer hpFrameRenderer;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] public TMP_Text nameText;
    [SerializeField] public TMP_Text manaText;
    [SerializeField] public TMP_Text atkText;
    [SerializeField] public TMP_Text hpText;

    [Header("Board Mode")]
    [SerializeField] private GameObject boardVisual;
    [SerializeField] private SpriteRenderer cardSpriteRendererBoard;
    [SerializeField] private SpriteRenderer frameRendererBoard;
    [SerializeField] private SpriteRenderer frameRenderer2Board;
    [SerializeField] private SpriteRenderer manaFrameRenderer1Board;
    [SerializeField] private SpriteRenderer manaFrameRenderer2Board;
    [SerializeField] private SpriteRenderer atkFrameRendererBoard;
    [SerializeField] private SpriteRenderer hpFrameRendererBoard;
    [SerializeField] public TMP_Text nameTextBoard;
    [SerializeField] public TMP_Text manaTextBoard;
    [SerializeField] public TMP_Text atkTextBoard;
    [SerializeField] public TMP_Text hpTextBoard;

    private int cardId;
    // Called by CollectionScreen after instantiation
    public void Init(CardData data)
    {
        CardData = data;
        SetupHandMode(data);
        inst = gameObject.GetComponentInChildren<CardInstance>();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        bool owned = UserCollectionManager.Instance.IsOwned(cardId);
        if (ScanController.Instance == null || (!owned && SceneManager.GetActiveScene().name == "Collection"))
            return;

        ScanController.Instance.OnCardHoverEnter(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ScanController.Instance == null)
            return;

        ScanController.Instance.OnCardHoverExit(this);
    }

    public void UpdateMode()
    {
        CardInstance thisInstance = gameObject.GetComponent<CardInstance>();

        switch (gameObject.GetComponent<CardInstance>().CurrentZone)
        {
            case CardZone.Hand:
                SetupHandMode(thisInstance.Data); break;
            case CardZone.Board:
                SetupBoardMode(thisInstance.Data); break;
        }
    }
    private void SetupHandMode(CardData card)
    {
        handVisual.SetActive(true); boardVisual.SetActive(false);
        CardInstance thisInstance = gameObject.GetComponent<CardInstance>();
        cardId = card.id;

        cardSpriteRenderer.sprite = card.artSprite;
        nameText.text = card.name;
        manaText.text = thisInstance.CurrentManaCost.ToString();
        atkText.text = thisInstance.CurrentAttack.ToString();
        hpText.text = thisInstance.CurrentHealth.ToString();

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
            atkFrameRenderer.gameObject.SetActive(false);
            hpFrameRenderer.gameObject.SetActive(false);

        }
        if (TryGetTraitColor(card.traits[0], out Color color))
        {
            frameRenderer.color = color;
            frameRenderer2.color = color;
            manaFrameRenderer1.color = color;
            manaFrameRenderer2.color = color;
            atkFrameRenderer.color = color;
            hpFrameRenderer.color = color;
        }
        if (card.traits.Count > 1 &&
   TryGetTraitColor(card.traits[1], out Color color2))
        {
            frameRenderer2.color = color2;
            manaFrameRenderer2.color = color2;
            hpFrameRenderer.color = color2;
        }

        Refresh();
    }
    private void SetupBoardMode(CardData card)
    {
        handVisual.SetActive(false); boardVisual.SetActive(true);
        CardInstance thisInstance = gameObject.GetComponent<CardInstance>();
        cardId = card.id;

        cardSpriteRendererBoard.sprite = card.artSpriteCompact;
        nameTextBoard.text = card.name;
        manaTextBoard.text = thisInstance.CurrentManaCost.ToString();
        atkTextBoard.text = thisInstance.CurrentAttack.ToString();
        hpTextBoard.text = thisInstance.CurrentHealth.ToString();

        frameRendererBoard.color = Color.white;
        frameRenderer2Board.color = Color.white;
        manaFrameRenderer1Board.color = Color.white;
        manaFrameRenderer2Board.color = Color.white;
        atkFrameRendererBoard.color = Color.white;
        hpFrameRendererBoard.color = Color.white;

        if (card.traits == null || card.traits.Count == 0)
            return;
        if (card.cardType.ToLower() == "spell")
        {
            atkFrameRendererBoard.gameObject.SetActive(false);
            hpFrameRendererBoard.gameObject.SetActive(false);

        }
        if (TryGetTraitColor(card.traits[0], out Color color))
        {
            frameRendererBoard.color = color;
            frameRenderer2Board.color = color;
            manaFrameRenderer1Board.color = color;
            manaFrameRenderer2Board.color = color;
            atkFrameRendererBoard.color = color;
            hpFrameRendererBoard.color = color;
        }
        if (card.traits.Count > 1 &&
  TryGetTraitColor(card.traits[1], out Color color2))
        {
            frameRenderer2Board.color = color2;
            manaFrameRenderer2Board.color = color2;
            hpFrameRendererBoard.color = color2;
        }
        Refresh();
    }
    private bool TryGetTraitColor(string traitString, out Color color)
    {
        color = Color.white;

        if (!System.Enum.TryParse<CardData.Trait>(traitString, true, out var trait))
            return false;

        color = TraitColorDatabase.Get(trait);
        return true;
    }

    /// <summary>
    /// Refresh owned / locked visual state
    /// </summary>
    public void Refresh()
    {
        if (SceneManager.GetActiveScene().name != "Collection") return;
        bool owned = UserCollectionManager.Instance.IsOwned(cardId);
        //Only in collection
        lockOverlay.SetActive(!owned);

        cardSpriteRenderer.color = owned
            ? Color.white
            : new Color(1f, 1f, 1f, 0.35f);
    }
}
