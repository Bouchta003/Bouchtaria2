using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PathOfPowerManager : MonoBehaviour
{
    public static PathOfPowerManager Instance;

    [Header("Content Libraries")]
    [SerializeField] private List<RelicDefinition> relicLibrary = new List<RelicDefinition>();
    [SerializeField] private List<EventDefinition> eventLibrary = new List<EventDefinition>();
    [SerializeField] private List<PathDefinition> pathLibrary = new List<PathDefinition>();
    [SerializeField] private List<EnemyEncounterDefinition> encounterLibrary = new List<EnemyEncounterDefinition>();

    [Header("Discovery UI Hooks")]
    [SerializeField] public GameObject DiscoverDisplay;
    [SerializeField] private Transform discoveryCardParent;
    [SerializeField] private Vector3 discoveryScale = new Vector3(0.6f, 0.6f, 0.6f);

    [Header("Display UI")]
    [SerializeField] public List<GameObject> differentDisplays;
    [SerializeField] public GameObject relicPrefab;//Discovery Positions : 400,540,0 960,540,0 1520,540,0
    [SerializeField] public GameObject reliPrefabParentDiscovery;

    [Header("Starter Deck Defaults")]
    [SerializeField] private int starterDeckTargetSize = 20;
    [SerializeField] private List<int> fallbackStarterDeck = new List<int>
    {
        3, 3, 4, 4, 5, 5, 6, 6, 9, 9,
        30, 30, 32, 32, 34, 34, 36, 36, 37, 37
    };

    public PathOfPowerRunData CurrentRun { get; private set; } = new PathOfPowerRunData();

    public event Action<PathOfPowerRunData> OnRunLoaded;
    public event Action<IReadOnlyList<RelicDefinition>> OnStarterRelicChoicesGenerated;
    public event Action<IReadOnlyList<RelicDefinition>> OnWardenRelicRewardsGenerated;
    public event Action<IReadOnlyList<int>> OnCardDiscoveryGenerated;
    public event Action<IReadOnlyList<PathDefinition>> OnPathChoicesRequested;
    public event Action<PathOfPowerStepData> OnStepReady;

    private PathOfPowerFloorGenerator floorGenerator;
    private readonly List<GameObject> spawnedRelicDiscoveryObjects = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        floorGenerator = new PathOfPowerFloorGenerator(eventLibrary, encounterLibrary);
        if (DiscoverDisplay != null)
            DiscoverDisplay.SetActive(false);

        LoadRun();
    }

    /// <summary>
    /// UI hook: call this from the Path Of Power entry button to start a clean run.
    /// It generates starter relic choices first, then waits for SelectStarterRelic.
    /// </summary>
    public void StartNewRun()
    {
        CurrentRun = new PathOfPowerRunData
        {
            currentFloor = 1,
            currentStep = 1,
            currentDeck = new List<int>(),
            currentRelics = new List<string>(),
            currentFloorSeed = GenerateSeed(),
            currentPathType = PathOfPowerPathType.Simple,
            currentStreak = 0,
            phase = PathOfPowerRunPhase.StarterRelicChoice,
            combatActive = false
        };


        SwitchDisplay(5);//Start Relic Discovery.
        CurrentRun.pendingStarterRelicChoices = PickRelics(3, relic => relic.CanAppearAsStarter, CurrentRun.currentFloorSeed)
            .Select(relic => relic.RelicId)
            .ToList();

        ShowRelicDiscovery(CurrentRun.pendingStarterRelicChoices);
        SaveRun();
        OnStarterRelicChoicesGenerated?.Invoke(ResolveRelics(CurrentRun.pendingStarterRelicChoices));
    }
    public void SwitchDisplay(int displayIndex)
    {
        foreach(GameObject display in differentDisplays)
        {
            if (display != null)
                display.SetActive(false);
        }
        differentDisplays[displayIndex].SetActive(true);
        //0 = Start, 1 = Step, 2 = Combat, 3 = Event, 4 = Path, 5 = DiscoveryDisplay...
    }
    public void LoadRun()
    {
        PathOfPowerSaveService.Load(runData =>
        {
            CurrentRun = runData ?? new PathOfPowerRunData();
            if (CurrentRun.currentFloor <= 0 || CurrentRun.phase == PathOfPowerRunPhase.None)
            {
                Debug.Log("No Data found for Path Of Power run; starting a new run.");
                SwitchDisplay(0);//New Run
                OnRunLoaded?.Invoke(CurrentRun);
                return;
            }

            if (CurrentRun.combatActive)
            {
                //Restart run because concede left combat mid run.
                Debug.LogWarning("Detected unfinished Path Of Power combat. The run is being marked defeated to avoid progress abuse.");
                EndRunAsDefeated();
                SwitchDisplay(0);//StartRun
                return;
            }

            if (CurrentRun.currentPathType == PathOfPowerPathType.Challenge && CurrentRun.currentStep == 5)
            {
                Debug.Log("Challenge path, step 5, display discovery for free relic.");
                GrantChallengePreWardenRelic();
                SwitchDisplay(5);//DiscoveryDisplay
                SaveRun();
            }

            if (CurrentRun.phase == PathOfPowerRunPhase.AwaitingWardenReward && CurrentRun.pendingWardenRelicRewards.Count == 0)
            {
                Debug.Log("Warden defeated, pending relic rewards.");
                SwitchDisplay(5);//DiscoveryDisplay
                CurrentRun.pendingWardenRelicRewards = PickRelics(2, relic => relic.CanAppearAsWardenReward, CurrentRun.currentFloorSeed + CurrentRun.currentStreak)
                    .Select(relic => relic.RelicId)
                    .ToList();
                SaveRun();
            }

            GameRunContext.PathOfPowerData = CurrentRun;
            OnRunLoaded?.Invoke(CurrentRun);
            ResumeCurrentPhaseHooks();
        });
    }


    /// <summary>
    /// UI hook: display a relic in the existing scan panel without building a separate relic tooltip system.
    /// </summary>
    public void ShowRelicInScanPanel(RelicDefinition relicDefinition)
    {
        if (relicDefinition == null || ScanController.Instance == null || ScanController.Instance.panelInstance == null)
            return;

        ScanController.Instance.panelInstance.PopulateRelic(new Relic(relicDefinition));
        ScanController.Instance.panelInstance.Slide(true);
    }

    public void SelectStarterRelic(string relicId)
    {
        if (CurrentRun.phase != PathOfPowerRunPhase.StarterRelicChoice)
            return;

        if (!CurrentRun.pendingStarterRelicChoices.Contains(relicId))
        {
            Debug.LogWarning($"Rejected starter relic '{relicId}' because it was not one of the generated choices.");
            return;
        }

        RelicDefinition selectedRelic = ResolveRelic(relicId);
        CurrentRun.currentRelics.Add(relicId);
        CurrentRun.pendingStarterRelicChoices.Clear();
        CurrentRun.currentDeck = BuildFallbackStarterDeck();
        CurrentRun.phase = PathOfPowerRunPhase.StartingDeckDiscovery;
        GameRunContext.PathOfPowerData = CurrentRun;
        Debug.Log($"Relic chosen : {relicId}, {GetRelicDisplayName(selectedRelic, relicId)}\nNext step is {CurrentRun.phase}.");
        ClearRelicDiscovery();
        GenerateStartingCardDiscovery();
        SaveRun();
    }

    public void GenerateStartingCardDiscovery()
    {
        CurrentRun.pendingCardChoices = GenerateCardChoices(CurrentRun.currentFloorSeed, 3);
        ShowCardDiscovery(CurrentRun.pendingCardChoices);
        OnCardDiscoveryGenerated?.Invoke(CurrentRun.pendingCardChoices);
    }

    public void SelectStartingCard(int cardId)
    {
        if (CurrentRun.phase != PathOfPowerRunPhase.StartingDeckDiscovery)
            return;

        if (CurrentRun.pendingCardChoices.Contains(cardId) && CurrentRun.currentDeck.Count < starterDeckTargetSize + 1)
            CurrentRun.currentDeck.Add(cardId);

        CurrentRun.pendingCardChoices.Clear();
        GenerateFloor(PathOfPowerPathType.Simple);
        CurrentRun.phase = PathOfPowerRunPhase.Lobby;
        SaveRun();
    }

    public void SkipStartingCardDiscovery()
    {
        if (CurrentRun.phase != PathOfPowerRunPhase.StartingDeckDiscovery)
            return;

        CurrentRun.pendingCardChoices.Clear();
        GenerateFloor(PathOfPowerPathType.Simple);
        CurrentRun.phase = PathOfPowerRunPhase.Lobby;
        SaveRun();
    }

    /// <summary>
    /// UI hook: call before floor 2+ to expose path buttons. Floor 1 uses Simple path by default.
    /// </summary>
    public void RequestPathSelection()
    {
        if (CurrentRun.currentFloor < 2)
        {
            GenerateFloor(PathOfPowerPathType.Simple);
            return;
        }

        CurrentRun.phase = PathOfPowerRunPhase.PathSelection;
        SaveRun();
        OnPathChoicesRequested?.Invoke(pathLibrary);
    }

    public void SelectPath(PathOfPowerPathType pathType)
    {
        GenerateFloor(pathType);
        CurrentRun.phase = PathOfPowerRunPhase.Lobby;
        SaveRun();
    }

    /// <summary>
    /// UI hook: call this from the lobby's "Next" button.
    /// Fights go to Combat, events notify OnStepReady for future event UI.
    /// </summary>
    public void EnterCurrentStep()
    {
        PathOfPowerStepData step = CurrentRun.CurrentStepData;
        if (step == null)
        {
            Debug.LogWarning("Path Of Power current step is missing; regenerating floor.");
            GenerateFloor(CurrentRun.currentPathType);
            step = CurrentRun.CurrentStepData;
        }

        OnStepReady?.Invoke(step);

        if (step.stepType == PathOfPowerStepType.Event)
        {
            CurrentRun.phase = PathOfPowerRunPhase.Event;
            SaveRun();
            return;
        }

        LaunchCombat(step);
    }

    public void CompleteCurrentEvent()
    {
        PathOfPowerStepData step = CurrentRun.CurrentStepData;
        if (step == null || step.stepType != PathOfPowerStepType.Event)
            return;

        step.completed = true;
        AdvanceToNextStepOrFloor();
        SaveRun();
    }

    public void ChooseWardenRelicReward(string relicId)
    {
        if (CurrentRun.phase != PathOfPowerRunPhase.AwaitingWardenReward)
            return;

        RelicDefinition selectedRelic = ResolveRelic(relicId);
        if (!string.IsNullOrWhiteSpace(relicId) && CurrentRun.pendingWardenRelicRewards.Contains(relicId))
            CurrentRun.currentRelics.Add(relicId);

        CurrentRun.pendingWardenRelicRewards.Clear();
        MoveToNextFloorAfterWardenReward();

        GameRunContext.PathOfPowerData = CurrentRun;

        if (!string.IsNullOrWhiteSpace(relicId))
            Debug.Log($"Relic chosen : {relicId}, {GetRelicDisplayName(selectedRelic, relicId)}\nNext step is {CurrentRun.phase}.");

        ClearRelicDiscovery();
        SaveRun();
    }

    public void SkipWardenRelicReward()
    {
        ChooseWardenRelicReward(string.Empty);
    }

    public void EndRunAsDefeated()
    {
        CurrentRun.phase = PathOfPowerRunPhase.Defeated;
        CurrentRun.combatActive = false;
        PathOfPowerSaveService.Save(CurrentRun);
    }

    private void GenerateFloor(PathOfPowerPathType pathType)
    {
        CurrentRun.currentPathType = pathType;
        CurrentRun.currentStep = 1;
        CurrentRun.currentFloorSeed = CurrentRun.currentFloorSeed == 0 ? GenerateSeed() : CurrentRun.currentFloorSeed;
        CurrentRun.currentFloorSteps = floorGenerator.GenerateFloor(CurrentRun.currentFloor, pathType, CurrentRun.currentFloorSeed);
    }

    private void LaunchCombat(PathOfPowerStepData step)
    {
        EnemyEncounterDefinition encounter = ResolveEncounter(step.encounterId);
        DeckSelectionCache.SelectedPlayerDeck = new List<int>(CurrentRun.currentDeck);
        DeckSelectionCache.SelectedEnemyDeck = PathOfPowerEnemyDeckBuilder.BuildEnemyDeck(CurrentRun, encounter);

        CurrentRun.phase = PathOfPowerRunPhase.Combat;
        CurrentRun.combatActive = true;
        GameRunContext.IsDungeonRun = false;
        GameRunContext.IsAdventureCombat = false;
        GameRunContext.IsAdventureHardMode = false;
        GameRunContext.AdventureFightId = 0;
        GameRunContext.IsPathOfPowerRun = true;
        GameRunContext.PathOfPowerData = CurrentRun;

        SaveRun(() => SceneManager.LoadScene("Combat"));
    }

    public void HandleCombatVictoryFromSave()
    {
        PathOfPowerStepData step = CurrentRun.CurrentStepData;
        if (step != null)
            step.completed = true;

        CurrentRun.combatActive = false;
        CurrentRun.currentStreak++;

        if (step != null && step.stepType == PathOfPowerStepType.Warden)
        {
            CurrentRun.pendingWardenRelicRewards = PickRelics(2, relic => relic.CanAppearAsWardenReward, CurrentRun.currentFloorSeed + CurrentRun.currentStreak)
                .Select(relic => relic.RelicId)
                .ToList();
            CurrentRun.phase = PathOfPowerRunPhase.AwaitingWardenReward;
            SaveRun();
            OnWardenRelicRewardsGenerated?.Invoke(ResolveRelics(CurrentRun.pendingWardenRelicRewards));
            return;
        }

        AdvanceToNextStepOrFloor();
        SaveRun();
    }

    private void AdvanceToNextStepOrFloor()
    {
        if (CurrentRun.currentPathType == PathOfPowerPathType.Challenge && CurrentRun.currentStep == 4)
            GrantChallengePreWardenRelic();

        if (CurrentRun.currentStep < 5)
        {
            CurrentRun.currentStep++;
            CurrentRun.phase = PathOfPowerRunPhase.Lobby;
            return;
        }

        CurrentRun.phase = PathOfPowerRunPhase.AwaitingWardenReward;
    }

    private void GrantChallengePreWardenRelic()
    {
        RelicDefinition relic = PickRelics(1, candidate => candidate.CanAppearAsWardenReward && !CurrentRun.currentRelics.Contains(candidate.RelicId), CurrentRun.currentFloorSeed + 404).FirstOrDefault();
        if (relic == null)
            return;

        // First foundation: challenge-path pre-warden relic is granted automatically.
        // TODO(Path Of Power): expose this through a dedicated reward/preview panel.
        CurrentRun.currentRelics.Add(relic.RelicId);
    }

    private void MoveToNextFloorAfterWardenReward()
    {
        CurrentRun.currentFloor++;
        CurrentRun.currentStep = 1;
        CurrentRun.currentFloorSeed = GenerateSeed();
        CurrentRun.currentFloorSteps.Clear();
        CurrentRun.phase = CurrentRun.currentFloor >= 2 ? PathOfPowerRunPhase.PathSelection : PathOfPowerRunPhase.Lobby;
    }

    private void ResumeCurrentPhaseHooks()
    {
        switch (CurrentRun.phase)
        {
            case PathOfPowerRunPhase.StarterRelicChoice:
                ShowRelicDiscovery(CurrentRun.pendingStarterRelicChoices);
                OnStarterRelicChoicesGenerated?.Invoke(ResolveRelics(CurrentRun.pendingStarterRelicChoices));
                break;
            case PathOfPowerRunPhase.StartingDeckDiscovery:
                ShowCardDiscovery(CurrentRun.pendingCardChoices);
                OnCardDiscoveryGenerated?.Invoke(CurrentRun.pendingCardChoices);
                break;
            case PathOfPowerRunPhase.PathSelection:
                OnPathChoicesRequested?.Invoke(pathLibrary);
                break;
            case PathOfPowerRunPhase.AwaitingWardenReward:
                ShowRelicDiscovery(CurrentRun.pendingWardenRelicRewards);
                OnWardenRelicRewardsGenerated?.Invoke(ResolveRelics(CurrentRun.pendingWardenRelicRewards));
                break;
        }
    }

    private List<int> BuildFallbackStarterDeck()
    {
        List<int> deck = new List<int>(fallbackStarterDeck);
        while (deck.Count > starterDeckTargetSize)
            deck.RemoveAt(deck.Count - 1);

        return deck;
    }

    private List<int> GenerateCardChoices(int seed, int count)
    {
        if (CardDatabase.Instance == null || CardDatabase.Instance.Cards == null || CardDatabase.Instance.Cards.Count == 0)
            return fallbackStarterDeck.Take(count).ToList();

        System.Random rng = new System.Random(seed + CurrentRun.currentDeck.Count + 17);
        return CardDatabase.Instance.Cards.Values
            .Where(card => card != null && card.packable && !card.token && !card.signature)
            .OrderBy(_ => rng.Next())
            .Take(count)
            .Select(card => card.id)
            .ToList();
    }

    private List<RelicDefinition> PickRelics(int count, Func<RelicDefinition, bool> predicate, int seed)
    {
        System.Random rng = new System.Random(seed == 0 ? GenerateSeed() : seed);
        return relicLibrary
            .Where(relic => relic != null && predicate(relic))
            .OrderBy(_ => rng.Next())
            .Take(count)
            .ToList();
    }

    private IReadOnlyList<RelicDefinition> ResolveRelics(IEnumerable<string> relicIds)
    {
        HashSet<string> ids = new HashSet<string>(relicIds ?? Enumerable.Empty<string>());
        return relicLibrary.Where(relic => relic != null && ids.Contains(relic.RelicId)).ToList();
    }

    private RelicDefinition ResolveRelic(string relicId)
    {
        return relicLibrary.FirstOrDefault(relic => relic != null && relic.RelicId == relicId);
    }

    private string GetRelicDisplayName(RelicDefinition relic, string fallbackId)
    {
        if (relic != null)
            return relic.DisplayName;

        return string.IsNullOrWhiteSpace(fallbackId) ? "Unknown Relic" : fallbackId;
    }

    private EnemyEncounterDefinition ResolveEncounter(string encounterId)
    {
        return encounterLibrary.FirstOrDefault(encounter => encounter != null && encounter.EncounterId == encounterId);
    }

    private void ShowRelicDiscovery(IReadOnlyList<string> relicIds)
    {
        if (DiscoverDisplay == null || relicPrefab == null || relicIds == null || relicIds.Count == 0)
            return;

        Transform parent = reliPrefabParentDiscovery != null ? reliPrefabParentDiscovery.transform : DiscoverDisplay.transform;
        ClearRelicDiscovery();

        DiscoverDisplay.SetActive(true);
        Vector3[] positions =
        {
            new Vector3(400f, 540f, 0f),
            new Vector3(960f, 540f, 0f),
            new Vector3(1520f, 540f, 0f)
        };

        for (int i = 0; i < relicIds.Count && i < positions.Length; i++)
        {
            RelicDefinition relic = ResolveRelic(relicIds[i]);
            if (relic == null)
                continue;

            GameObject relicObject = Instantiate(relicPrefab, parent);
            spawnedRelicDiscoveryObjects.Add(relicObject);
            relicObject.name = $"Relic Discovery - {relic.DisplayName}";
            PositionRelicForDiscovery(relicObject, positions[i]);
            PopulateRelicPrefab(relicObject, relic);

            Button button = relicObject.GetComponentInChildren<Button>(true);
            if (button == null)
            {
                Debug.LogWarning($"Relic discovery prefab for '{relic.RelicId}' does not contain a Button component.");
                continue;
            }

            string selectedRelicId = relic.RelicId;
            button.onClick.AddListener(() => ValidateRelicChoice(selectedRelicId));
        }
    }

    private void ClearRelicDiscovery()
    {
        if (reliPrefabParentDiscovery != null)
        {
            Transform parent = reliPrefabParentDiscovery.transform;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
        else
        {
            foreach (GameObject relicObject in spawnedRelicDiscoveryObjects)
            {
                if (relicObject != null)
                    Destroy(relicObject);
            }
        }

        spawnedRelicDiscoveryObjects.Clear();
    }

    private void PositionRelicForDiscovery(GameObject relicObject, Vector3 position)
    {
        RectTransform rectTransform = relicObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition3D = position;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            return;
        }

        relicObject.transform.localPosition = position;
        relicObject.transform.localRotation = Quaternion.identity;
        relicObject.transform.localScale = Vector3.one;
    }

    private void PopulateRelicPrefab(GameObject relicObject, RelicDefinition relic)
    {
        Image relicImage = relicObject.GetComponentInChildren<Image>(true);
        if (relicImage != null && relic.Icon != null)
            relicImage.sprite = relic.Icon;
    }

    private void ValidateRelicChoice(string relicId)
    {
        switch (CurrentRun.phase)
        {
            case PathOfPowerRunPhase.StarterRelicChoice:
                SelectStarterRelic(relicId);
                break;
            case PathOfPowerRunPhase.AwaitingWardenReward:
                ChooseWardenRelicReward(relicId);
                break;
            default:
                Debug.LogWarning($"Cannot choose relic '{relicId}' while Path Of Power is in phase {CurrentRun.phase}.");
                break;
        }
    }

    private void ShowCardDiscovery(List<int> cardIds)
    {
        if (DiscoverDisplay == null || cardIds == null || cardIds.Count == 0)
            return;

        if (CardFactory.Instance == null)
        {
            GameObject factoryObj = new GameObject("CardFactory");
            factoryObj.AddComponent<CardFactory>();
        }

        Transform parent = discoveryCardParent != null ? discoveryCardParent : DiscoverDisplay.transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);

        DiscoverDisplay.SetActive(true);
        Vector3[] positions = { new Vector3(-5, 0, 0), Vector3.zero, new Vector3(5, 0, 0) };

        for (int i = 0; i < cardIds.Count && i < positions.Length; i++)
        {
            CardData data = CardDatabase.Instance.GetCardById(cardIds[i]);
            if (data == null)
                continue;

            CardInstance instance = CardFactory.Instance.CreateCardInPosition(data, PlayerOwner.Player, positions[i], discoveryScale, parent);
            instance.IsDisplay = true;
            SortingGroup sortingGroup = instance.GetComponent<SortingGroup>();
            if (sortingGroup != null)
                sortingGroup.sortingOrder = 201;
        }
    }

    private void SaveRun(Action onComplete = null)
    {
        PathOfPowerSaveService.Save(CurrentRun, onComplete);
    }

    private int GenerateSeed()
    {
        return UnityEngine.Random.Range(100000, int.MaxValue);
    }
}
