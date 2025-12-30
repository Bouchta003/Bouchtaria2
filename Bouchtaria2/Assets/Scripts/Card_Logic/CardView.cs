using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;

public class CardView : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public CardData CardData { get; private set; }
    CardInstance inst;
    public Vector3 BoardPosition { get; set; }
    [Header("SFX")]
    [SerializeField] private AudioSource punchSFX;

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
    [SerializeField] private SpriteRenderer glowRenderer;

    private Coroutine glowCoroutine;
    private CardGlowState currentGlowState = CardGlowState.None;

    [Header("EffectDisplay")]
    [SerializeField] GameObject protectSprite;
    [SerializeField] GameObject quickStrikeSprite;
    [SerializeField] GameObject evolveSprite;
    public enum CardGlowState
    {
        None,
        CanAttack,
        CanBeTargeted
    }

    #region AttackAnimation
    private IEnumerator MoveOverTime(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;
            transform.position = Vector3.Lerp(from, to, lerp);
            yield return null;
        }
        transform.position = to;
    }
    public IEnumerator PlayAttackAnimation(Transform target)
    {
        transform.DOKill();

        Vector3 startPos = BoardPosition; // IMPORTANT
        Vector3 targetPos = target.position;

        Vector3 dir = (targetPos - startPos).normalized;

        float windupDistance = 0.15f;
        float attackReach = Vector3.Distance(startPos, targetPos) * 0.6f;

        Vector3 windupPos = startPos - dir * windupDistance;
        Vector3 hitPos = startPos + dir * attackReach;

        // Timings

        float windupTime = 0.2f;
        float dashTime = 0.06f;
        float returnTime = 0.12f;

        // Wind-up
        yield return MoveOverTime(startPos, windupPos, windupTime);

        // Dash (impact)
        yield return MoveOverTime(windupPos, hitPos, dashTime);
        //Play SFX punch

        // Return
        yield return MoveOverTime(hitPos, BoardPosition, returnTime);
    }
    public IEnumerator PlayHitReaction(int damage)
    {
        transform.DOKill(); // stop other tweens just in case

        Vector3 originalPos = transform.position;
        Vector3 originalScale = transform.localScale;

        float duration = 0.08f;
        float strength = 0.08f;

        // Small shake
        transform.DOShakePosition(
            duration,
            strength,
            vibrato: 12,
            randomness: 90,
            fadeOut: true
        );

        // Scale punch (impact feel)
        transform.DOPunchScale(
            Vector3.one * 0.12f,
            duration,
            vibrato: 8,
            elasticity: 0.8f
        );

        GameManager gm = FindFirstObjectByType<GameManager>();
        gm.ShakeCameraForDamage(damage);

        yield return new WaitForSeconds(duration);

        transform.position = originalPos;
        transform.localScale = originalScale;
    }

    #endregion
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
        manaFrameRenderer1Board.gameObject.SetActive(true);
        manaFrameRenderer2Board.gameObject.SetActive(true);
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
        if (card.traits.Count > 1 && TryGetTraitColor(card.traits[1], out Color color2))
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
        manaTextBoard.gameObject.SetActive(false);
        atkTextBoard.text = thisInstance.CurrentAttack.ToString();
        hpTextBoard.text = thisInstance.CurrentHealth.ToString();

        frameRendererBoard.color = Color.white;
        frameRenderer2Board.color = Color.white;
        manaFrameRenderer1Board.gameObject.SetActive(false);
        manaFrameRenderer2Board.gameObject.SetActive(false);
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
        if (card.traits.Count > 1 && TryGetTraitColor(card.traits[1], out Color color2))
        {
            frameRenderer2Board.color = color2;
            manaFrameRenderer2Board.color = color2;
            hpFrameRendererBoard.color = color2;
        }

        //Keyword Display
        protectSprite.SetActive(false);
        quickStrikeSprite.SetActive(false);
        evolveSprite.SetActive(false);

        if (inst.CurrentEffect.ToLower().Contains("protect"))
            protectSprite.SetActive(true);
        if (inst.CurrentEffect.ToLower().Contains("quickstrike"))
            quickStrikeSprite.SetActive(true); 
        if (inst.CurrentEffect.ToLower().Contains("morphto"))
            evolveSprite.SetActive(true);

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
    #region Glow
    public void SetGlow(CardGlowState state)
    {
        if (currentGlowState == state)
            return;

        currentGlowState = state;

        if (glowCoroutine != null)
            StopCoroutine(glowCoroutine);

        if (state == CardGlowState.None)
        {
            glowCoroutine = StartCoroutine(FadeOutGlow());
        }
        else
        {
            glowCoroutine = StartCoroutine(GlowPulseRoutine(state));
        }
    }
    private IEnumerator GlowPulseRoutine(CardGlowState state)
    {
        Color baseColor = state switch
        {
            CardGlowState.CanAttack => new Color(0.2f, 1f, 0.2f, 1f),
            CardGlowState.CanBeTargeted => new Color(1f, 0.25f, 0.25f, 1f),
            _ => Color.clear
        };

        float pulseSpeed = 3f;
        float minAlpha = 0.3f;
        float maxAlpha = 0.8f;
        float baseScale = 1.05f;
        float pulseScale = 1.1f;

        glowRenderer.gameObject.SetActive(true);

        // Smooth fade-in first
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime * 8f;
            glowRenderer.color = new Color(
                baseColor.r,
                baseColor.g,
                baseColor.b,
                Mathf.Lerp(0f, maxAlpha, t)
            );
            yield return null;
        }

        // Continuous pulse
        while (currentGlowState == state)
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);
            float scale = Mathf.Lerp(baseScale, pulseScale, pulse);

            glowRenderer.color = new Color(
                baseColor.r,
                baseColor.g,
                baseColor.b,
                alpha
            );

            glowRenderer.transform.localScale = new Vector3(1.22f,0.82f,1) * scale;

            yield return null;
        }
    }
    private IEnumerator FadeOutGlow()
    {
        Color start = glowRenderer.color;
        float t = 0f;

        while (t < 0.15f)
        {
            t += Time.deltaTime * 8f;
            glowRenderer.color = new Color(
                start.r,
                start.g,
                start.b,
                Mathf.Lerp(start.a, 0f, t)
            );
            yield return null;
        }

        glowRenderer.color = Color.clear;
        glowRenderer.gameObject.SetActive(false);
    }
    #endregion
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

