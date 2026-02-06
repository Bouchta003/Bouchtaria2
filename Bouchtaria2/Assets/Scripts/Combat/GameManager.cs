using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;
using System.Linq;
using UnityEngine.Rendering;
using System;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

public interface IAttackable
{
    Transform Transform{ get; } 
    PlayerOwner Owner { get; }
    int CurrentAttack { get; }
    string CurrentEffect { get; set; }
    void TakeDamage(int amount);
    void Heal(int amount);
}
public enum GameState
{
    Playing,
    PlayerWon,
    PlayerLost
}

public class GameManager : MonoBehaviour
{
    [Header("Core")]
    public CoreInstance PlayerCore;
    public CoreInstance EnemyCore;
    [SerializeField] private GameObject corePrefab;
    [SerializeField] private int startingCoreHealth = 30;
    [SerializeField] private GameObject spawnPlayerCore;
    [SerializeField] private GameObject spawnEnemyCore;
    public GameState CurrentGameState { get; private set; } = GameState.Playing;

    [Header("Deck and Board")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] public AllyCardDropArea allyDropArea;
    [SerializeField] public EnemyCardDropArea enemyDropArea;
    [SerializeField] public HandManager allyHand;
    [SerializeField] public HandManager enemyHand;

    private Transform playerCoreProxy;
    private Transform enemyCoreProxy;

    [Header("Mana")]
    [SerializeField] private int baseManaCap = 10;
    public int AllyCurrentMana { get; private set; }
    public int AllyCurrentMaxMana { get; private set; }
    public int EnemyCurrentMana { get; private set; }
    public int EnemyCurrentMaxMana { get; private set; }
    public int AllyBonusManaCap { get; private set; }
    public int EnemyBonusManaCap { get; private set; }

    [Header("UI")]
    [SerializeField] TextMeshProUGUI manacounterAlly;
    [SerializeField] GameObject boardDesign;
    [SerializeField] Sprite defaultBoard;
    [SerializeField] Sprite defaultMagicBoard;
    [SerializeField] Sprite distortionBoard;
    [SerializeField] TextMeshProUGUI manacounterEnmy;
    [SerializeField] Canvas MainCanvas;
    [SerializeField] public Canvas PauseCanvas;

    [Header("Cursor")]
    [SerializeField] Image attackCursor;
    
    [Header("EnemySpellReveal")]
    [SerializeField] GameObject enemySpellReveal;
    [SerializeField] GameObject enemySpellAnchor;

    [Header("Trait Systems")]
    [SerializeField] private TraitSystem allyTraitSystem;
    [SerializeField] private TraitSystem enemyTraitSystem;
    [SerializeField] private TraitUIManager allyTraitUI;
    [SerializeField] private TraitUIManager enemyTraitUI;
    [SerializeField] private WinLoseUI winLoseUI;
    private readonly Queue<Action> deferredActions = new();

    [Header("Camera Shake")]
    [SerializeField] private Camera mainCamera;

    //Graveyard 
    public Graveyard PlayerGraveyard { get; private set; } = new();
    public Graveyard EnemyGraveyard { get; private set; } = new();


    public int PlayerHealBonus = 0;
    public int EnemyHealBonus = 0;
    public bool PlayerDarkHeal = false;
    public bool EnemyDarkHeal = false;
    public bool DistortionWorld = false;
    //Animation
    public bool IsCombatAnimating { get; private set; }
    //Trait logic
    private readonly List<ITraitProgression> activeProgressions = new();
    //Attack logic
    private readonly Queue<AttackRequest> attackQueue = new Queue<AttackRequest>();
    private bool isResolvingAttack = false;
    public bool isTargettingAttack;
    Card currentAttacker;

    //Trait Actions
    public event System.Action<CardInstance> OnCardKilled;
    public event System.Action<CardInstance> OnCardAttack;
    public event System.Action<CardInstance> OnCardKiller;
    public event System.Action<PlayerOwner, int> OnOwnerHeal;
    public event System.Action<PlayerOwner, int> OnOwnerDamage;
    public event System.Action<PlayerOwner> OnDiscover;
    public event System.Action<PlayerOwner> OnPraise;
    public event System.Action<PlayerOwner> OnDamageCard;

