using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

public interface IAttackable
{
    Transform Transform{ get; } 
    PlayerOwner Owner { get; }
    int CurrentAttack { get; }

    void TakeDamage(int amount);
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
    [SerializeField] private AllyCardDropArea allyDropArea;
    [SerializeField] private EnemyCardDropArea enemyDropArea;

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
    public bool isTargettingAttack;
    Card currentAttacker;
    [Header("Trait Systems")]
    [SerializeField] private TraitSystem allyTraitSystem;
    [SerializeField] private TraitSystem enemyTraitSystem;
    [SerializeField] private TraitUIManager allyTraitUI;
    [SerializeField] private TraitUIManager enemyTraitUI;

    private readonly List<ITraitProgression> activeProgressions = new();
    [SerializeField] private WinLoseUI winLoseUI;
    public bool IsCombatAnimating { get; private set; }
    private readonly Queue<AttackRequest> attackQueue = new Queue<AttackRequest>();
    private bool isResolvingAttack = false;
    [Header("Camera Shake")]
    [SerializeField] private Camera mainCamera;

    private Vector3 cameraBasePos;
    private Tween cameraShakeTween;
    private bool isTargetingEffect;
    private System.Action<IAttackable> onEffectTargetChosen;
    private CardInstance effectSource;

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
                CardData.Trait.Speedster => throw new System.NotImplementedException(),
                CardData.Trait.Gunner => throw new System.NotImplementedException(),
                CardData.Trait.Inazuma => throw new System.NotImplementedException(),
                CardData.Trait.Pokemon => throw new System.NotImplementedException(),
                CardData.Trait.Blizzard => throw new System.NotImplementedException(),
                CardData.Trait.Workout => throw new System.NotImplementedException(),
                CardData.Trait.Faith => throw new System.NotImplementedException(),
                CardData.Trait.Ritual => throw new System.NotImplementedException(),
                CardData.Trait.Hater => throw new System.NotImplementedException(),
                CardData.Trait.SpellFocus => throw new System.NotImplementedException(),
                CardData.Trait.Combo => throw new System.NotImplementedException(),
                CardData.Trait.Healer => throw new System.NotImplementedException(),
                CardData.Trait.Meme => throw new System.NotImplementedException()
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

    #region Combat Manager
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

        if (attackerInst.HasAttackedThisTurn || attackerInst.IsSummoningSick)
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

        if (attacker.HasAttackedThisTurn)
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
        List<CardInstance> aliveUnits = new();

        foreach (var go in defendingBoard.GetCards())
        {
            if (go == null || !go.activeSelf)
                continue;

            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci == null || ci.IsDead)
                continue;

            aliveUnits.Add(ci);
        }

        bool hasProtect = aliveUnits.Exists(ci => ci.HasKeyword("protect"));

        foreach (var ci in aliveUnits)
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
        List<CardInstance> aliveUnits = new();

        foreach (var go in defendingBoard.GetCards())
        {
            if (go == null || !go.activeSelf)
                continue;

            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci == null || ci.IsDead)
                continue;

            aliveUnits.Add(ci);
        }

        bool hasProtect = aliveUnits.Exists(ci => ci.HasKeyword("protect"));

        foreach (var ci in aliveUnits)
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
    public void ResolveAttack(CardInstance attacker, IAttackable target)
    {
        if (attacker == null || target == null)
            return;

        if (!CanSelectAttacker(attacker))
            return;

        if (attacker.Owner == target.Owner)
            return;

        attacker.HasAttackedThisTurn = true;
        if (attacker.Owner == PlayerOwner.Player)
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
            foreach (IAttackable targets in GetValidTargets(attacker))
            {
                if (targets is CardInstance ci)
                {
                    ci.GetComponent<CardView>()
                        .SetGlow(CardView.CardGlowState.CanBeTargeted);
                }
            }
        }

        // UNIT vs UNIT
        if (target is CardInstance targetUnit)
        {
            int attackerDmg = attacker.CurrentAttack;
            int defenderDmg = targetUnit.CurrentAttack;

            attacker.TakeDamage(defenderDmg);
            targetUnit.TakeDamage(attackerDmg);
            return;
        }

        // UNIT vs CORE
        if (target is CoreInstance core)
        {
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

        if (CanAttackUnit(clickedInst))
        {
            QueueAttack(attackerInst, clickedInst);
        }
    }
    public bool CanAttackUnit(CardInstance target)
    {
        // Basic checks (turn, owner, already attacked, etc.)

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

        if (attacker.HasAttackedThisTurn || attacker.IsSummoningSick)
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
                req.Attacker.HasAttackedThisTurn)
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
    }
    public void BeginEffectTargeting(
    CardInstance source,
    PlayerOwner owner,
    System.Action<IAttackable> onTargetChosen)
    {
        isTargetingEffect = true;
        effectSource = source;
        onEffectTargetChosen = onTargetChosen;

        attackCursor.gameObject.SetActive(true);
    }
    private bool IsValidEffectTarget(PlayerOwner owner, IAttackable target)
    {
        // Example rules:
        // - enemy units
        // - enemy core
        return target.Owner != owner;
    }
    public void HandleTargetClick(IAttackable target)
    {
        if (!isTargetingEffect)
            return;

        if (!IsValidEffectTarget(effectSource.Owner, target))
            return;

        isTargetingEffect = false;

        attackCursor.gameObject.SetActive(false);

        onEffectTargetChosen?.Invoke(target);

        onEffectTargetChosen = null;
        effectSource = null;
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


