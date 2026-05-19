using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
    [SerializeField] public GameObject relicGridLayout;
    [SerializeField] public Image nextEnemyImage;
    [SerializeField] public Image enemyEliteIcon;
    [SerializeField] public Image enemyWarden;
    [SerializeField] public TextMeshProUGUI EnemyNameText;
    [SerializeField] public TextMeshProUGUI DiscoveryText;
    [SerializeField] public Button EventAcceptBtn;
    [SerializeField] public Button EventSkipButton;
    [SerializeField] private List<int> eventDialogues = new List<int>();

    private bool eventDialogueResolved;

    [Header("Starter Deck Discovery")]
    [SerializeField] private int starterDeckTargetSize = 20;
    [SerializeField] private CardData.Trait starterDeckTrait = CardData.Trait.Neutral;

    public PathOfPowerRunData CurrentRun { get; private set; } = new PathOfPowerRunData();

    public event Action<PathOfPowerRunData> OnRunLoaded;
    public event Action<IReadOnlyList<RelicDefinition>> OnStarterRelicChoicesGenerated;
    public event Action<IReadOnlyList<RelicDefinition>> OnWardenRelicRewardsGenerated;
    public event Action<IReadOnlyList<int>> OnCardDiscoveryGenerated;
    public event Action<IReadOnlyList<PathDefinition>> OnPathChoicesRequested;
    public event Action<PathOfPowerStepData> OnStepReady;

    private PathOfPowerFloorGenerator floorGenerator;
    private readonly List<GameObject> spawnedRelicDiscoveryObjects = new List<GameObject>();
    private readonly List<GameObject> spawnedRelicGridObjects = new List<GameObject>();

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
        EnsureCardInputManager();
        if (DiscoverDisplay != null)
            DiscoverDisplay.SetActive(false);

        LoadRun();
    }
    public void BackButton()
    {
        SceneManager.LoadScene("Main_Menu");
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
            activeEnemyRelics = new List<int>(),
            currentFloorSeed = GenerateSeed(),
            currentPathType = PathOfPowerPathType.Simple,
            currentStreak = 0,
            starterDeckTrait = this.starterDeckTrait,
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
        Debug.Log($"[PathOfPower] SwitchDisplay requested for index {displayIndex}.");

        if (differentDisplays == null || displayIndex < 0 || displayIndex >= differentDisplays.Count)
        {
            Debug.LogWarning($"[PathOfPower] Cannot switch display because index {displayIndex} is outside the configured display list.");
            return;
        }

        foreach(GameObject display in differentDisplays)
        {
            if (display != null)
                display.SetActive(false);
        }

        if (differentDisplays[displayIndex] != null)
            differentDisplays[displayIndex].SetActive(true);
        else
            Debug.LogWarning($"[PathOfPower] Display index {displayIndex} is configured but the GameObject reference is missing.");

        UpdateRelicGrid();
        //0 = Start, 1 = Step, 2 = Combat, 3 = Event, 4 = Path, 5 = DiscoveryDisplay...
    }
    public void LoadRun()
    {
        PathOfPowerSaveService.Load(runData =>
        {
            CurrentRun = runData ?? new PathOfPowerRunData();
            EnsureRunLists();
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
            ShowDisplayForCurrentPhase();
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
        UpdateRelicGrid();
        CurrentRun.pendingStarterRelicChoices.Clear();
        CurrentRun.currentDeck.Clear();
        CurrentRun.starterDeckTrait = this.starterDeckTrait;
        CurrentRun.phase = PathOfPowerRunPhase.StartingDeckDiscovery;
        GameRunContext.PathOfPowerData = CurrentRun;
        Debug.Log($"Relic chosen : {relicId}, {GetRelicDisplayName(selectedRelic, relicId)}\nNext step is {CurrentRun.phase}.");
        ClearRelicDiscovery();
        GenerateStartingCardDiscovery();
        SaveRun();
    }

    public void GenerateStartingCardDiscovery()
    {
        if (CurrentRun.currentDeck.Count >= starterDeckTargetSize)
        {
            CompleteStartingDeckDiscovery();
            return;
        }

        CurrentRun.pendingCardChoices = GenerateCardChoices(CurrentRun.currentFloorSeed, 3);
        ShowCardDiscovery(CurrentRun.pendingCardChoices);
        OnCardDiscoveryGenerated?.Invoke(CurrentRun.pendingCardChoices);
    }

    public void SelectStartingCard(int cardId)
    {
        if (CurrentRun.phase != PathOfPowerRunPhase.StartingDeckDiscovery)
            return;

        if (!CurrentRun.pendingCardChoices.Contains(cardId))
        {
            Debug.LogWarning($"Rejected starter card '{cardId}' because it was not one of the generated choices.");
            return;
        }

        if (CurrentRun.currentDeck.Count >= starterDeckTargetSize)
        {
            CompleteStartingDeckDiscovery();
            return;
        }

        CurrentRun.currentDeck.Add(cardId);
        CurrentRun.pendingCardChoices.Clear();
        ClearCardDiscovery();

        if (CurrentRun.currentDeck.Count >= starterDeckTargetSize)
            CompleteStartingDeckDiscovery();
        else
            GenerateStartingCardDiscovery();

        SaveRun();
    }

    private void CompleteStartingDeckDiscovery()
    {
        CurrentRun.pendingCardChoices.Clear();
        ClearCardDiscovery();
        GenerateFloor(PathOfPowerPathType.Simple);
        CurrentRun.phase = PathOfPowerRunPhase.Lobby;
        SwitchDisplay(1);
    }

    public void SkipStartingCardDiscovery()
    {
        if (CurrentRun.phase != PathOfPowerRunPhase.StartingDeckDiscovery)
            return;

        Debug.LogWarning("Starter deck discovery cannot be skipped; choose cards until the starter deck reaches its target size.");
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
    /// Calculates or repairs the next step, logs the selected event/encounter, switches to the matching display,
    /// and then sets up the event/combat state.
    /// </summary>
    public void NextStep()
    {
        Debug.Log($"[PathOfPower] NextStep requested. Floor={CurrentRun?.currentFloor}, Step={CurrentRun?.currentStep}, Phase={CurrentRun?.phase}, Path={CurrentRun?.currentPathType}.");

        if (CurrentRun == null)
        {
            Debug.LogError("[PathOfPower] NextStep aborted because CurrentRun is null.");
            return;
        }

        EnsureRunLists();
        EnsureFloorGenerator();
        EnsureCurrentFloorSteps();

        PathOfPowerStepData step = CurrentRun.CurrentStepData;
        if (step == null)
        {
            Debug.LogError($"[PathOfPower] NextStep could not find or generate step {CurrentRun.currentStep} for floor {CurrentRun.currentFloor}.");
            return;
        }

        Debug.Log($"[PathOfPower] NextStep resolved step {step.stepIndex}: Type={step.stepType}, EventId='{step.eventId}', EncounterId='{step.encounterId}'.");
        OnStepReady?.Invoke(step);

        if (step.stepType == PathOfPowerStepType.Event)
        {
            Debug.Log($"[PathOfPower] Step {step.stepIndex} is an event. Switching to Event display and waiting for event logic to call CompleteCurrentEvent. EventId='{step.eventId}'.");
            CurrentRun.activeEnemyRelics?.Clear();
            CurrentRun.phase = PathOfPowerRunPhase.Event;
            SwitchDisplay(3);
            GameRunContext.PathOfPowerData = CurrentRun;
            SaveRun();
            StartEventDialogue(step);
            return;
        }

        Debug.Log($"[PathOfPower] Step {step.stepIndex} is a combat encounter. Switching to Combat display before loading the Combat scene. EncounterId='{step.encounterId}'.");
        nextEnemyImage.sprite = ResolveEncounter(step.encounterId)?.DisplaySprite;
        enemyEliteIcon.gameObject.SetActive(ResolveEncounter(step.encounterId).Elite);
        enemyWarden.gameObject.SetActive(ResolveEncounter(step.encounterId).Warden);
        EnemyNameText.text = ResolveEncounter(step.encounterId).DisplayName;
        SwitchDisplay(2);
    }

    /// <summary>
    /// Backwards-compatible hook for any existing UI buttons still wired to EnterCurrentStep.
    /// </summary>
    public void EnterCurrentStep()
    {
        NextStep();
    }

    public void CompleteCurrentEvent()
    {
        PathOfPowerStepData step = CurrentRun.CurrentStepData;
        if (step == null || step.stepType != PathOfPowerStepType.Event)
            return;

        if (CombatDialogue.Instance != null)
            CombatDialogue.Instance.CloseDialogue();

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
        {
            CurrentRun.currentRelics.Add(relicId);
            UpdateRelicGrid();
        }

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
        EnsureFloorGenerator();
        CurrentRun.currentPathType = pathType;
        CurrentRun.currentStep = 1;
        CurrentRun.currentFloorSeed = CurrentRun.currentFloorSeed == 0 ? GenerateSeed() : CurrentRun.currentFloorSeed;
        CurrentRun.currentFloorSteps = floorGenerator.GenerateFloor(CurrentRun.currentFloor, pathType, CurrentRun.currentFloorSeed);
        Debug.Log($"[PathOfPower] Generated floor {CurrentRun.currentFloor} with path {pathType}, seed {CurrentRun.currentFloorSeed}, steps: {DescribeFloorSteps(CurrentRun.currentFloorSteps)}.");
    }

    private void EnsureFloorGenerator()
    {
        if (floorGenerator != null)
            return;

        Debug.Log("[PathOfPower] Floor generator was not initialized yet; creating it now from the configured event and encounter libraries.");
        floorGenerator = new PathOfPowerFloorGenerator(eventLibrary, encounterLibrary);
    }

    private void EnsureRunLists()
    {
        CurrentRun.currentDeck ??= new List<int>();
        CurrentRun.currentRelics ??= new List<string>();
        CurrentRun.currentFloorSteps ??= new List<PathOfPowerStepData>();
        CurrentRun.pendingStarterRelicChoices ??= new List<string>();
        CurrentRun.pendingWardenRelicRewards ??= new List<string>();
        CurrentRun.pendingCardChoices ??= new List<int>();
        CurrentRun.activeEnemyRelics ??= new List<int>();
    }

    private void EnsureCurrentFloorSteps()
    {
        if (CurrentRun.currentFloorSteps != null && CurrentRun.CurrentStepData != null)
            return;

        Debug.LogWarning($"[PathOfPower] Missing step data for floor {CurrentRun.currentFloor}, step {CurrentRun.currentStep}. Regenerating floor with path {CurrentRun.currentPathType}.");
        int desiredStep = Mathf.Clamp(CurrentRun.currentStep, 1, 5);
        GenerateFloor(CurrentRun.currentPathType);
        CurrentRun.currentStep = desiredStep;
    }

    public void LaunchCombat()
    {
        EnemyEncounterDefinition encounter = ResolveEncounter(CurrentRun.CurrentStepData.encounterId);
        IReadOnlyList<int> encounterRelics = GetRelicIdsForEncounter(encounter);
        CurrentRun.activeEnemyRelics = encounterRelics.ToList();
        Debug.Log($"[PathOfPower] LaunchCombat setup. Encounter='{CurrentRun.CurrentStepData.encounterId}', Name='{(encounter != null ? encounter.DisplayName : "Missing Encounter")}', EnemyRelics=[{string.Join(", ", CurrentRun.activeEnemyRelics)}].");

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
        {
            if (CombatDialogue.Instance != null)
                CombatDialogue.Instance.CloseDialogue();

            step.completed = true;
        }

        CurrentRun.combatActive = false;
        CurrentRun.activeEnemyRelics?.Clear();
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
        UpdateRelicGrid();
    }

    private void MoveToNextFloorAfterWardenReward()
    {
        CurrentRun.currentFloor++;
        CurrentRun.currentStep = 1;
        CurrentRun.currentFloorSeed = GenerateSeed();
        CurrentRun.currentFloorSteps.Clear();
        CurrentRun.activeEnemyRelics?.Clear();
        CurrentRun.phase = CurrentRun.currentFloor >= 2 ? PathOfPowerRunPhase.PathSelection : PathOfPowerRunPhase.Lobby;
    }


    private void ShowDisplayForCurrentPhase()
    {
        switch (CurrentRun.phase)
        {
            case PathOfPowerRunPhase.StarterRelicChoice:
            case PathOfPowerRunPhase.StartingDeckDiscovery:
            case PathOfPowerRunPhase.AwaitingWardenReward:
                SwitchDisplay(5);
                break;
            case PathOfPowerRunPhase.PathSelection:
                SwitchDisplay(4);
                break;
            case PathOfPowerRunPhase.Event:
                SwitchDisplay(3);
                StartEventDialogue(CurrentRun.CurrentStepData);
                break;
            case PathOfPowerRunPhase.Combat:
                SwitchDisplay(2);
                break;
            default:
                SwitchDisplay(1);
                break;
        }
    }

    private void StartEventDialogue(PathOfPowerStepData step)
    {
        eventDialogueResolved = false;
        SetEventButtonsInteractable(false);

        if (step == null || CombatDialogue.Instance == null)
        {
            SetEventButtonsInteractable(true);
            return;
        }

        int eventId = 0;
        int.TryParse(step.eventId, out eventId);
        int dialogueId = (eventId >= 0 && eventId < eventDialogues.Count) ? eventDialogues[eventId] : eventId;

        void OnDialogueEnded()
        {
            if (eventDialogueResolved)
                return;

            eventDialogueResolved = true;
            CombatDialogue.Instance.OnDialogueEnded -= OnDialogueEnded;
            SetEventButtonsInteractable(true);
        }

        CombatDialogue.Instance.OnDialogueEnded -= OnDialogueEnded;
        CombatDialogue.Instance.OnDialogueEnded += OnDialogueEnded;
        CombatDialogue.Instance.TriggerCutscene(dialogueId, false, true);
    }

    private void SetEventButtonsInteractable(bool value)
    {
        if (EventAcceptBtn != null)
            EventAcceptBtn.interactable = value;

        if (EventSkipButton != null)
            EventSkipButton.interactable = value;
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
                if (CurrentRun.currentDeck.Count >= starterDeckTargetSize)
                {
                    CompleteStartingDeckDiscovery();
                    SaveRun();
                    break;
                }

                if (CurrentRun.pendingCardChoices == null || CurrentRun.pendingCardChoices.Count == 0)
                {
                    GenerateStartingCardDiscovery();
                    SaveRun();
                    break;
                }

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

    private List<int> GenerateCardChoices(int seed, int count)
    {
        if (CardDatabase.Instance == null || CardDatabase.Instance.Cards == null || CardDatabase.Instance.Cards.Count == 0)
        {
            Debug.LogWarning("Cannot generate Path Of Power starter card choices because the card database is not ready.");
            return new List<int>();
        }

        Dictionary<int, int> deckCounts = CurrentRun.currentDeck
            .GroupBy(cardId => cardId)
            .ToDictionary(group => group.Key, group => group.Count());
        string requiredTrait = CurrentRun.starterDeckTrait.ToString();
        System.Random rng = new System.Random(seed + CurrentRun.currentDeck.Count + 17);

        return CardDatabase.Instance.Cards.Values
            .Where(card => IsEligibleStarterDeckCard(card, requiredTrait, deckCounts))
            .OrderBy(_ => rng.Next())
            .Take(count)
            .Select(card => card.id)
            .ToList();
    }

    private bool IsEligibleStarterDeckCard(CardData card, string requiredTrait, IReadOnlyDictionary<int, int> deckCounts)
    {
        if (card == null || !card.packable || card.token || card.signature)
            return false;

        if (deckCounts != null && deckCounts.TryGetValue(card.id, out int ownedCopies) && ownedCopies >= 2)
            return false;

        return card.traits != null && card.traits.Any(trait => trait.Equals(requiredTrait, StringComparison.OrdinalIgnoreCase));
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

    /// <summary>
    /// Commentary/helper hook for encounter setup: use this to fetch every relic id assigned to an encounter asset.
    /// The returned ids are copied into CurrentRun.activeEnemyRelics before combat so combat-only systems do not need
    /// a direct ScriptableObject reference after the Combat scene loads.
    /// </summary>
    public IReadOnlyList<int> GetRelicIdsForEncounter(EnemyEncounterDefinition encounter)
    {
        if (encounter == null)
        {
            Debug.LogWarning("[PathOfPower] GetRelicIdsForEncounter called with a missing encounter; returning no enemy relics.");
            return Array.Empty<int>();
        }

        IReadOnlyList<int> relicIds = encounter.RelicIds ?? Array.Empty<int>();
        Debug.Log($"[PathOfPower] Encounter '{encounter.EncounterId}' has relic ids: [{string.Join(", ", relicIds)}].");
        return relicIds;
    }

    private string DescribeFloorSteps(IEnumerable<PathOfPowerStepData> steps)
    {
        if (steps == null)
            return "no steps";

        return string.Join(" | ", steps.Select(step => step == null
            ? "null"
            : $"#{step.stepIndex}:{step.stepType}:event={step.eventId}:encounter={step.encounterId}"));
    }

    private void ShowRelicDiscovery(IReadOnlyList<string> relicIds)
    {
        if (DiscoverDisplay == null || relicPrefab == null || relicIds == null || relicIds.Count == 0)
            return;

        DiscoveryText.text= "Select a relic (hold 'space bar' to enter scan mode)";

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
            relicObject.transform.localScale = new Vector3(5, 5, 1);
            spawnedRelicDiscoveryObjects.Add(relicObject);
            relicObject.name = $"Relic Discovery - {relic.DisplayName}";
            PositionRelicForDiscovery(relicObject, positions[i]);
            PopulateRelicPrefab(relicObject, relic);
            ConfigureRelicScan(relicObject, relic);

            Button button = relicObject.GetComponentInChildren<Button>(true);
            if (button == null)
            {
                Debug.LogWarning($"Relic discovery prefab for '{relic.RelicId}' does not contain a Button component.");
                continue;
            }

            button.onClick.RemoveAllListeners();
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
            rectTransform.localScale = new Vector3(5, 5, 1);
            return;
        }

        relicObject.transform.localPosition = position;
        relicObject.transform.localRotation = Quaternion.identity;
        relicObject.transform.localScale = new Vector3(5,5,1);
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

        DiscoveryText.text = "Select a Card to add to your deck (hold 'space bar' to enter scan mode)";
        if (CardFactory.Instance == null)
        {
            GameObject factoryObj = new GameObject("CardFactory");
            factoryObj.AddComponent<CardFactory>();
        }

        EnsureCardInputManager();
        Transform parent = discoveryCardParent != null ? discoveryCardParent : DiscoverDisplay.transform;
        ClearCardDiscovery();

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


    private void ClearCardDiscovery()
    {
        if (DiscoverDisplay == null)
            return;

        Transform parent = discoveryCardParent != null ? discoveryCardParent : DiscoverDisplay.transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);

        DiscoverDisplay.SetActive(false);
    }

    private void UpdateRelicGrid()
    {
        if (relicGridLayout == null || relicPrefab == null)
            return;

        Transform parent = relicGridLayout.transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
        spawnedRelicGridObjects.Clear();

        if (CurrentRun == null || CurrentRun.currentRelics == null)
            return;

        foreach (string relicId in CurrentRun.currentRelics.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            RelicDefinition relic = ResolveRelic(relicId);
            if (relic == null)
                continue;

            GameObject relicObject = Instantiate(relicPrefab, parent);
            spawnedRelicGridObjects.Add(relicObject);
            relicObject.name = $"Owned Relic - {relic.DisplayName}";
            PopulateRelicPrefab(relicObject, relic);
            ConfigureRelicScan(relicObject, relic);

            RectTransform rectTransform = relicObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.localScale = Vector3.one;
            }

            Button button = relicObject.GetComponentInChildren<Button>(true);
            if (button != null)
                button.onClick.RemoveAllListeners();
        }
    }

    private void ConfigureRelicScan(GameObject relicObject, RelicDefinition relic)
    {
        if (relicObject == null || relic == null)
            return;

        RelicScanTarget scanTarget = relicObject.GetComponentInChildren<RelicScanTarget>(true);
        if (scanTarget == null)
            scanTarget = relicObject.AddComponent<RelicScanTarget>();

        scanTarget.Initialize(relic);
    }

    private void EnsureCardInputManager()
    {
        if (FindFirstObjectByType<CardInputManager>() != null)
            return;

        GameObject inputManagerObject = new GameObject("CardInputManager");
        inputManagerObject.AddComponent<CardInputManager>();
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