    //Camera shake
    private Vector3 cameraBasePos;
    private Tween cameraShakeTween;
    //Target effects
    private bool isTargetingEffect;
    private Func<IAttackable, bool> onEffectTargetChosen;
    private CardInstance effectSource;
    private EffectTarget targetType = EffectTarget.None;
    PlayerOwner effectOwner;
    [Header("Discovery")]
    [SerializeField] public GameObject discoverDisplay;
    public bool isDiscovering;
    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        cameraBasePos = mainCamera.transform.position;
    }
    void Start()
    {
        isTargettingAttack = false;

        if (TurnManager.Instance == null)
        {
            Debug.LogError("TurnManager missing!");
            return;
        }

        //Setup cores mana and deck before the turn logic
        Sprite[] defaultBoards = new Sprite[] { defaultBoard, defaultMagicBoard };
        boardDesign.GetComponentInChildren<SpriteRenderer>().sprite = defaultBoards[UnityEngine.Random.Range(0, defaultBoards.Length-1)];

        deckManager.InitializeDecks();        // build decks
        deckManager.DetectUnlockableTraits(); // analyze decks
        SetupTraits();                        // create progressions
        InitializeMana();

        SetupCores();
        PlayerCore.GetComponent<CoreView>().Bind(PlayerCore);
        EnemyCore.GetComponent<CoreView>().Bind(EnemyCore);

        playerCoreProxy = PlayerCore.AttackProxy;
        enemyCoreProxy = EnemyCore.AttackProxy;
        //Start turn logic
        TurnManager.Instance.OnTurnStarted += HandleTurnStart;
        TurnManager.Instance.StartFirstTurn();
        foreach (var progression in activeProgressions)
        {
            progression.ResetProgression();
            progression.PushInitialState();
        }
    }
    public void TogglePause()
    {
        MainCanvas.gameObject.SetActive(PauseCanvas.gameObject.activeSelf);
        PauseCanvas.gameObject.SetActive(!PauseCanvas.gameObject.activeSelf);
    }
    public void MainMenu()
    {
        GameFlowController.Instance.GoToMainMenu();
    }
    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted -= HandleTurnStart;
    }
    // Update is called once per frame
    void Update()
    {
        manacounterAlly.text = $"{AllyCurrentMana}/{AllyCurrentMaxMana}";
        manacounterEnmy.text = $"{EnemyCurrentMana}/{EnemyCurrentMaxMana}";
        attackCursor.transform.position = Input.mousePosition;
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (DistortionWorld && boardDesign.GetComponentInChildren<SpriteRenderer>().sprite != distortionBoard)
        {
            boardDesign.GetComponentInChildren<SpriteRenderer>().sprite = distortionBoard;
        }
        if (!DistortionWorld && boardDesign.GetComponentInChildren<SpriteRenderer>().sprite == distortionBoard)
        {
            boardDesign.GetComponentInChildren<SpriteRenderer>().sprite = defaultMagicBoard;
        }
    }
    #region Turn Logic
    private void HandleTurnStart(PlayerOwner owner)
    {
        IncreaseMaxMana(owner);
        RefillMana(owner);
    }
    private void EndGame()
    {
        isTargettingAttack = false;
        attackCursor.gameObject.SetActive(false);

        if (TurnManager.Instance != null)
            TurnManager.Instance.enabled = false;

        if (winLoseUI != null)
        {
            winLoseUI.gameObject.SetActive(true);
            if (CurrentGameState == GameState.PlayerWon)
                winLoseUI.ShowWin();
            else if (CurrentGameState == GameState.PlayerLost)
                winLoseUI.ShowLose();
        }

        Debug.Log($"Game ended with state: {CurrentGameState}");
    }
    #endregion

    #region Trait Management
    private void SetupTraits()
    {
        allyTraitSystem.Initialize(PlayerOwner.Player);
        enemyTraitSystem.Initialize(PlayerOwner.Enemy);
        allyTraitUI.DetectTraitBorder();enemyTraitUI.DetectTraitBorder();
        allyTraitSystem.OnTraitTierActivated += OnAllyTraitActivated;
        enemyTraitSystem.OnTraitTierActivated += OnEnemyTraitActivated;

        SetupPlayerTraits(PlayerOwner.Player, deckManager.AllyTraitsUnlockable, allyTraitSystem);
        SetupPlayerTraits(PlayerOwner.Enemy, deckManager.EnemyTraitsUnlockable, enemyTraitSystem);
    }
    private void SetupPlayerTraits(PlayerOwner owner, Dictionary<CardData.Trait, int> unlockables, TraitSystem traitSystem)
    {
        if (unlockables == null)
        {
            Debug.LogError($"Unlockables dictionary is NULL for {owner}");
            return;
        }
        foreach (var pair in unlockables)
        {
            CardData.Trait trait = pair.Key;
            int maxTier = pair.Value;

            ITraitProgression progression = trait switch
            {
                CardData.Trait.Neutral => new NeutralProgression(owner, maxTier, traitSystem, allyDropArea, enemyDropArea),
                CardData.Trait.Pokemon => new PokemonProgression(owner, maxTier, traitSystem, allyDropArea, enemyDropArea, this),
                CardData.Trait.MonsterHunter => new MonsterHunterProgression(owner, maxTier, traitSystem, allyDropArea, enemyDropArea, this),
                CardData.Trait.Gunner => new GunnerProgression(owner, maxTier, traitSystem, this),
                CardData.Trait.Inazuma => throw new System.NotImplementedException(),
                CardData.Trait.Speedster => new SpeedsterProgression(owner, maxTier, traitSystem, this),
                CardData.Trait.Blizzard => throw new System.NotImplementedException(),
                CardData.Trait.Fighter => throw new System.NotImplementedException(),
                CardData.Trait.Faith => new FaithProgression(owner, maxTier, traitSystem, this),
                CardData.Trait.Avatar => new AvatarProgression(owner, maxTier, traitSystem, this),
                CardData.Trait.Hater => throw new System.NotImplementedException(),
                CardData.Trait.SpellFocus => throw new System.NotImplementedException(),
                CardData.Trait.Combo => throw new System.NotImplementedException(),
                CardData.Trait.Healer => new HealerProgression(owner, maxTier, traitSystem, this),
                _ => throw new System.NotImplementedException(),
            };

            //CardData.Trait.Gunner => new GunnerProgression( owner, maxTier,traitSystem, allyDropArea, enemyDropArea), _ => null};

            if (progression != null)
            {
                progression.Register(); progression.OnProgressUpdated += HandleTraitProgressUpdated;

                activeProgressions.Add(progression);
            }
        }
    }
    private void HandleTraitProgressUpdated(    CardData.Trait trait,    int progress, int currentCap,    PlayerOwner owner)
    {
        TraitUIManager ui =
            owner == PlayerOwner.Player
                ? allyTraitUI
                : enemyTraitUI;
        TraitsDisplay display = ui.GetTraitDisplay(trait);
        if (display == null)
            return;

        ui.UpdateTraitProgress(trait, progress, currentCap);

        display.Progression = progress;
        display.ShowProgress(progress, currentCap);
    }
    public bool OwnerHasTrait(PlayerOwner owner, CardData.Trait trait, int minTier = 1)
    {
        TraitSystem system =
            owner == PlayerOwner.Player
                ? allyTraitSystem
                : enemyTraitSystem;

        return system.HasTraitAtTier(trait, minTier);
    }

    private void OnAllyTraitActivated(CardData.Trait trait, int tier)
    {
        allyTraitUI.ActivateTrait(trait, tier);
    }
    private void OnEnemyTraitActivated(CardData.Trait trait, int tier)
    {
        enemyTraitUI.ActivateTrait(trait, tier);
    }
    #endregion

    #region Core Management
    private void SetupCores()
    {
        //PlayerCore = Instantiate(corePrefab, spawnPlayerCore.transform).GetComponent<CoreInstance>();
        PlayerCore.Initialize(PlayerOwner.Player, startingCoreHealth);

        //EnemyCore = Instantiate(corePrefab, spawnEnemyCore.transform).GetComponent<CoreInstance>();
        EnemyCore.Initialize(PlayerOwner.Enemy, startingCoreHealth);
    }
    public void OnCoreDestroyed(PlayerOwner owner)
    {
        if (CurrentGameState != GameState.Playing)
            return; // prevent double fire

        if (owner == PlayerOwner.Player)
        {
            CurrentGameState = GameState.PlayerLost;
            Debug.Log("PLAYER LOSES");
            ModifyUserGold(20);
        }
        else
        {
            CurrentGameState = GameState.PlayerWon;
            Debug.Log("PLAYER WINS");
            ModifyUserGold(100);
        }

        EndGame();
    }
    private void ModifyUserGold(int delta)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        db.Collection("users")
          .Document(user.UserId)
          .UpdateAsync("gold", FieldValue.Increment(delta))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to modify gold.");
                    return;
                }
            });

    }
    #endregion

    #region Mana Management
    private void InitializeMana()
    {
        AllyCurrentMana = 0;
        AllyCurrentMaxMana = 0;
        AllyBonusManaCap = 0;

        EnemyCurrentMana = 0;
        EnemyCurrentMaxMana = 0;
        EnemyBonusManaCap = 0;
    }
    public void RefreshMaxMana(PlayerOwner owner)
    {
        if (owner == PlayerOwner.Player)
            AllyCurrentMana = AllyCurrentMaxMana;
        else
            EnemyCurrentMana = EnemyCurrentMaxMana;
    }
    public void UseMana(int mana, PlayerOwner owner)
    {
        if (owner == PlayerOwner.Player)
            AllyCurrentMana -= mana;
        else
            EnemyCurrentMana -= mana;
    }
    public void GainMana(int mana, PlayerOwner owner)
    {
        if (owner == PlayerOwner.Player)
            AllyCurrentMana += mana;
        else
            EnemyCurrentMana += mana;
    }
    private int GetEffectiveManaCap(PlayerOwner owner)
    {
        return baseManaCap + GetBonusManaCap(owner);
    }

    private int GetBonusManaCap(PlayerOwner owner)
    {
        return owner == PlayerOwner.Player
            ? AllyBonusManaCap
            : EnemyBonusManaCap;
    }
    private void IncreaseMaxMana(PlayerOwner owner)
    {
        int effectiveCap = GetEffectiveManaCap(owner);

        if (owner == PlayerOwner.Player)
        {
            if (AllyCurrentMaxMana < effectiveCap)
                AllyCurrentMaxMana++;
        }
        else
        {
            if (EnemyCurrentMaxMana < effectiveCap)
                EnemyCurrentMaxMana++;
        }
    }
    private void RefillMana(PlayerOwner owner)
    {
        if (owner == PlayerOwner.Player)
            AllyCurrentMana = AllyCurrentMaxMana;
        else
            EnemyCurrentMana = EnemyCurrentMaxMana;
    }

    #endregion

    #region Effects
    public int ActiveEffectCount { get; private set; } = 0;
    public void EnqueueDeferredAction(Action action)
    {
        if (IsResolvingEffects)
        {
            deferredActions.Enqueue(action);
        }
        else
        {
            action.Invoke();
        }
    }

    public bool IsResolvingEffects => ActiveEffectCount > 0;

    public event Action OnAllEffectsResolved;

    public void BeginEffect()
    {
        ActiveEffectCount++;
    }
    public void EndEffect()
    {
        ActiveEffectCount = Mathf.Max(0, ActiveEffectCount - 1);

        if (ActiveEffectCount == 0)
        {
            OnAllEffectsResolved?.Invoke();
            ResolveDeferredActions();
        }
    }

    private void ResolveDeferredActions()
    {
        while (deferredActions.Count > 0)
        {
            deferredActions.Dequeue().Invoke();
        }
    }

    public IEnumerator DamageRandomEnemy(bool andCore, int ticsDmg, PlayerOwner owner)
    {
        BeginEffect();
        for (int i = 0; i < ticsDmg; i++)
        {
            if (andCore) GetCoreForEnemy(owner).TakeDamage(1);
            if (GetBoardForOther(owner).GetCards().Count > 0)
            {
                int rndTarget = UnityEngine.Random.Range(0,GetBoardForOther(owner).GetCards().Count);
                GetBoardForOther(owner).GetCards()[rndTarget].GetComponent<CardInstance>().TakeDamage(1);
                Debug.Log("Dealt damage to enemy GUN");
            }
            yield return new WaitForSeconds(0.5f);
        }
        EndEffect();
    }
    // reservation counters to avoid concurrent over-summon
    private int allyPendingSummons = 0;
    private int enemyPendingSummons = 0;
    private readonly Dictionary<CardInstance, int> deploySummonCaps = new();

    private int GetPendingSummons(PlayerOwner owner) =>
        owner == PlayerOwner.Player ? allyPendingSummons : enemyPendingSummons;

    private void IncPendingSummons(PlayerOwner owner)
    {
        if (owner == PlayerOwner.Player) allyPendingSummons++;
        else enemyPendingSummons++;
    }

    private void DecPendingSummons(PlayerOwner owner)
    {
        if (owner == PlayerOwner.Player) allyPendingSummons = Mathf.Max(0, allyPendingSummons - 1);
        else enemyPendingSummons = Mathf.Max(0, enemyPendingSummons - 1);
    }

    public void SetDeploySummonCap(CardInstance source, int allowedSummons)
    {
        if (source == null)
            return;

        deploySummonCaps[source] = Mathf.Max(0, allowedSummons);
    }

    public bool ConsumeDeploySummonSlot(CardInstance source)
    {
        if (source == null)
            return true;

        if (!deploySummonCaps.TryGetValue(source, out int remaining))
            return true;

        if (remaining <= 0)
            return false;

        deploySummonCaps[source] = remaining - 1;
        return true;
    }

    public void ClearDeploySummonCap(CardInstance source)
    {
        if (source == null)
            return;

        deploySummonCaps.Remove(source);
    }

    public bool TrySummonForOwnerSafe(PlayerOwner owner, int cardId, bool isTrait = false)
    {
        var board = GetBoardForOwner(owner);

        if (board.IsFull())
            return false;

        TrySummonForOwner(owner, cardId, isTrait);
        return true;
    }
    public void TrySummonForOwner(PlayerOwner owner, int cardId, bool isTrait = false)
    {
        var board = GetBoardForOwner(owner);
        if (board == null) return;

        // compute effective occupied slots including pending reservations
        int effectiveCount = board.GetCards().Count + GetPendingSummons(owner);
        if (effectiveCount >= (owner == PlayerOwner.Player ? allyDropArea.maxBoardSize : enemyDropArea.maxBoardSize))
            return;

        // Reserve a slot immediately to prevent other concurrent summons from oversubscribing.
        IncPendingSummons(owner);

        CardData data = CardDatabase.Instance.GetCardById(cardId);
        if (data == null)
        {
            DecPendingSummons(owner);
            return;
        }

        ICardDropArea parent = owner == PlayerOwner.Player ? allyDropArea : enemyDropArea;

        // create the card (this instantiates a GameObject)
        CardInstance cardInst = CardFactory.Instance.CreateCard(data, owner, parent.CardContainer);

        if (cardInst == null)
        {
            DecPendingSummons(owner);
            return;
        }

        // attempt to add to board
        if (owner == PlayerOwner.Player)
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 1;
            cardInst.SetZone(CardZone.Board);
            allyDropArea.AddSummonedCard(cardInst);
            allyDropArea.UpdateAllyCardPositions();
        }
        else
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 3;
            cardInst.SetZone(CardZone.Board);
            enemyDropArea.AddSummonedCard(cardInst);
            enemyDropArea.UpdateEnemyCardPositions();
        }

        // Verify the card was actually added to board list (AddSummonedCard may early-return when full)
        bool actuallyAdded = parent.GetCards().Contains(cardInst.gameObject);

        if (!actuallyAdded)
        {
            // board might have filled up in-between, destroy created GameObject and free reservation
            Destroy(cardInst.gameObject);
            DecPendingSummons(owner);
            return;
        }

        // successful add -> release reservation
        DecPendingSummons(owner);

        if (isTrait)
        {
            StartCoroutine(DelayedDeploy(cardInst, forceRandomTarget: true));
        }
    }

    public IEnumerator DelayedDeploy(CardInstance card, bool forceRandomTarget = false)
    {
        yield return null; // wait one frame
        if (card != null)
            card.TriggerDeploy(forceRandomTarget);
    }
    public void TrySummonForOther(PlayerOwner owner, int cardId)
    {
        // This means: summon on the opponent's board
        var board = GetBoardForOther(owner);
        if (board == null)
            return;

        PlayerOwner targetOwner =
            owner == PlayerOwner.Player
                ? PlayerOwner.Enemy
                : PlayerOwner.Player;

        int effectiveCount =
            board.GetCards().Count + GetPendingSummons(targetOwner);

        int maxSize =
    targetOwner == PlayerOwner.Player
        ? allyDropArea.maxBoardSize
        : enemyDropArea.maxBoardSize;

        if (effectiveCount >= maxSize)
            return;

        // 🔒 Reserve slot
        IncPendingSummons(targetOwner);

        CardData data = CardDatabase.Instance.GetCardById(cardId);
        if (data == null)
        {
            DecPendingSummons(targetOwner);
            return;
        }

        ICardDropArea parent =
            targetOwner == PlayerOwner.Player
                ? allyDropArea
                : enemyDropArea;

        CardInstance cardInst =
            CardFactory.Instance.CreateCard(data, targetOwner, parent.CardContainer);

        if (cardInst == null)
        {
            DecPendingSummons(targetOwner);
            return;
        }

        // Add to board
        if (targetOwner == PlayerOwner.Player)
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 1;
            cardInst.SetZone(CardZone.Board);
            allyDropArea.AddSummonedCard(cardInst);
            allyDropArea.UpdateAllyCardPositions();
        }
        else
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 3;
            cardInst.SetZone(CardZone.Board);
            enemyDropArea.AddSummonedCard(cardInst);
            enemyDropArea.UpdateEnemyCardPositions();
        }

        // Verify success
        bool actuallyAdded =
            parent.GetCards().Contains(cardInst.gameObject);

        if (!actuallyAdded)
        {
            Destroy(cardInst.gameObject);
            DecPendingSummons(targetOwner);
            return;
        }

        // ✅ Success
        DecPendingSummons(targetOwner);
    }

    public void Praise(PlayerOwner owner)
    {
        OnPraise?.Invoke(owner);
    }
    public void DamageRandomEnemyAmount(int amount, PlayerOwner owner)
    {
        List<IAttackable> possibleTargets = new();

        // Add enemy minions
        var enemyBoard = GetBoardForOther(owner).GetCards();
        foreach (var go in enemyBoard)
        {
            if (go == null) continue;

            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci == null || ci.IsDead) continue;

            possibleTargets.Add(ci);
        }

        CoreInstance enemyCore = GetCoreForEnemy(owner);

        // If no minions, core MUST be hit
        if (possibleTargets.Count == 0)
        {
            enemyCore.TakeDamage(amount);
            Debug.Log("Random damage hit enemy CORE (no units)");
            return;
        }

        // If minions exist, core is also a valid random target
        possibleTargets.Add(enemyCore);

        // Pick random target
        IAttackable target =
            possibleTargets[UnityEngine.Random.Range(0, possibleTargets.Count)];

        target.TakeDamage(amount);

        Debug.Log($"Random damage hit: {target.GetType().Name}");
    }
    public void EmperorSapphire(PlayerOwner owner)
    {
        // 🔑 Snapshot the board to avoid collection modification
        List<GameObject> allOwnerAllies =
            new List<GameObject>(GetBoardForOwner(owner).GetCards());

        foreach (GameObject ownerAlly in allOwnerAllies)
        {
            if (ownerAlly == null) continue;

            CardInstance cardInst = ownerAlly.GetComponent<CardInstance>();
            if (cardInst == null || cardInst.IsDead) continue;

            if (cardInst.Data.id == 15)
            {
                StartCoroutine(KillCrystalWithDelay(cardInst, owner));
            }
        }
    }
    private IEnumerator KillCrystalWithDelay(CardInstance cardInst, PlayerOwner owner)
    {
        yield return new WaitForSeconds(0.1f);

        if (cardInst == null || cardInst.IsDead)
            yield break;

        cardInst.TakeDamage(999);
        DamageRandomEnemyAmount(7, owner);
    }

    public void OnDamageWithCard(PlayerOwner owner)
    {
        OnDamageCard?.Invoke(owner);
    }
    public void LimitEnemySpace(PlayerOwner effectOwner, int limit)
    {
        ICardDropArea board =
            effectOwner == PlayerOwner.Player
                ? enemyDropArea
                : allyDropArea;

        if (board is EnemyCardDropArea enemyBoard)
        {
            ApplyBoardLimit(enemyBoard.enemyPrefabCards, limit, enemyBoard.HandleEnemyDeath, enemyBoard.UpdateEnemyCardPositions);
            enemyBoard.maxBoardSize = limit;
        }
        else if (board is AllyCardDropArea allyBoard)
        {
            ApplyBoardLimit(allyBoard.allyPrefabCards, limit, allyBoard.HandleAllyDeath, allyBoard.UpdateAllyCardPositions);
            allyBoard.maxBoardSize = limit;
        }
    }
    private void ApplyBoardLimit(   List<GameObject> cards,   int limit,
        System.Action<CardInstance> deathHandler,   System.Action updateLayout)
    {
        if (cards == null)
            return;

        Debug.Log("Applying Board limit");

        // Destroy newest cards first
        while (cards.Count > limit)
        {
            GameObject last = cards[^1];

            if (last == null)
            {
                cards.RemoveAt(cards.Count - 1);
                continue;
            }

            CardInstance ci = last.GetComponent<CardInstance>();
            if (ci != null && !ci.IsDead)
            {
                // 🔑 death handler will remove + destroy + relayout
                deathHandler.Invoke(ci);
            }
            else
            {
                cards.RemoveAt(cards.Count - 1);
                Destroy(last);
            }
        }

        updateLayout?.Invoke();
    }

    public void Discover(int id1, int id2, int id3, PlayerOwner owner)
    {
        if(owner == PlayerOwner.Enemy)
        {
            if (OwnerHasTrait(owner, CardData.Trait.Faith, 2))
            {
                int randintDisc = UnityEngine.Random.Range(0, 3);
                if (randintDisc == 0) AddCardToHand(PlayerOwner.Enemy, id1,-1);
                if (randintDisc == 1) AddCardToHand(PlayerOwner.Enemy, id2,-1);
                if (randintDisc == 2) AddCardToHand(PlayerOwner.Enemy, id3,-1);
                if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner);
                return;
            }
            int randint = UnityEngine.Random.Range(0, 3);
            if (randint == 0) AddCardToHand(PlayerOwner.Enemy, id1);
            if (randint == 1) AddCardToHand(PlayerOwner.Enemy, id2);
            if (randint == 2) AddCardToHand(PlayerOwner.Enemy, id3);
            if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner);
            return;
        }
        isDiscovering = true;
        discoverDisplay.SetActive(true);
        CardData data1 = CardDatabase.Instance.GetCardById(id1);
        CardInstance dataInst1 = CardFactory.Instance.CreateCardInPosition(data1, PlayerOwner.Player, Vector3.zero, new Vector3(0.6f,0.6f,0.6f), discoverDisplay.transform);
        dataInst1.IsDisplay = true;
        dataInst1.GetComponent<SortingGroup>().sortingOrder = 201;
        CardData data2 = CardDatabase.Instance.GetCardById(id2);
        CardInstance dataInst2 = CardFactory.Instance.CreateCardInPosition(data2, PlayerOwner.Player, new Vector3(5,0,0), new Vector3(0.6f,0.6f,0.6f), discoverDisplay.transform);
        dataInst2.IsDisplay = true;
        dataInst2.GetComponent<SortingGroup>().sortingOrder = 201;
        CardData data3 = CardDatabase.Instance.GetCardById(id3);
        CardInstance dataInst3 = CardFactory.Instance.CreateCardInPosition(data3, PlayerOwner.Player, new Vector3(-5, 0, 0), new Vector3(0.6f,0.6f,0.6f), discoverDisplay.transform);
        dataInst3.IsDisplay = true;
        dataInst3.GetComponent<SortingGroup>().sortingOrder = 201;

        //Call Discover
        OnDiscover?.Invoke(owner);
    }
    public void DiscoverEffect(string effect, PlayerOwner owner)
    {
        List<CardData> options = CardDatabase.Instance.GetCardsByEffect(effect+"*");
        Debug.Log(effect + "*");
        string res = "options :";
        foreach(CardData option in options)
        {
            res += option.name + " ";
        }
        if (options.Count <= 0) return;
        if (owner == PlayerOwner.Enemy)
        {
            if(OwnerHasTrait(owner, CardData.Trait.Faith, 2))
            {
                AddCardToHand(PlayerOwner.Enemy, options[UnityEngine.Random.Range(0, options.Count)].id,-1);
                if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner);
                return;
            }
            AddCardToHand(PlayerOwner.Enemy, options[UnityEngine.Random.Range(0, options.Count)].id);
            if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner);
            return;
        }
        isDiscovering = true;
        discoverDisplay.SetActive(true);
        CardData data1 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst1 = CardFactory.Instance.CreateCardInPosition(data1, PlayerOwner.Player, Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst1.IsDisplay = true;
        dataInst1.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data1);
        CardData data2 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst2 = CardFactory.Instance.CreateCardInPosition(data2, PlayerOwner.Player, new Vector3(5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst2.IsDisplay = true;
        dataInst2.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data2);
        CardData data3 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst3 = CardFactory.Instance.CreateCardInPosition(data3, PlayerOwner.Player, new Vector3(-5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst3.IsDisplay = true;
        dataInst3.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data3);
        //Call Discover
        OnDiscover?.Invoke(owner);
    }
    public void DiscoverTrait(string trait, PlayerOwner owner)
    {
        List<CardData> options =
       new List<CardData>(CardDatabase.Instance.GetCardsByTraitPackable(trait));

        string res = "options :";
        foreach (CardData option in options)
        {
            res += option.name + " ";
        }
        if (options.Count <= 0) return;
        if (owner == PlayerOwner.Enemy)
        {
            if (OwnerHasTrait(owner, CardData.Trait.Faith, 2))
            {
                AddCardToHand(PlayerOwner.Enemy, options[UnityEngine.Random.Range(0, options.Count)].id, -1);
                if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner);
                return;
            }
            AddCardToHand(PlayerOwner.Enemy, options[UnityEngine.Random.Range(0, options.Count)].id);
            if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner);
            return;
        }
        isDiscovering = true;
        discoverDisplay.SetActive(true);
        CardData data1 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst1 = CardFactory.Instance.CreateCardInPosition(data1, PlayerOwner.Player, Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst1.IsDisplay = true;
        dataInst1.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data1);
        CardData data2 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst2 = CardFactory.Instance.CreateCardInPosition(data2, PlayerOwner.Player, new Vector3(5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst2.IsDisplay = true;
        dataInst2.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data2);
        CardData data3 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst3 = CardFactory.Instance.CreateCardInPosition(data3, PlayerOwner.Player, new Vector3(-5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst3.IsDisplay = true;
        dataInst3.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data3);
        //Call Discover
        OnDiscover?.Invoke(owner);
    }
    public void DiscoverOwnerTrait(PlayerOwner owner)
    {
        // 1️⃣ Get trait pool from DECK (not active effects)
        Dictionary<CardData.Trait, int> traitPool =
            owner == PlayerOwner.Player
                ? deckManager.AllyTraitsUnlockable
                : deckManager.EnemyTraitsUnlockable;

        if (traitPool == null || traitPool.Count == 0)
            return;

        // 2️⃣ Pick up to 3 distinct traits
        // 2️⃣ Build exactly 3 traits based on owner's pool
        List<CardData.Trait> availableTraits =
            traitPool.Keys
                     .Where(t => t != CardData.Trait.Neutral)
                     .ToList();

        List<CardData.Trait> traits = new();

        if (availableTraits.Count == 0)
        {
            // No traits → all Neutral
            traits.Add(CardData.Trait.Neutral);
            traits.Add(CardData.Trait.Neutral);
            traits.Add(CardData.Trait.Neutral);
        }
        else if (availableTraits.Count == 1)
        {
            // One trait → all same
            traits.Add(availableTraits[0]);
            traits.Add(availableTraits[0]);
            traits.Add(availableTraits[0]);
        }
        else if (availableTraits.Count == 2)
        {
            // Two traits → one duplicated, one single
            bool firstIsDouble = UnityEngine.Random.value < 0.5f;

            CardData.Trait a = availableTraits[0];
            CardData.Trait b = availableTraits[1];

            if (firstIsDouble)
            {
                traits.Add(a);
                traits.Add(a);
                traits.Add(b);
            }
            else
            {
                traits.Add(b);
                traits.Add(b);
                traits.Add(a);
            }
        }
        else
        {
            // 3+ traits → pick 3 distinct
            traits = availableTraits
                .OrderBy(_ => UnityEngine.Random.value)
                .Take(3)
                .ToList();
        }


        // 3️⃣ Pick one card per trait
        List<CardData> options = new();

        foreach (var trait in traits)
        {
            List<CardData> cards =
                CardDatabase.Instance.GetCardsByTraitPackable(trait.ToString());

            if (cards.Count > 0)
                options.Add(cards[UnityEngine.Random.Range(0, cards.Count)]);
        }

        if (options.Count == 0)
            return;

        // 4️⃣ Enemy: auto-pick
        if (owner == PlayerOwner.Enemy)
        {
            CardData choice = options[UnityEngine.Random.Range(0, options.Count)];

            if (OwnerHasTrait(owner, CardData.Trait.Faith, 2))
                AddCardToHand(owner, choice.id, -1);
            else
                AddCardToHand(owner, choice.id);

            if (OwnerHasTrait(owner, CardData.Trait.Faith, 3))
                GainMana(1, owner);

            return;
        }

        // 5️⃣ Player: Discover UI
        isDiscovering = true;
        discoverDisplay.SetActive(true);

        float[] xPos = { 0f, 5f, -5f };

        for (int i = 0; i < options.Count && i < 3; i++)
        {
            CardInstance preview =
                CardFactory.Instance.CreateCardInPosition(
                    options[i],
                    PlayerOwner.Player,
                    new Vector3(xPos[i], 0, 0),
                    new Vector3(0.6f, 0.6f, 0.6f),
                    discoverDisplay.transform
                );

            preview.IsDisplay = true;
            preview.GetComponent<SortingGroup>().sortingOrder = 201;
        }

        OnDiscover?.Invoke(owner);
    }

    public void ShuffleInDeck(int id, PlayerOwner owner)
    {
        CardData shuffledCard = CardDatabase.Instance.GetCardById(id);
        deckManager.decks[owner].Enqueue(shuffledCard);
        deckManager.Shuffle(deckManager.decks[owner]);
    }
    public CardInstance AddCardToHand(PlayerOwner owner, int id)
    {
        HandManager hand = owner == PlayerOwner.Player
            ? allyHand
            : enemyHand;

        if (hand.handCards.Count >= hand.maxHandSize)
        {
            Debug.Log($"{owner} hand is full.");
            return null;
        }

        CardData data = CardDatabase.Instance.GetCardById(id);

        CardInstance card =
            CardFactory.Instance.CreateCard(data, owner);

        card.SetZone(CardZone.Hand);
        hand.AddCard(card.gameObject);
        hand.UpdateCardPositions();

        return card;
    }
    public CardInstance AddRandomCardToHandType(PlayerOwner owner, string type)
    {
        HandManager hand = owner == PlayerOwner.Player
            ? allyHand
            : enemyHand;

        if (hand.handCards.Count >= hand.maxHandSize)
        {
            Debug.Log($"{owner} hand is full.");
            return null;
        }

        List<CardData> options = CardDatabase.Instance.GetCardsByTypePackable(type);

        if (options.Count == 0)
            return null;
        CardData data = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance card =
            CardFactory.Instance.CreateCard(data, owner);

        card.SetZone(CardZone.Hand);
        hand.AddCard(card.gameObject);
        hand.UpdateCardPositions();

        return card;
    }
    public CardInstance AddRandomCardToHandText(PlayerOwner owner, string text)
    {
        HandManager hand = owner == PlayerOwner.Player
            ? allyHand
            : enemyHand;

        if (hand.handCards.Count >= hand.maxHandSize)
        {
            Debug.Log($"{owner} hand is full.");
            return null;
        }

        List<CardData> options = CardDatabase.Instance.GetCardsByTextPackable(text);

        if (options.Count == 0)
            return null;
        CardData data = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance card =
            CardFactory.Instance.CreateCard(data, owner);

        card.SetZone(CardZone.Hand);
        hand.AddCard(card.gameObject);
        hand.UpdateCardPositions();

        return card;
    }
    public CardInstance AddCardToHand(PlayerOwner owner, int id, int discount)
    {
        HandManager hand = owner == PlayerOwner.Player
            ? allyHand
            : enemyHand;

        if (hand.handCards.Count >= hand.maxHandSize)
        {
            Debug.Log($"{owner} hand is full.");
            return null;
        }

        CardData data = CardDatabase.Instance.GetCardById(id);

        CardInstance card =
            CardFactory.Instance.CreateCard(data, owner);

        card.SetZone(CardZone.Hand);
        card.AddTemporaryManaModifier(discount);
        hand.AddCard(card.gameObject);
        hand.UpdateCardPositions();

        return card;
    }
    public void ResurrectLast(PlayerOwner owner, CardData excluded)
    {
        Graveyard graveyard =
            owner == PlayerOwner.Player
                ? PlayerGraveyard
                : EnemyGraveyard;

        CardData data = graveyard.PopLastExcluding(excluded);
        if (data == null)
            return;

        TrySummonForOwner(owner, data.id);
    }
    public void ResurrectRandom(PlayerOwner owner, CardData excluded)
    {
        Graveyard graveyard =
            owner == PlayerOwner.Player
                ? PlayerGraveyard
                : EnemyGraveyard;

        CardData data = graveyard.PopRandomExcluding(excluded);
        if (data == null)
            return;

        TrySummonForOwner(owner, data.id);
    }
    #endregion

    #region Combat Manager
    public void NotifyCardKilled(CardInstance deadCard)
    {
        OnCardKilled?.Invoke(deadCard);

        // 🔑 Add to graveyard
        if (deadCard.Owner == PlayerOwner.Player)
            PlayerGraveyard.Add(deadCard.Data);
        else
            EnemyGraveyard.Add(deadCard.Data);

        if (deadCard.Data.id == 86)
            DistortionWorld = false;
    }

    public void NotifyHealed(PlayerOwner owner, int amount)
    {
        OnOwnerHeal?.Invoke(owner, amount);
    }
    public void NotifyDamage(PlayerOwner owner, int amount)
    {
        OnOwnerDamage?.Invoke(owner, amount);
    }
    public void CancelCurrentTargeting()
    {
        // Cancel attack targeting
        if (isTargettingAttack)
        {
            CancelAttackTargeting();
            return;
        }

        // Cancel effect / spell targeting
        if (isTargetingEffect)
        {
            CancelEffectTargeting();
            return;
        }
    }
    private void CancelEffectTargeting()
    {
        if (effectSource == null)
            return;
        Debug.Log("[CANCEL] Effect targeting cancelled");
        if (effectSource is not CardInstance card)
            return;

        card.SetZone(CardZone.Hand);
        card.GetComponent<CardView>().UpdateMode();
        card.ClearTemporaryManaModifiers();
        card.GetComponent<Card>().ResetCard();
        card.DeployPending = false;
        card.CurrentCastEffect = null;
        if(card.Data.cardType=="minion")GainMana(card.CurrentManaCost, PlayerOwner.Player);
        // Restore card so it behaves like a fresh hand card
        card.WasPlayed = true;
        card.EffectsSuppressed = false;
        card.DeployPending = false;
        card.IsSummoningSick = false;

        // Rebuild effect parsing & progress hooks
        card.InitializeProgressIfAny();
        card.ParseEffects();

        allyHand.AddCard(card.gameObject);
        allyHand.UpdateCardPositions();
        allyDropArea.allyPrefabCards.Remove(card.gameObject);

        isTargetingEffect = false;
        attackCursor.gameObject.SetActive(false);

        card = null;
        onEffectTargetChosen = null;
        targetType = EffectTarget.None;

        CheckGlow();
    }

    private void CancelAttackTargeting()
    {
        isTargettingAttack = false;
        attackCursor.gameObject.SetActive(false);

        // IMPORTANT: do NOT mark attacker as having attacked
        currentAttacker = null;

        // Reset glows
        CheckGlow();
    }

    public void ShakeCameraForDamage(int damage)
    {
        float strength = 0f;
        float duration = 0.1f;

        if (damage <= 3)
            return; // no shake
        else if (damage <= 5)
            strength = 0.08f;
        else if (damage <= 9)
            strength = 0.14f;
        else
            strength = Mathf.Clamp(0.18f + (damage - 10) * 0.03f, 0.18f, 0.35f);

        cameraShakeTween?.Kill();

        cameraShakeTween = mainCamera.transform.DOShakePosition(
            duration,
            strength,
            vibrato: 18,
            randomness: 90,
            fadeOut: true
        ).OnComplete(() =>
        {
            mainCamera.transform.position = cameraBasePos;
        });
    }
    public void BeginAttack(Card attacker)
    {
        if (CurrentGameState != GameState.Playing)
            return;

        CardInstance attackerInst = attacker.GetComponent<CardInstance>();
        if (attackerInst == null)
            return;

        if (attackerInst.Data.cardType.ToLower() == "spell")//Add Spell Logic later
            return;

        if (attackerInst.CurrentZone != CardZone.Board)
            return;

        if (!TurnManager.Instance.IsPlayerTurn(attackerInst.Owner))
            return;

        if (!CanSelectAttacker(attackerInst))
            return;


        // 🔑 Cancel previous selection safely
        currentAttacker = attacker;
        isTargettingAttack = true;
        attackCursor.gameObject.SetActive(true);
    }
    public IEnumerator Tawakkul(int damage)
    {
        BeginEffect(); // 🔒 LOCK TURN SYSTEM

        try
        {
            while (true)
            {
                List<IAttackable> targets = GetAllDamageableTargets();

                // Stop when no minions remain
                if (!targets.Any(t => t is CardInstance))
                    break;

                IAttackable target =
                    targets[UnityEngine.Random.Range(0, targets.Count)];

                target.TakeDamage(damage);

                yield return new WaitForSeconds(0.5f);
            }
        }
        finally
        {
            EndEffect(); // 🔓 ALWAYS UNLOCK
        }
    }

    private List<IAttackable> GetAllDamageableTargets()
    {
        List<IAttackable> targets = new List<IAttackable>();

        // Ally minions
        foreach (GameObject go in allyDropArea.allyPrefabCards)
        {
            if (go == null) continue;

            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci != null && !ci.IsDead)
                targets.Add(ci);
        }

        // Enemy minions
        foreach (GameObject go in enemyDropArea.enemyPrefabCards)
        {
            if (go == null) continue;

            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci != null && !ci.IsDead)
                targets.Add(ci);
        }

        // Always include both cores
        targets.Add(PlayerCore);
        targets.Add(EnemyCore);

        return targets;
    }

    public bool CanSelectAttacker(CardInstance attacker)
    {
        if (!TurnManager.Instance.IsPlayerTurn(attacker.Owner))
            return false;
        if (isTargetingEffect) return false;
        if ((attacker.HasAttackedThisTurn && !attacker.HasKeyword("haste")) ||
            (attacker.HasAttackedThisTurn && attacker.HasKeyword("haste") && attacker.HasAttackedTwiceThisTurn)||
            attacker.CurrentAttack<=0 || attacker.IsAsleep)
            return false;

        if (attacker.IsSummoningSick)
        {
            // Can only attack units or core if keyword allows it
            if (!attacker.CanAttackUnitOnSummon() && !attacker.CanAttackCoreOnSummon())
                return false;
        }

        if (attacker.IsAsleep)
            return false;

        return true;
    }
    public ICardDropArea GetBoardForOwner(PlayerOwner owner)
    {
        return owner == PlayerOwner.Player
            ? allyDropArea
            : enemyDropArea;
    }
    public ICardDropArea GetBoardForOther(PlayerOwner owner)
    {
        return owner == PlayerOwner.Player
            ? enemyDropArea
            : allyDropArea;
    }
    private CoreInstance GetCoreForOwner(PlayerOwner owner)
    {
        return owner == PlayerOwner.Player
            ? PlayerCore
            : EnemyCore;
    }
    private CoreInstance GetCoreForEnemy(PlayerOwner owner)
    {
        return owner == PlayerOwner.Player
            ? EnemyCore
            : PlayerCore;
    }
    public List<IAttackable> GetValidTargets(CardInstance attacker)
    {
        List<IAttackable> targets = new();

        var defendingBoard = GetBoardForOwner(
            attacker.Owner == PlayerOwner.Player
                ? PlayerOwner.Enemy
                : PlayerOwner.Player
        );

        // Only consider alive units
        List<CardInstance> aliveNonHiddenUnits = new();

        foreach (var go in defendingBoard.GetCards())
        {
            if (go == null || !go.activeSelf)
                continue;

            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci == null || ci.IsDead || ci.HasKeyword("hidden"))
                continue;

            aliveNonHiddenUnits.Add(ci);
        }

        bool hasProtect = aliveNonHiddenUnits.Exists(ci => ci.HasKeyword("protect") && !ci.HasKeyword("hidden"));

        foreach (var ci in aliveNonHiddenUnits)
        {
            if (hasProtect && !ci.HasKeyword("protect"))
                continue;

            targets.Add(ci);
        }

        if (!hasProtect)
        {
            if (!attacker.IsSummoningSick || attacker.CanAttackCoreOnSummon())
            {
                CoreInstance core = GetCoreForOwner(defendingBoard.Owner);
                targets.Add(core);
            }
        }

        return targets;
    }
    public List<IAttackable> GetValidTargets(PlayerOwner owner)
    {
        List<IAttackable> targets = new();

        var defendingBoard = GetBoardForOwner(owner
        );

        // Only consider alive units
        List<CardInstance> aliveNonHiddenUnits = new();

        foreach (var go in defendingBoard.GetCards())
        {
            if (go == null || !go.activeSelf)
                continue;

            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci == null || ci.IsDead || ci.HasKeyword("hidden"))
                continue;

            aliveNonHiddenUnits.Add(ci);
        }

        bool hasProtect = aliveNonHiddenUnits.Exists(ci => ci.HasKeyword("protect"));

        foreach (var ci in aliveNonHiddenUnits)
        {
            if (hasProtect && !ci.HasKeyword("protect"))
                continue;

            targets.Add(ci);
        }

        if (!hasProtect)
        {
            CoreInstance core = GetCoreForOwner(defendingBoard.Owner);
            targets.Add(core);
        }

        return targets;
    }
    public void CheckGlow()
    {
            foreach (GameObject cardGO in allyDropArea.allyPrefabCards)
            {
                CardInstance ci = cardGO.GetComponent<CardInstance>();
                CardView view = ci.GetComponent<CardView>();

                if (CanSelectAttacker(ci))
                    view.SetGlow(CardView.CardGlowState.CanAttack);
                else
                    view.SetGlow(CardView.CardGlowState.None);
            }
            foreach (IAttackable targets in GetValidTargets(PlayerOwner.Enemy))
            {
                if (targets is CardInstance ci)
                {
                    ci.GetComponent<CardView>()
                        .SetGlow(CardView.CardGlowState.CanBeTargeted);
                }
            }
        
    }
    public void ResolveAttack(CardInstance attacker, IAttackable target)
    {
        if (attacker == null || target == null)
            return;
        // 🔒 FINAL SUMMON-TURN VALIDATION (authoritative)
        if (attacker.IsSummoningSick)
        {
            if (target is CoreInstance && !attacker.CanAttackCoreOnSummon())
                return;

            if (target is CardInstance && !attacker.CanAttackUnitOnSummon())
                return;
        }

        if (!CanSelectAttacker(attacker))
            return;

        if (attacker.Owner == target.Owner)
            return;
        //Handle Haste Scenario
        if (attacker.HasAttackedThisTurn && attacker.HasKeyword("haste"))
            attacker.HasAttackedTwiceThisTurn = true;

        attacker.HasAttackedThisTurn = true;
        attacker.RemoveEffect("hidden");

        if (attacker.Owner == PlayerOwner.Player)
        {
            CheckGlow();
        }

        OnCardAttack?.Invoke(attacker);

        // UNIT vs UNIT
        if (target is CardInstance targetUnit)
        {
            bool isKill = false;
            int attackerDmg = attacker.CurrentAttack;
            int defenderDmg = targetUnit.CurrentAttack;
            if (attackerDmg >= targetUnit.CurrentHealth && !targetUnit.HasKeyword("blessed")) isKill = true;
            int thornDamage = 0;
            attacker.TriggerStrike();
            //Taking thorns damage
            if (targetUnit.HasKeyword("thorns"))
            {
                thornDamage = targetUnit.ThornsDamage;
            }
            //Apply Bleeding to target
            if (attacker.HasKeyword("bleed"))
            {
                targetUnit.IsBleeding = true;
                targetUnit.GetComponent<CardView>().UpdateMode();
            }
            //Apply lifesteal Heal before damage if enemy not blessed
            if (attacker.HasKeyword("lifesteal") && !targetUnit.HasKeyword("blessed"))
            {
                attacker.AutoHealCore(attacker.CurrentAttack);
            }
            if (targetUnit.HasKeyword("lifesteal") && !attacker.HasKeyword("blessed"))
            {
                targetUnit.AutoHealCore(targetUnit.CurrentAttack);
            }
            if (isKill)
            {
                OnCardKiller?.Invoke(attacker);
            }
            //Only Take damage if not blessed, remove effect if damaged
            if (!attacker.HasKeyword("blessed")) { 
                attacker.TakeDamage(defenderDmg+thornDamage);}
            else attacker.RemoveEffect("blessed");

            if(!targetUnit.HasKeyword("blessed"))
                targetUnit.TakeDamage(attackerDmg);
            else targetUnit.RemoveEffect("blessed");

            return;
        }

        // UNIT vs CORE
        if (target is CoreInstance core)
        {
            attacker.TriggerStrike();
            if (attacker.HasKeyword("bleed"))
            {
                core.IsBleeding = true;
            }

            if (attacker.HasKeyword("lifesteal"))
            {
                attacker.AutoHealCore(attacker.CurrentAttack);
            }
            core.TakeDamage(attacker.CurrentAttack);
            return;
        }
    }
    public void HandleBoardCardClick(Card card)
    {
        if (CurrentGameState != GameState.Playing)
            return;

        CardInstance clickedInst = card.GetComponent<CardInstance>();
        if (clickedInst == null)
            return;

        // CASE 1: Not targeting → try to select attacker
        if (!isTargettingAttack && !isTargetingEffect && !clickedInst.IsAsleep)
        {
            BeginAttack(card);
            return;
        }

        // CASE 2: Targeting but attacker was cleared (race condition)
        if (currentAttacker == null)
        {
            isTargettingAttack = false;
            BeginAttack(card);
            return;
        }

        // CASE 3: Valid attack attempt
        CardInstance attackerInst = currentAttacker.GetComponent<CardInstance>();
        if (attackerInst == null)
        {
            isTargettingAttack = false;
            currentAttacker = null;
            return;
        }

        if (GetValidTargets(attackerInst).Contains(clickedInst) && !attackerInst.IsAsleep)
        {
            QueueAttack(attackerInst, clickedInst);
        }
    }
    public bool CanAttackUnit(CardInstance target)
    {
        if (target.Owner == PlayerOwner.Player)
        {
            if (!target.HasKeyword("protect") && allyDropArea.HasProtectUnits())
                return false;
            else return true;
        }
        else
        {
            if (!target.HasKeyword("protect") && enemyDropArea.HasProtectUnits())
                return false;
            else return true;
        }
    }
    public void TryAttackCore(CoreInstance targetCore)
    {
        if (currentAttacker == null)
            return;
        CardInstance cardInst = currentAttacker.GetComponent<CardInstance>();

        if (cardInst.IsSummoningSick && !cardInst.CanAttackCoreOnSummon())
            return;

        if (CurrentGameState != GameState.Playing)
            return;


        if (cardInst.Owner == targetCore.Owner)
            return;

        if (cardInst.Owner == PlayerOwner.Player && enemyDropArea.HasProtectUnits())
        {
            return;
        }
        else if (cardInst.Owner == PlayerOwner.Enemy && allyDropArea.HasProtectUnits())
        {
            return;
        }
        QueueAttack(cardInst, targetCore);
        attackCursor.gameObject.SetActive(false);
        currentAttacker = null; isTargettingAttack = false;
    }
    public void QueueAttack(CardInstance attacker, IAttackable target)
    {
        // Safety checks
        if (attacker == null || target == null)
            return; 
        if (attacker.IsSummoningSick)
        {
            if (target is CoreInstance && !attacker.CanAttackCoreOnSummon())
                return;

            if (target is CardInstance && !attacker.CanAttackUnitOnSummon())
                return;
        }

        if ((attacker.HasAttackedThisTurn && !attacker.HasKeyword("haste")) ||
                (attacker.HasAttackedThisTurn && attacker.HasKeyword("haste") && attacker.HasAttackedTwiceThisTurn) || 
                    attacker.CurrentAttack<=0 ||attacker.IsAsleep)
            return;


        attackQueue.Enqueue(new AttackRequest(attacker, target));

        // Start processing if idle
        if (!isResolvingAttack)
            StartCoroutine(ProcessAttackQueue());
    }
    public bool IsResolvingAttackQueue()
    {
        return isResolvingAttack || attackQueue.Count > 0;
    }
    private Transform GetCoreProxy(PlayerOwner owner)
    {
        return owner == PlayerOwner.Player
            ? enemyCoreProxy
            : playerCoreProxy;
    }
    private IEnumerator ProcessAttackQueue()
    {
        isResolvingAttack = true;

        while (attackQueue.Count > 0)
        {
            AttackRequest req = attackQueue.Dequeue();

            // Attacker validation
            if (req.Attacker == null ||
                req.Attacker.CurrentZone != CardZone.Board ||
                (req.Attacker.HasAttackedThisTurn && !req.Attacker.HasKeyword("haste")) || 
                (req.Attacker.HasAttackedThisTurn && req.Attacker.HasKeyword("haste") && req.Attacker.HasAttackedTwiceThisTurn))
            {
                continue;
            }

            // Determine actual target (with retargeting)
            IAttackable target = req.Target;


            bool targetInvalid = false;

            if (target == null)
            {
                targetInvalid = true;
            }
            else if (target is CardInstance ci)
            {
                if (ci.IsDead || !ci.gameObject.activeSelf || ci.CurrentZone != CardZone.Board)
                    targetInvalid = true;
            }

            if (targetInvalid)
            {
                List<IAttackable> newTargets = GetValidTargets(req.Attacker);
                if (newTargets.Count == 0)
                    continue;

                target = newTargets[0];
            }


            // Stop cursor & targeting immediately
            isTargettingAttack = false;
            attackCursor.gameObject.SetActive(false);
            currentAttacker = null;

            // Animate
            CardView attackerView = req.Attacker.GetComponent<CardView>();

            if (target is CoreInstance core)
            {
                Transform proxy = GetCoreProxy(core.Owner);

                if (attackerView != null && proxy != null)
                    yield return attackerView.PlayAttackAnimation(proxy);

                yield return core.GetComponent<CoreView>()
                    .PlayHitReaction(req.Attacker.CurrentAttack);
            }
            else
            {
                CardView targetView = target.Transform.GetComponent<CardView>();

                if (attackerView != null)
                    yield return attackerView.PlayAttackAnimation(target.Transform);

                if (targetView != null)
                    yield return targetView.PlayHitReaction(req.Attacker.CurrentAttack);
            }

            // Apply combat logic AFTER visual impact
            ResolveAttack(req.Attacker, target);


            // Small pacing delay (FEELS GOOD)
            yield return new WaitForSeconds(0.05f);
        }

        isResolvingAttack = false;
        enemyDropArea.FlushLayoutIfDirty();

    }
    public void EndEffectTargetting()
    {
        // Exit effect targeting mode
        isTargetingEffect = false;

        // Hide cursor
        attackCursor.gameObject.SetActive(false);

        // Clear callbacks & state
        onEffectTargetChosen = null;
        effectSource = null;
        targetType = EffectTarget.None;

        // Refresh glows (attack / playable cards)
        CheckGlow();
    }
    public void BeginEffectTargeting(
    CardInstance source,
    PlayerOwner owner,
    Func<IAttackable, bool> onTargetChosen,
    EffectTarget effectTargetType
)
    {
        if (isTargetingEffect)
            return;

        isTargetingEffect = true;
        effectSource = source;
        effectOwner = owner;
        onEffectTargetChosen = onTargetChosen;
        targetType = effectTargetType;

        attackCursor.gameObject.SetActive(true);
    }
    private bool IsValidEffectTarget(PlayerOwner owner, IAttackable target, EffectTarget effectTargetType)
    {
        // Enforce type rules
        if ((target is CoreInstance) && effectTargetType == EffectTarget.Unit)
            return false;

        if ((target is CardInstance) && effectTargetType == EffectTarget.Core)
            return false;

        // 🔑 Allow allies by default (gear, buffs, heals)
        return true;
    }

    public IAttackable ChooseEnemyEffectTarget(EffectTarget type, bool targetPlayer, bool canTargetCore)
    {
        List<IAttackable> targets = GetValidTargets(PlayerOwner.Player);
        if (!targetPlayer)
        {
            targets = GetValidTargets(PlayerOwner.Enemy);
            if(canTargetCore)
            targets.Add(EnemyCore);
        }
        else if(canTargetCore)
        targets.Add(PlayerCore);

        if (targets.Count == 0)
            return null;

        // Filter by effect target type
        targets = targets.Where(t =>
        {
            if (type == EffectTarget.Unit || type == EffectTarget.Any)
                return t is CardInstance;
            if (type == EffectTarget.Core || type == EffectTarget.Any)
                return t is CoreInstance;
            if (effectSource != null && effectSource.CurrentEffect.Contains("sleep") && t is CardInstance ti && ti.IsAsleep)
                return false;            
            return true; // Any
        }).ToList();
        
        if (targets.Count == 0)
            return null;

        // Simple AI: UnityEngine.Random valid target
        IAttackable choice = targets[UnityEngine.Random.Range(0, targets.Count)];
        Debug.Log("Enemy triggered effect on " + choice.ToString() + " ");
        return choice;
    }

    public IAttackable ChooseRandomEffectTarget(PlayerOwner targetOwner, EffectTarget type, bool canTargetCore = true, bool excludeSleepingUnits = false)
    {
        List<IAttackable> targets = GetValidTargets(targetOwner);

        targets = targets.Where(t =>
        {
            if (!canTargetCore && t is CoreInstance)
                return false;

            if (excludeSleepingUnits && t is CardInstance ci && ci.IsAsleep)
                return false;

            if (type == EffectTarget.Unit)
                return t is CardInstance;

            if (type == EffectTarget.Core)
                return t is CoreInstance;

            return true;
        }).ToList();

        if (targets.Count == 0)
            return null;

        return targets[UnityEngine.Random.Range(0, targets.Count)];
    }
    public void HandleTargetClick(IAttackable target)
    {
        if (!isTargetingEffect || effectSource == null)
            return;

        if (!IsValidEffectTarget(effectSource.Owner, target, targetType))
            return;

        isTargetingEffect = false;
        attackCursor.gameObject.SetActive(false);

        onEffectTargetChosen?.Invoke(target);

        onEffectTargetChosen = null;
        effectSource = null;
    }

    public IEnumerator ShowEnemySpell(CardData data)
    {
        enemySpellReveal.SetActive(true);

        // Create preview card
        CardInstance preview =
            CardFactory.Instance.CreateCardInPosition(
                data,
                PlayerOwner.Enemy,Vector3.zero,Vector3.one,
                enemySpellAnchor.transform
            );
        preview.GetComponent<SortingGroup>().sortingOrder = 500;
        preview.SetZone(CardZone.Board);
        preview.GetComponent<CardView>().UpdateMode();
        preview.IsDisplay = true; preview.GetComponent<Collider2D>().enabled = false;

        yield return new WaitForSeconds(1.0f);

        Destroy(preview.gameObject);
        enemySpellReveal.SetActive(false);
    }

    #endregion
}
public class AttackRequest
{
    public CardInstance Attacker;
    public IAttackable Target;
    public Transform TargetTransform;

    public AttackRequest(CardInstance attacker, IAttackable target)
    {
        Attacker = attacker;
        Target = target;
        TargetTransform = target != null ? target.Transform : null;
    }
}
