using System;
using System.Collections.Generic;
using System.Linq;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LeaderboardManager : MonoBehaviour
{
    [SerializeField] GameObject InformationPanel;
    [SerializeField] TextMeshProUGUI InformationTextDeck;
    [SerializeField] TextMeshProUGUI InformationTextRelics;


    [SerializeField] TextMeshProUGUI FirstName;
    [SerializeField] TextMeshProUGUI SecondName;
    [SerializeField] TextMeshProUGUI ThirdName;

    [SerializeField] TextMeshProUGUI FirstFloor;
    [SerializeField] TextMeshProUGUI SecondFloor;
    [SerializeField] TextMeshProUGUI ThirdFloor;

    [SerializeField] Image FirstImageMainTrait;
    [SerializeField] Image FirstImageSecondaryTrait;
    [SerializeField] Image SecondImageMainTrait;
    [SerializeField] Image SecondImageSecondaryTrait;
    [SerializeField] Image ThirdImageMainTrait;
    [SerializeField] Image ThirdImageSecondaryTrait;

    [Header("Path Of Power Content")]
    [SerializeField] private GameObject traitSpritePrefab;
    [SerializeField] private List<RelicDefinition> relicLibrary = new List<RelicDefinition>();

    private readonly List<LeaderboardPlayerData> topPlayers = new List<LeaderboardPlayerData>();
    private TraitsDisplay traitSpriteProvider;

    private class LeaderboardPlayerData
    {
        public string displayName;
        public int floor;
        public int step;
        public List<int> deck = new List<int>();
        public List<string> relics = new List<string>();
        public List<string> bestTraits = new List<string>();
    }

    void Start()
    {
        ClearLeaderboard();
        if (InformationPanel != null)
            InformationPanel.SetActive(false);

        EnsureTraitSpriteProvider();

        if (CardDatabase.Instance != null && !CardDatabase.Instance.IsReady)
        {
            CardDatabase.Instance.OnCardsLoaded += LoadLeaderboard;
            return;
        }

        LoadLeaderboard();
    }

    private void OnDestroy()
    {
        if (CardDatabase.Instance != null)
            CardDatabase.Instance.OnCardsLoaded -= LoadLeaderboard;
    }

    private void Update()
    {
        if (InformationPanel != null && Input.GetMouseButtonDown(0))
        {
            InformationPanel.SetActive(false);
        }
    }

    public void ToggleFirstInfo()
    {
        TogglePlayerInfo(0);
    }

    public void ToggleSecondInfo()
    {
        TogglePlayerInfo(1);
    }

    public void ToggleThirdInfo()
    {
        TogglePlayerInfo(2);
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("PathofPower");
    }

    private void LoadLeaderboard()
    {
        if (CardDatabase.Instance != null)
            CardDatabase.Instance.OnCardsLoaded -= LoadLeaderboard;

        FirebaseFirestore.DefaultInstance.Collection("users").GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                topPlayers.Clear();

                if (task.IsFaulted || task.Result == null)
                {
                    Debug.LogError("Failed to load Path Of Power leaderboard: " + task.Exception);
                    ClearLeaderboard();
                    return;
                }

                topPlayers.AddRange(task.Result.Documents
                    .Select(ParsePlayerData)
                    .Where(player => player != null && player.floor > 0)
                    .OrderByDescending(player => player.floor)
                    .ThenByDescending(player => player.step)
                    .Take(3));

                PopulateLeaderboard();
            });
    }

    private LeaderboardPlayerData ParsePlayerData(DocumentSnapshot doc)
    {
        int floor = ReadInt(doc, PathOfPowerSaveService.BestFloorField, 0);
        int step = ReadInt(doc, PathOfPowerSaveService.BestStepField, 0);
        if (floor <= 0)
            return null;

        return new LeaderboardPlayerData
        {
            displayName = ReadDisplayName(doc),
            floor = floor,
            step = step,
            deck = ReadIntList(doc, PathOfPowerSaveService.BestDeckField),
            relics = ReadStringList(doc, PathOfPowerSaveService.BestRelicsField),
            bestTraits = ReadStringList(doc, PathOfPowerSaveService.BestTraitsField)
        };
    }

    private void PopulateLeaderboard()
    {
        ClearLeaderboard();
        PopulateRank(0, FirstName, FirstFloor, FirstImageMainTrait, FirstImageSecondaryTrait);
        PopulateRank(1, SecondName, SecondFloor, SecondImageMainTrait, SecondImageSecondaryTrait);
        PopulateRank(2, ThirdName, ThirdFloor, ThirdImageMainTrait, ThirdImageSecondaryTrait);
    }

    private void PopulateRank(int index, TextMeshProUGUI nameText, TextMeshProUGUI floorText, Image mainTraitImage, Image secondaryTraitImage)
    {
        if (index >= topPlayers.Count)
            return;

        LeaderboardPlayerData player = topPlayers[index];
        if (nameText != null)
            nameText.text = player.displayName;
        if (floorText != null)
            floorText.text = $"{player.floor} - {player.step}";

        PopulateTraitImage(mainTraitImage, player.bestTraits, 0);
        PopulateTraitImage(secondaryTraitImage, player.bestTraits, 1);
    }

    private void PopulateTraitImage(Image targetImage, List<string> traitNames, int traitIndex)
    {
        if (targetImage == null)
            return;

        targetImage.enabled = false;
        if (traitNames == null || traitIndex >= traitNames.Count)
            return;

        if (!Enum.TryParse(traitNames[traitIndex], true, out CardData.Trait trait))
        {
            Debug.LogWarning($"Unknown leaderboard trait '{traitNames[traitIndex]}'.");
            return;
        }

        EnsureTraitSpriteProvider();
        if (traitSpriteProvider == null)
            return;

        Sprite traitSprite = traitSpriteProvider.GetSpriteForTrait(trait);
        if (traitSprite == null)
            return;

        targetImage.sprite = traitSprite;
        targetImage.enabled = true;
    }

    private void TogglePlayerInfo(int playerIndex)
    {
        if (InformationPanel == null)
            return;

        if (playerIndex < 0 || playerIndex >= topPlayers.Count)
        {
            SetInformationText("No deck data.", "No relic data.");
            InformationPanel.SetActive(!InformationPanel.activeSelf);
            return;
        }

        LeaderboardPlayerData player = topPlayers[playerIndex];
        SetInformationText(FormatCardList(player.deck), FormatRelicList(player.relics));
        InformationPanel.SetActive(!InformationPanel.activeSelf);
    }

    private void SetInformationText(string deckText, string relicText)
    {
        if (InformationTextDeck != null)
            InformationTextDeck.text = deckText;
        if (InformationTextRelics != null)
            InformationTextRelics.text = relicText;
    }

    private string FormatCardList(List<int> cardIds)
    {
        if (cardIds == null || cardIds.Count == 0)
            return "No deck data.";

        return string.Join(" - ", cardIds
            .GroupBy(id => id)
            .Select(group => FormatCountedName(group.Count(), ResolveCardName(group.Key))));
    }

    private string FormatRelicList(List<string> relicIds)
    {
        if (relicIds == null || relicIds.Count == 0)
            return "No relic data.";

        return string.Join(" - ", relicIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Select(group => FormatCountedName(group.Count(), ResolveRelicName(group.Key))));
    }

    private string FormatCountedName(int count, string name)
    {
        return count >= 2 ? $"{count} * {name}" : name;
    }

    private string ResolveCardName(int cardId)
    {
        if (CardDatabase.Instance != null && CardDatabase.Instance.Cards != null && CardDatabase.Instance.Cards.TryGetValue(cardId, out CardData card))
            return card.name;

        return $"Unknown Card {cardId}";
    }

    private string ResolveRelicName(string relicId)
    {
        RelicDefinition relic = relicLibrary.FirstOrDefault(definition => definition != null && definition.RelicId.Equals(relicId, StringComparison.OrdinalIgnoreCase));
        return relic != null ? relic.DisplayName : relicId;
    }

    private void ClearLeaderboard()
    {
        ClearRank(FirstName, FirstFloor, FirstImageMainTrait, FirstImageSecondaryTrait);
        ClearRank(SecondName, SecondFloor, SecondImageMainTrait, SecondImageSecondaryTrait);
        ClearRank(ThirdName, ThirdFloor, ThirdImageMainTrait, ThirdImageSecondaryTrait);
    }

    private void ClearRank(TextMeshProUGUI nameText, TextMeshProUGUI floorText, Image mainTraitImage, Image secondaryTraitImage)
    {
        if (nameText != null)
            nameText.text = "-";
        if (floorText != null)
            floorText.text = "-";
        if (mainTraitImage != null)
            mainTraitImage.enabled = false;
        if (secondaryTraitImage != null)
            secondaryTraitImage.enabled = false;
    }

    private void EnsureTraitSpriteProvider()
    {
        if (traitSpriteProvider != null)
            return;

        traitSpriteProvider = FindObjectOfType<TraitsDisplay>();
        if (traitSpriteProvider != null)
            return;

        if (traitSpritePrefab == null)
        {
            Debug.LogWarning("LeaderboardManager needs a TraitsDisplay prefab/reference to resolve trait sprites.");
            return;
        }

        GameObject providerObject = Instantiate(traitSpritePrefab, transform);
        providerObject.name = "Leaderboard Trait Sprite Provider";
        traitSpriteProvider = providerObject.GetComponentInChildren<TraitsDisplay>();
        providerObject.SetActive(false);
    }

    private string ReadDisplayName(DocumentSnapshot doc)
    {
        if (doc.ContainsField("playertag"))
            return doc.GetValue<string>("playertag");
        if (doc.ContainsField("playerTag"))
            return doc.GetValue<string>("playerTag");
        if (doc.ContainsField("displayName"))
            return doc.GetValue<string>("displayName");

        return doc.Id;
    }

    private int ReadInt(DocumentSnapshot doc, string field, int fallback)
    {
        return doc.ContainsField(field) ? Convert.ToInt32(doc.GetValue<object>(field)) : fallback;
    }

    private List<int> ReadIntList(DocumentSnapshot doc, string field)
    {
        if (!doc.ContainsField(field))
            return new List<int>();

        object raw = doc.GetValue<object>(field);
        if (raw is IEnumerable<object> objects)
            return objects.Select(Convert.ToInt32).ToList();
        if (raw is IEnumerable<int> ints)
            return ints.ToList();

        return new List<int>();
    }

    private List<string> ReadStringList(DocumentSnapshot doc, string field)
    {
        if (!doc.ContainsField(field))
            return new List<string>();

        object raw = doc.GetValue<object>(field);
        if (raw is IEnumerable<object> objects)
            return objects.Select(value => value?.ToString() ?? string.Empty).Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
        if (raw is IEnumerable<string> strings)
            return strings.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();

        return new List<string>();
    }
}
