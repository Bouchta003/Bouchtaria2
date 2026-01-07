using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;
using System.Linq;
using UnityEngine.Rendering;

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
    [SerializeField] private int startingCoreHealth = 20;
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

    [SerializeField] TextMeshProUGUI manacounterAlly;
    [SerializeField] TextMeshProUGUI manacounterEnmy;

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
    [Header("Camera Shake")]
    [SerializeField] private Camera mainCamera;

    public int PlayerHealBonus = 0;
    public int EnemyHealBonus = 0;
    public bool PlayerDarkHeal = false;
    public bool EnemyDarkHeal = false;
    //Animation
    public bool IsCombatAnimating { get; private set; }
    //Trait logic
    private readonly List<ITraitProgression> activeProgressions = new();
    //Attack logic
    private readonly Queue<AttackRequest> attackQueue = new Queue<AttackRequest>();
    private bool isResolvingAttack = false;
    public bool isTargettingAttack;
    Card currentAttacker;
    public event System.Action<CardInstance> OnCardKilled;
    public event System.Action<PlayerOwner, int> OnOwnerHeal;

    //Camera shake
    private Vector3 cameraBasePos;
    private Tween cameraShakeTween;
    //Target effects
    private bool isTargetingEffect;
    private System.Action<IAttackable> onEffectTargetChosen;
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
                CardData.Trait.Gunner => throw new System.NotImplementedException(),
                CardData.Trait.Inazuma => throw new System.NotImplementedException(),
                CardData.Trait.Speedster => throw new System.NotImplementedException(),
                CardData.Trait.Blizzard => throw new System.NotImplementedException(),
                CardData.Trait.Workout => throw new System.NotImplementedException(),
                CardData.Trait.Faith => throw new System.NotImplementedException(),
                CardData.Trait.Ritual => throw new System.NotImplementedException(),
                CardData.Trait.Hater => throw new System.NotImplementedException(),
                CardData.Trait.SpellFocus => throw new System.NotImplementedException(),
                CardData.Trait.Combo => throw new System.NotImplementedException(),
                CardData.Trait.Healer => new HealerProgression(owner, maxTier, traitSystem, this),
                CardData.Trait.Meme => throw new System.NotImplementedException(),
                _ => throw new System.NotImplementedException()
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
        }
        else
        {
            CurrentGameState = GameState.PlayerWon;
            Debug.Log("PLAYER WINS");
        }

        EndGame();
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
    public void TrySummonForOwner(PlayerOwner owner, int cardId)
    {
        var board = GetBoardForOwner(owner);

        if (board.IsFull())
            return;

        CardData data = CardDatabase.Instance.GetCardById(cardId);
        if (data == null)
            return;

        ICardDropArea parent =
    owner == PlayerOwner.Player
        ? allyDropArea
        : enemyDropArea;

        CardInstance cardInst =
            CardFactory.Instance.CreateCard(data, owner, parent.CardContainer);

        if (owner == PlayerOwner.Player) {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 1;
            cardInst.SetZone(CardZone.Board);
            allyDropArea.AddSummonedCard(cardInst);
            allyDropArea.UpdateAllyCardPositions();
        }
        else
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 3;
            cardInst.SetZone(CardZone.Board);
            //put sorting order to 1 if necessary and limit board size
            enemyDropArea.AddSummonedCard(cardInst);
            enemyDropArea.UpdateEnemyCardPositions();
            Debug.Log(
              $"[ENEMY SUMMON] pos={cardInst.transform.position} " +
              $"localPos={cardInst.transform.localPosition} " +
              $"zone={cardInst.CurrentZone}"
            );

        }
    }
    public void TrySummonForOther(PlayerOwner owner, int cardId)
    {
        var board = GetBoardForOther(owner);

        if (board.IsFull())
            return;

        CardData data = CardDatabase.Instance.GetCardById(cardId);
        if (data == null)
            return;

        CardInstance cardInst =
            CardFactory.Instance.CreateCard(data, owner);

        if (owner == PlayerOwner.Player)
        {
            cardInst.transform.parent = enemyHand.transform;
            cardInst.GetComponent<SortingGroup>().sortingOrder =2;
            enemyDropArea.AddSummonedCard(cardInst);
        }
        else
        {
            cardInst.transform.parent = allyHand.transform;
            cardInst.GetComponent<SortingGroup>().sortingOrder =2;
            allyDropArea.AddSummonedCard(cardInst);
        }
    }
    public void Discover(int id1, int id2, int id3, PlayerOwner owner)
    {
        if(owner == PlayerOwner.Enemy)
        {
            int randint = Random.Range(0, 3);
            if (randint == 0) AddCardToHand(PlayerOwner.Enemy, id1);
            if (randint == 1) AddCardToHand(PlayerOwner.Enemy, id2);
            if (randint == 2) AddCardToHand(PlayerOwner.Enemy, id3);
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
            AddCardToHand(PlayerOwner.Enemy, options[Random.Range(0, options.Count)].id);
            return;
        }
        isDiscovering = true;
        discoverDisplay.SetActive(true);
        CardData data1 = options[Random.Range(0, options.Count)];
        CardInstance dataInst1 = CardFactory.Instance.CreateCardInPosition(data1, PlayerOwner.Player, Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst1.IsDisplay = true;
        dataInst1.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data1);
        CardData data2 = options[Random.Range(0, options.Count)];
        CardInstance dataInst2 = CardFactory.Instance.CreateCardInPosition(data2, PlayerOwner.Player, new Vector3(5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst2.IsDisplay = true;
        dataInst2.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data2);
        CardData data3 = options[Random.Range(0, options.Count)];
        CardInstance dataInst3 = CardFactory.Instance.CreateCardInPosition(data3, PlayerOwner.Player, new Vector3(-5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst3.IsDisplay = true;
        dataInst3.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data3);
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
    #endregion

    #region Combat Manager
    public void NotifyCardKilled(CardInstance deadCard)
    {
        OnCardKilled?.Invoke(deadCard);
    }
    public void NotifyHealed(PlayerOwner owner, int amount)
    {
        OnOwnerHeal?.Invoke(owner, amount);
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
        GainMana(card.CurrentManaCost, PlayerOwner.Player);

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

        if ((attackerInst.HasAttackedThisTurn && !attackerInst.HasKeyword("haste"))||
            (attackerInst.HasAttackedThisTurn && attackerInst.HasKeyword("haste") && attackerInst.HasAttackedTwiceThisTurn) || attackerInst.IsSummoningSick)
            return;

        // 🔑 Cancel previous selection safely
        currentAttacker = attacker;
        isTargettingAttack = true;
        attackCursor.gameObject.SetActive(true);
    }
    public bool CanSelectAttacker(CardInstance attacker)
    {
        if (!TurnManager.Instance.IsPlayerTurn(attacker.Owner))
            return false;

        if ((attacker.HasAttackedThisTurn && !attacker.HasKeyword("haste")) ||
            (attacker.HasAttackedThisTurn && attacker.HasKeyword("haste") && attacker.HasAttackedTwiceThisTurn))
            return false;

        if (attacker.IsSummoningSick)
            return false;

        return true;
    }
    private ICardDropArea GetBoardForOwner(PlayerOwner owner)
    {
        return owner == PlayerOwner.Player
            ? allyDropArea
            : enemyDropArea;
    }
    private ICardDropArea GetBoardForOther(PlayerOwner owner)
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

        // UNIT vs UNIT
        if (target is CardInstance targetUnit)
        {
            bool isKill = false;
            int attackerDmg = attacker.CurrentAttack;
            int defenderDmg = targetUnit.CurrentAttack;
            if (attackerDmg >= targetUnit.CurrentHealth && !targetUnit.HasKeyword("blessed")) isKill = true;
            int thornDamage = 0;
            attacker.TriggerStrike();
            if (targetUnit.HasKeyword("thorns"))
            {
                thornDamage = targetUnit.ThornsDamage;
            }
            if (attacker.HasKeyword("bleed"))
            {
                targetUnit.IsBleeding = true;
                targetUnit.GetComponent<CardView>().UpdateMode();
            }
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
                OnCardKilled?.Invoke(attacker);
            }

            attacker.TakeDamage(defenderDmg+thornDamage);
            targetUnit.TakeDamage(attackerDmg);
            
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
        if (!isTargettingAttack)
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

        if (GetValidTargets(attackerInst).Contains(clickedInst))
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

        if ((attacker.HasAttackedThisTurn && !attacker.HasKeyword("haste")) || 
            (attacker.HasAttackedThisTurn && attacker.HasKeyword("haste") && attacker.HasAttackedTwiceThisTurn) || 
            attacker.IsSummoningSick)
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
    public void BeginEffectTargeting(    CardInstance source,    PlayerOwner owner,    System.Action<IAttackable> onTargetChosen, EffectTarget effectTargetType)
    {
        isTargetingEffect = true;
        effectSource = source;
        effectOwner = owner;
        onEffectTargetChosen = onTargetChosen;
        targetType = effectTargetType;

        attackCursor.gameObject.SetActive(true);
    }
    private bool IsValidEffectTarget(PlayerOwner owner, IAttackable target, EffectTarget effectTargetType)
    {
        //if (target.Owner == owner) return false;
        if ((target is CoreTarget || target is CoreInstance) && effectTargetType == EffectTarget.Unit)
            return false;
        if ((target is CardInstance) && effectTargetType == EffectTarget.Core)
            return false;

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
            if (type == EffectTarget.Unit)
                return t is CardInstance;
            if (type == EffectTarget.Core)
                return t is CoreInstance;
            return true; // Any
        }).ToList();

        if (targets.Count == 0)
            return null;

        // Simple AI: random valid target
        IAttackable choice = targets[Random.Range(0, targets.Count)];
        Debug.Log("Enemy triggered effect on " + choice.ToString() + " ");
        return choice;
    }
    public void HandleTargetClick(IAttackable target)
    {
        if (!isTargetingEffect || effectSource == null)
            return;

        if (!isTargetingEffect)
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


