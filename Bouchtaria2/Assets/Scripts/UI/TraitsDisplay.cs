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
    [SerializeField] public Sprite gunnerIcon;
    [SerializeField] public Sprite FighterIcon;//
    [SerializeField] public Sprite faithIcon;//
    [SerializeField] public Sprite AvatarIcon;//
    [SerializeField] public Sprite memeIcon;//
    [SerializeField] public Sprite neutralIcon;//
    [SerializeField] public Sprite comboIcon;//
    [SerializeField] public Sprite chaosIcon;//
    [SerializeField] public Sprite spellFocusIcon;//
    [SerializeField] public Sprite speedsterIcon;
    [SerializeField] public Sprite soulForceIcon;
    [SerializeField] public Sprite cozyIcon;
    [SerializeField] public Sprite swordsmanIcon;

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
                display += $"Kill or catch enemies to activate : {Progression}/{CurrentCap}" +
                    "\nTier 1 : The next Pokemon you play evolves instantly.";
                if (tier > 1) display +=
                        "\nTier 2 : The next Pokemon you play evolves instantly and gains +3/+3.";
                if (tier > 2) display +=
                         "\nTier 3 : Discover a LEGENDARY Pokemon.";
                break;
            case CardData.Trait.Inazuma:
                display += $"Use Hissatsus to activate : {Progression}/{CurrentCap}" +
                    "\nTier 1 : Unlock tension gauge to use your hissatsu instead of mana.";
                if (tier > 1) display +=
                        "\nTier 2 : Unlock combined hissatsus and hissatsus gain power when chaining them.";
                if (tier > 2) display +=
                         "\nTier 3 : Every 5 hissatsus you cast gain 1 aura discovery.";
                break;
            case CardData.Trait.Chaos:
                display += $"Play cards with the 'random' Keyword to activate : {Progression}/{CurrentCap}" +
                    "\nTier 1 : Turn start: summon a random 2 cost unit (5 cost if you have at least 10 mana.";
                if (tier > 1) display +=
                        "\nTier 2 : Turn Start : Trigger a random chaotic event.";
                if (tier > 2) display +=
                         "\nTier 3 : End of turn : Get a Cheater's will.";
                break;
            case CardData.Trait.Neutral:
                display += $"Play neutral cards to unlock. Currently played : {Progression}/{CurrentCap}" +
                    "\nTier 1 : The first unit you play each turn has +2 HP.";
                if (tier > 1) display +=
                        "\nTier 2 : End of turn, discount the cost of a random card in your hand by 1.";
                if (tier > 2) display +=
                         "\nTier 3 : The first unit you play also has +2 ATK.\nStart of turn, discount a random card in your hand by 1.";
                break;
            case CardData.Trait.Fighter:
                display += $"Play fighter units to unlock. Currently played : {Progression}/{CurrentCap}" +
                    "\nTier 1 : The first unit you play each turn has +1/+1.";
                if (tier > 1) display +=
                        "\nTier 2 : Discover a Fighter Relic, reduce its cost by 3.";
                if (tier > 2) display +=
                         "\nTier 3 : End of Turn : Buff all your units by +1/+1.";
                break;
            case CardData.Trait.Healer:
                display += $"Heal ally units or core to unlock : {Progression}/{CurrentCap}" +
                    "\nTier 1 : You draw 1 card during your first heal of each turn.";
                if (tier > 1) display +=
                        "\nTier 2 : Give extra health to units and armor to your core equal to half your overhealings.";
                if (tier > 2) display +=
                         "\nTier 3 : Deal damage to a random enemy equal to half your heals.";
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
                    "\nTier 1 : From now on your summonned speedsters have +1 ATK.";
                if (tier > 1) display +=
                        "\nTier 2 : From now on your summonned speedsters also have quickstrike.";
                if (tier > 2) display +=
                         "\nTier 3 : From now on all your summonned units have quickstrike and +2 ATK.";
                break;
            case CardData.Trait.MonsterHunter:
                display += $"Colossal Monsters have died : {Progression}/{CurrentCap}" +
                    "\nTier 1 : Summon a random tier one monster.";
                if (tier > 1) display +=
                        "\nTier 2 : Summon a random tier two unique monster";
                if (tier > 2) display +=
                         "\nTier 3 : Summon a random nuclear monster.";
                break;
            case CardData.Trait.Avatar:
                display += $"Praise to unlock the following: {Progression}/{CurrentCap}" +
                    "\nTier 1 : Shuffle the two avatars into your deck.";
                if (tier > 1) display +=
                        "\nTier 2 : Shuffle two more Avatars; unlock their elements (in hand or deck).";
                if (tier > 2) display +=
                         "\nTier 3 : Your avatars enter AVATAR STATE (in hand or deck).";
                break;
            case CardData.Trait.Gunner:
                display += $"Deal non-combat damage multiple times with your cards: {Progression}/{CurrentCap}" +
                    "\nTier 1 : End of turn : Deal 1 damage to a random unit.";
                if (tier > 1) display +=
                        "\nTier 2 : Also deal 1 damage to the enemy core.";
                if (tier > 2) display +=
                         "\nTier 3 : Gun trait effect triggers thrice.";
                break;
            case CardData.Trait.Combo:
                display += $"Play 2/3/4 cards in the same turn during three different turns to unlock: {Progression}/{CurrentCap}" +
                    "\nTier 1 Effect : Your second card each turn costs 1 less.";
                if (tier > 1) display +=
                        "\nTier 2 Effect : Your third card each turn increases the cost of a card in the enemy hand by 1.";
                if (tier > 2) display +=
                         "\nTier 3 Effect : The first time you play 4 cards in a turn, draw 2.";
                break;
            case CardData.Trait.SoulForce:
                display += $"Killed enemies releses souls. Collect souls with Soul Eater units: {Progression}/{CurrentCap}" +
                    "\nTier 1 : The first soul collected each turn discounts a random card in your hand by 2.";
                if (tier > 1) display +=
                        "\nTier 2 : Whenever a unit consumes souls, it gains +1/+1 per soul consumed.";
                if (tier > 2) display +=
                         "\nTier 3 : End turn with 5+ souls to consume them and gain a random Evangelist Grace.";
                break;
            case CardData.Trait.Swordsman:
                display += $"Apply Bleed to enemies to unlock: {Progression}/{CurrentCap}" +
                    "\nTier 1 : The first Swordsman attack each turn applies Bleed.";
                if (tier > 1) display +=
                    "\nTier 2 : Bleeding enemies have -2 ATK.";
                if (tier > 2) display +=
                    "\nTier 3 : Attacking a Bleeding enemy with a Swordsman consumes the Bleed and deals double damage.";
                break;

            case CardData.Trait.Cozy:
                display += $"End turns with units choosing not to attack to unlock: {Progression}/{CurrentCap}" +
                    "\nTier 1 : Each unit that didn't attack gains +1 HP. Cozy units gain +2 HP instead.";
                if (tier > 1) display +=
                    "\nTier 2 : Every 2 Cozy units that didn't attack discount a random card in your hand by 1 and heal your core by 2.";
                if (tier > 2) display +=
                    "\nTier 3 : End of turn, units that didn't attack gain Blessed.";
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
