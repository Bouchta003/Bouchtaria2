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
        UpdateTexts(card.effectText);
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

        UpdateTexts(card.CurrentEffectText);
        DisplayRelatedCards(card.Data);
    }
    void DisplayRelatedCards(CardData data)
    {
        HashSet<int> desiredIds = new HashSet<int>(data.relatedCards);
        Dictionary<int, GameObject> existingEntries = new Dictionary<int, GameObject>();
        List<GameObject> entriesToRemove = new List<GameObject>();

        foreach (Transform child in relatedContainer)
        {
            RelatedCardEntry marker = child.GetComponent<RelatedCardEntry>();
            if (marker == null || !desiredIds.Contains(marker.CardId) || existingEntries.ContainsKey(marker.CardId))
            {
                entriesToRemove.Add(child.gameObject);
                continue;
            }

            existingEntries[marker.CardId] = child.gameObject;
        }

        foreach (GameObject staleEntry in entriesToRemove)
        {
            Destroy(staleEntry);
        }

        foreach (int id in data.relatedCards)
        {
            if (existingEntries.ContainsKey(id))
                continue;

            Debug.Log("New related id " + id);
            GameObject entry = Instantiate(relatedPrefab, relatedContainer);
            RelatedCardEntry marker = entry.GetComponent<RelatedCardEntry>();
            if (marker == null)
                marker = entry.AddComponent<RelatedCardEntry>();
            marker.CardId = id;

            CardData relatedData = CardDatabase.Instance.GetCardById(id);
            entry.GetComponentInChildren<TextMeshProUGUI>().text = relatedData.effectText;
            if (relatedData.effectText == "")
            {
                entry.GetComponentInChildren<TextMeshProUGUI>().text = "No effect.";
            }

            Color traitLeft = Color.white;
            if (relatedData.traits.Count > 0 && TryGetTraitColor(relatedData.traits[0], out Color color))
            {
                traitLeft = color;
            }

            Color traitRight = Color.white;
            if (relatedData.traits.Count > 1 && TryGetTraitColor(relatedData.traits[1], out Color color2))
            {
                traitRight = color2;
            }
            else traitRight = traitLeft;

            Transform CardPreview = entry.transform.GetChild(1);
            Transform Artwork = CardPreview.GetChild(0);
            Artwork.GetComponent<Image>().sprite = relatedData.artSpriteCompact;
            Transform LeftTrait = CardPreview.GetChild(1);
            LeftTrait.GetComponent<Image>().color = traitLeft;
            Transform RightTrait = CardPreview.GetChild(2);
            RightTrait.GetComponent<Image>().color = traitRight;
            Transform Mana = CardPreview.GetChild(3);
            Mana.GetComponentInChildren<TextMeshProUGUI>().text = relatedData.manaCost.ToString();
            Transform LeftTraitMana = Mana.GetChild(0);
            LeftTraitMana.GetComponent<Image>().color = traitLeft;
            Transform RightTraitMana = Mana.GetChild(1);
            RightTraitMana.GetComponent<Image>().color = traitRight;
            Transform Atk = CardPreview.GetChild(4);
            if (relatedData.cardType == "spell") Atk.gameObject.SetActive(false);
            Atk.GetComponentInChildren<TextMeshProUGUI>().text = relatedData.atkValue.ToString();
            Atk.GetComponent<Image>().color = traitLeft;
            Transform Hp = CardPreview.GetChild(5);
            if (relatedData.cardType == "spell") Hp.gameObject.SetActive(false);
            Hp.GetComponentInChildren<TextMeshProUGUI>().text = relatedData.hpValue.ToString();
            Hp.GetComponent<Image>().color = traitRight;
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
    private void UpdateTexts(string effectText)
    {
        if (effectText.ToLower().Contains(("protect").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "Protect";
            keyDescription.text =
                "Forces enemies to attack this unit.";
        }
        if (effectText.ToLower().Contains(("chaotic event").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "Chaotic Event";
            keyDescription.text =
                "A random effect effect chosen at random in a fixed pool.\nExample : Draw, Gain mana, Summon units ...";
        }
        if (effectText.ToLower().Contains(("hunter*").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "Monster Hunter";
            keyDescription.text =
                "Equipping gear to hunters grant them +1/+1.";
        }
        if (effectText.ToLower().Contains(("gear").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "Gear";
            keyDescription.text =
                "Equip this spell to a unit to grand them various buffs.";
        }
        if (effectText.ToLower().Contains(("morphto").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "Morph";
            keyDescription.text =
                "Transforms into another unit and keeps damage.";
        }
        if (effectText.ToLower().Contains(("thorns").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "Thorns";
            keyDescription.text =
                "When defending, deal damage to the attacker eaqual to the thorn value.";
        }
        if (effectText.ToLower().Contains(("avatar").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "Avatar";
            keyDescription.text =
                "The avatar's stats increase for each praise this game.";
        }
        if (effectText.ToLower().Contains(("praise").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "Praise";
            keyDescription.text =
                "The avatar gets stronger when played.";
        }
        if (effectText.ToLower().Contains(("lifesteal").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "Lifesteal";
            keyDescription.text =
                "Heal the unit's core for the damage dealt during combat.";
        }
        if (effectText.ToLower().Contains(("sleep").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "Sleep";
            keyDescription.text =
                "An asleep unit cannot attack, at the end of its turn it awakens.";
        }
        if (effectText.ToLower().Contains(("monsterpart").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "Monster Parts";
            keyDescription.text =
                "Cannot be used, get other monster parts to assemble into gear.";
        }
        if (effectText.ToLower().Contains(("blessed").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "Blessed";
            keyDescription.text =
                "Has a divine shield that absorbs one instance of damage.";
        }
        if (effectText.ToLower().Contains(("hidden").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "Hidden";
            keyDescription.text =
                "Cannot be target by attacks until this unit attacks.";
        }
        if (effectText.ToLower().Contains(("quickstrike").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "QuickStrike";
            keyDescription.text =
                "Can attack units only during the turn it is summoned.";
        }
          if (effectText.ToLower().Contains(("charge").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "Charge";
            keyDescription.text =
                "Can attack any enemy during the turn it is summoned.";
        }
          if (effectText.ToLower().Contains(("haste").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "Haste";
            keyDescription.text =
                "Can attack twice per turn (not stackable).";
        }
          if (effectText.ToLower().Contains(("summon").ToLower()))
        {
            GameObject entry = Instantiate(keywordPrefab, keywordContainer);
            var keyName = entry.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var keyDescription = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            keyName.text = "Summon";
            keyDescription.text = "Summons a unit without triggering its Deploy effect.";
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
