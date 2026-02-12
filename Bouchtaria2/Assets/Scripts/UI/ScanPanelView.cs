using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScanPanelView : MonoBehaviour
{
    [Header("Animated Panel Root")]
    [SerializeField] private RectTransform panelRoot; // ASSIGN IN INSPECTOR

    [Header("UI Content")]
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image cardSpriteCompact;
    [SerializeField] private Transform keywordContainer;
    [SerializeField] private Transform relatedContainer;
    [SerializeField] private GameObject keywordPrefab;
    [SerializeField] private GameObject relatedPrefab;

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
        cardSpriteCompact.sprite = card.artSpriteCompact;

        nameText.text = card.name;
        effectText.text = card.effectText;
        if (SceneManager.GetActiveScene().name == "Combat")
        {
            CardInstance cardInst = cardView.GetComponent<CardInstance>();


            nameText.text = cardInst.Data.name;
            effectText.text = cardInst.CurrentEffectText;
        }

        foreach (Transform child in keywordContainer)
            Destroy(child.gameObject);
        //KeyWordCheck
        string[] keywordList = card.effect.Split(' ');
        UpdateTexts(keywordList);
        DisplayRelatedCards(card);
    }
    private void PopulateBoard(CardView cardView)
    {
        if (cardView == null)
            return;

        CardInstance card = cardView.GetComponent<CardInstance>();
        cardSpriteCompact.sprite = card.Data.artSpriteCompact;

        nameText.text = card.Data.name;
        effectText.text = card.CurrentEffectText;

        if (card.ProgressionCounter > 0 && card.ProgressionCap>0)
        {
            effectText.text += $"\nProgression : {card.ProgressionCounter}/{card.ProgressionCap}";
        }

        foreach (Transform child in keywordContainer)
            Destroy(child.gameObject);
        //KeyWordCheck
        string[] keywordList = card.CurrentEffect.Split(' ');

        UpdateTexts(keywordList);
        DisplayRelatedCards(card.Data);
    }
    void DisplayRelatedCards(CardData data)
    {
        foreach (int id in data.relatedCards)
        {
            Debug.Log("New related id " + id);
            GameObject entry = Instantiate(relatedPrefab, relatedContainer);
            CardData relatedData = CardDatabase.Instance.GetCardById(id);
            entry.GetComponentInChildren<TextMeshProUGUI>().text = relatedData.effectText; if(relatedData.effectText == "") { entry.GetComponentInChildren<TextMeshProUGUI>().text = "No effect."; }
            Color traitLeft = Color.white;
            if (relatedData.traits.Count > 0 && TryGetTraitColor(relatedData.traits[0], out Color color))
            { traitLeft = color; }
            Color traitRight = Color.white;
            if (relatedData.traits.Count > 1 && TryGetTraitColor(relatedData.traits[1], out Color color2))
            { traitRight = color2; }
            Transform CardPreview = entry.transform.GetChild(1);
            Transform Artwork = CardPreview.GetChild(0); Artwork.GetComponent<Image>().sprite = relatedData.artSpriteCompact;
            Transform LeftTrait = CardPreview.GetChild(1); LeftTrait.GetComponent<Image>().color = traitLeft;
            Transform RightTrait = CardPreview.GetChild(2); RightTrait.GetComponent<Image>().color = traitRight;
            Transform Mana = CardPreview.GetChild(3); Mana.GetComponentInChildren<TextMeshProUGUI>().text = relatedData.manaCost.ToString();
            Transform LeftTraitMana = Mana.GetChild(0); LeftTraitMana.GetComponent<Image>().color = traitLeft;
            Transform RightTraitMana = Mana.GetChild(1); RightTraitMana.GetComponent<Image>().color = traitRight;
            Transform Atk = CardPreview.GetChild(4); Atk.GetComponentInChildren<TextMeshProUGUI>().text = relatedData.atkValue.ToString(); Atk.GetComponent<Image>().color = traitLeft;
            Transform Hp = CardPreview.GetChild(5); Hp.GetComponentInChildren<TextMeshProUGUI>().text = relatedData.hpValue.ToString(); ; Hp.GetComponent<Image>().color = traitRight;
        }
    }
    private bool TryGetTraitColor(string traitString, out Color color)
    {
        color = Color.white;

        if (!System.Enum.TryParse<CardData.Trait>(traitString, true, out var trait))
            return false;

        color = TraitColorDatabase.Get(trait);
        return true;
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
            if (keyword.Contains("chaotic event"))
            {
                keyName.text = "Chaotic Event";
                keyDescription.text =
                    "A random effect effect chosen at random in a fixed pool.\nExample : Draw, Gain mana, Summon units ...";
            }
            else if (keyword.Contains("hunter*"))
            {
                keyName.text = "Monster Hunter";
                keyDescription.text =
                    "Equipping gear to hunters grant them +1/+1.";
            }
            else if (keyword.Contains("gear"))
            {
                keyName.text = "Gear";
                keyDescription.text =
                    "Equip this spell to a unit to grand them various buffs.";
            }
            else if (keyword.Contains("morphto"))
            {
                keyName.text = "Morph";
                keyDescription.text =
                    "Transforms into another unit and keeps damage.";
            }
            else if (keyword.Contains("thorns"))
            {
                keyName.text = "Thorns";
                keyDescription.text =
                    "When defending, deal damage to the attacker eaqual to the thorn value.";
            }
            else if (keyword.Contains("avatar"))
            {
                keyName.text = "Avatar";
                keyDescription.text =
                    "The avatar's stats increase for each praise this game.";
            }
            else if (keyword.Contains("praise"))
            {
                keyName.text = "Praise";
                keyDescription.text =
                    "The avatar gets stronger when played.";
            }
            else if (keyword.Contains("lifesteal"))
            {
                keyName.text = "Lifesteal";
                keyDescription.text =
                    "Heal the unit's core for the damage dealt during combat.";
            }
            else if (keyword.Contains("sleep"))
            {
                keyName.text = "Sleep";
                keyDescription.text =
                    "An asleep unit cannot attack, at the end of its turn it awakens.";
            }
            else if (keyword.Contains("monsterpart"))
            {
                keyName.text = "Monster Parts";
                keyDescription.text =
                    "Cannot be used, get other monster parts to assemble into gear.";
            }
            else if (keyword.Contains("blessed"))
            {
                keyName.text = "Blessed";
                keyDescription.text =
                    "Has a divine shield that absorbs one instance of damage.";
            }
            else if (keyword.Contains("hidden"))
            {
                keyName.text = "Hidden";
                keyDescription.text =
                    "Cannot be target by attacks until this unit attacks.";
            }
            else if (keyword.Contains("quickstrike"))
            {
                keyName.text = "QuickStrike";
                keyDescription.text =
                    "Can attack units only during the turn it is summoned.";
            }
            else if (keyword.Contains("charge"))
            {
                keyName.text = "Charge";
                keyDescription.text =
                    "Can attack any enemy during the turn it is summoned.";
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
                keyDescription.text = "Summons a unit without triggering its Deploy effect.";
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
