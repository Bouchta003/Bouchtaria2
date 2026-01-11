using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TraitsDisplay : MonoBehaviour
{
    [Header("Traits Icons")]
    [SerializeField] public Sprite pokemonIcon;//
    [SerializeField] public Sprite inazumaIcon;//
    [SerializeField] public Sprite monsterhunterIcon;//
    [SerializeField] public Sprite healIcon;//
    [SerializeField] public Sprite blizzardIcon;//
    [SerializeField] public Sprite gunnerIcon;
    [SerializeField] public Sprite FighterIcon;//
    [SerializeField] public Sprite faithIcon;//
    [SerializeField] public Sprite ritualIcon;//
    [SerializeField] public Sprite memeIcon;//
    [SerializeField] public Sprite neutralIcon;//
    [SerializeField] public Sprite comboIcon;//
    [SerializeField] public Sprite haterIcon;//
    [SerializeField] public Sprite spellFocusIcon;//
    [SerializeField] public Sprite speedsterIcon;

    [Header("Prefab components")]
    [SerializeField] public Image iconSlot;
    [SerializeField] public Image gemSlot;
    [SerializeField] public Image frameRaritySlot;
    [SerializeField] public GameObject traitEffect;
    [SerializeField] private TMP_Text progressPopup;
    [SerializeField] private float popupDuration = 0.6f;
    [SerializeField] private float popupScale = 1.2f;

    private Coroutine progressRoutine;

    [Header("Rarity Sprites")]
    [SerializeField] public Sprite bronzeTraitSprite;
    [SerializeField] public Sprite silverTraitSprite;
    [SerializeField] public Sprite goldenTraitSprite;
    //[SerializeField] public Sprite trait4Icon;
    public CardData.Trait thisTrait;
    public int tier;
    public int Progression;
    public int CurrentCap;
    private void Start()
    {
        frameRaritySlot.gameObject.SetActive(false);
        if (transform.parent.tag == "Enemy")
            traitEffect.transform.localPosition *= -1;
        traitEffect.SetActive(false);
    }
    public void Activate(int tier)
    {
        frameRaritySlot.gameObject.SetActive(true);
        switch (tier)
        {
            case 1:
                frameRaritySlot.sprite = bronzeTraitSprite;
                break;
            case 2:
                frameRaritySlot.sprite = silverTraitSprite;
                break;
            case 3:
                frameRaritySlot.sprite = goldenTraitSprite;
                break;
            default:frameRaritySlot.gameObject.SetActive(false);break;
                }
    }
    public void DisplayTraitProgression()
    {
        traitEffect.SetActive(!traitEffect.activeSelf);
        string display = thisTrait.ToString()+" :\n";
        switch (thisTrait)
        {
            case CardData.Trait.Pokemon:
                display += $"Kill enemies to activate : {Progression}/{CurrentCap}" +
                    "\nTier 1 : The next Pokemon you play evolves instantly.";
                if (tier > 1) display +=
                        "\nTier 2 : The next Pokemon you play evolves instantly and gains +2/+2.";
                if (tier > 2) display +=
                         "\nTier 3 : Discover a LEGENDARY Pokemon.";
                break;
            case CardData.Trait.Neutral:
                display += $"Play neutral cards to unlock. Currently played : {Progression}/{CurrentCap}" +
                    "\nTier 1 : The first card you play each turn has +2 HP.";
                if (tier > 1) display +=
                        "\nTier 2 : The first card you draw that costs 3 mana or more each turn costs 1 less.";
                if (tier > 2) display +=
                         "\nTier 3 :NOT IMPLEMENTED .";
                break;
            case CardData.Trait.Healer:
                display += $"Heal ally units or core to unlock : {Progression}/{CurrentCap}" +
                    "\nTier 1 : Your heals heal 2 more HP for the rest of the game.";
                if (tier > 1) display +=
                        "\nTier 2 : Your heals heal 3 more HP for the rest of the game.";
                if (tier > 2) display +=
                         "\nTier 3 : Your heals deal damage when targetting enemies this game.";
                break;
            case CardData.Trait.Faith:
                display += $"Discover Cards to unlock : {Progression}/{CurrentCap}" +
                    "\nTier 1 : Add a Duaa and Hijab card to your hand.";
                if (tier > 1) display +=
                        "\nTier 2 : Your discovered cards cost 1 less.";
                if (tier > 2) display +=
                         "\nTier 3 : When you discover a card, refresh 1 mana.";
                break;
            case CardData.Trait.Speedster:
                display += $"Attack with allies to unlock : {Progression}/{CurrentCap}" +
                    "\nTier 1 : Your speedsters has +1 ATK.";
                if (tier > 1) display +=
                        "\nTier 2 : Your speedsters also have quickstrike.";
                if (tier > 2) display +=
                         "\nTier 3 : All your units have quickstrike and +1 ATK.";
                break;
            case CardData.Trait.MonsterHunter:
                display += $"Colossal Monsters have died : {Progression}/{CurrentCap}" +
                    "\nTier 1 : Summon a random tier one monster.";
                if (tier > 1) display +=
                        "\nTier 2 : Summon a random tier two unique monster";
                if (tier > 2) display +=
                         "\nTier 3 : Summon a random nuclear monster.";
                break;
            default:
                display += "Need to define this trait's tier logic";
                break;
        }
        traitEffect.GetComponentInChildren<TextMeshProUGUI>().text=display;
    }
    public void ShowProgress(int current, int cap)
    {
        if (cap >= 900) return;
        if (progressRoutine != null)
            StopCoroutine(progressRoutine);

        progressRoutine = StartCoroutine(ProgressPopupRoutine(current, cap));
    }
    private IEnumerator ProgressPopupRoutine(int current, int cap)
    {
        progressPopup.gameObject.SetActive(true);
        progressPopup.text = $"{current} / {cap}";

        RectTransform rt = progressPopup.rectTransform;
        CanvasGroup cg = progressPopup.GetComponent<CanvasGroup>();

        if (cg == null)
            cg = progressPopup.gameObject.AddComponent<CanvasGroup>();

        rt.localScale = Vector3.one * 0.9f;
        cg.alpha = 0f;

        // Fade + pop in
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float k = t / 0.15f;

            rt.localScale = Vector3.Lerp(Vector3.one * 0.9f, Vector3.one * popupScale, k);
            cg.alpha = k;
            yield return null;
        }

        rt.localScale = Vector3.one;

        // Hold
        yield return new WaitForSeconds(popupDuration);

        // Fade out
        t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            cg.alpha = 1f - (t / 0.2f);
            yield return null;
        }

        progressPopup.gameObject.SetActive(false);
    }

}
