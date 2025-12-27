using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
public interface IAttackable
{
    PlayerOwner Owner { get; }
    int CurrentAttack { get; }

    void TakeDamage(int amount);
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

    [Header("Deck and Board")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private AllyCardDropArea allyDropArea;
    [SerializeField] private EnemyCardDropArea enemyDropArea;


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


    [Header("References")]
    [SerializeField] private AllyCardDropArea allyBoard;
    [SerializeField] private EnemyCardDropArea enemyBoard;

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
        //Start turn logic
        TurnManager.Instance.OnTurnStarted += HandleTurnStart;
        TurnManager.Instance.StartFirstTurn();
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
                progression.Register();
                activeProgressions.Add(progression);
            }
        }
    }
    private void OnAllyTraitActivated(CardData.Trait trait, int tier)
    {
        allyTraitUI.ActivateTrait(trait, tier);
    }

    private void OnEnemyTraitActivated(CardData.Trait trait, int tier)
    {
        enemyTraitUI.ActivateTrait(trait, tier);
    }

    private void SetupCores()
    {
        //PlayerCore = Instantiate(corePrefab, spawnPlayerCore.transform).GetComponent<CoreInstance>();
        PlayerCore.Initialize(PlayerOwner.Player, startingCoreHealth);

        //EnemyCore = Instantiate(corePrefab, spawnEnemyCore.transform).GetComponent<CoreInstance>();
        EnemyCore.Initialize(PlayerOwner.Enemy, startingCoreHealth);
    }
    public void OnCoreDestroyed(PlayerOwner owner)
    {
        if (owner == PlayerOwner.Player)
            Debug.Log("PLAYER LOSES");
        else
            Debug.Log("PLAYER WINS");
    }
    private void HandleTurnStart(PlayerOwner owner)
    {
        IncreaseMaxMana(owner);
        RefillMana(owner);
    }

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
    public void BeginAttack(Card attacker)
    {
        CardInstance attackerInst = attacker.GetComponentInChildren<CardInstance>();
        if (attackerInst.Data.cardType.ToLower() == "spell") return;
        if (attackerInst.CurrentZone != CardZone.Board) return;
        if (!TurnManager.Instance.IsPlayerTurn(attackerInst.Owner))
            return;
        if (attackerInst.HasAttackedThisTurn)
            return;
        if (attackerInst.IsSummoningSick)
            return;
        isTargettingAttack = true;
        currentAttacker = attacker;

        attackCursor.gameObject.SetActive(true);

        Debug.Log(attacker + " started attack");
    }
    public void ResolveAttack(Card target)
    {
        CardInstance targetInst = target.GetComponentInChildren<CardInstance>();
        CardInstance attackerInst = currentAttacker.GetComponentInChildren<CardInstance>();

        //Verify Legality of attack
        if (targetInst.Owner == attackerInst.Owner) return;
        if (targetInst.Data.cardType.ToLower() == "spell") return;
        if (targetInst.CurrentZone != CardZone.Board) return;

        Debug.Log(currentAttacker + " attacked " + target);
        attackCursor.gameObject.SetActive(false);

        //Attack Logic :
        int attackerDmg = attackerInst.CurrentAttack;
        int targetDmg = targetInst.CurrentAttack;
        attackerInst.HasAttackedThisTurn = true;
        attackerInst.TakeDamage(targetDmg);
        targetInst.TakeDamage(attackerDmg);

        //Reset Attack process
        isTargettingAttack = false;
        currentAttacker = null;
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
    public void ResolveAttack(CardInstance attacker, IAttackable target)
    {
        // Final safety checks
        if (attacker == null || target == null)
            return;

        if (!CanSelectAttacker(attacker))
            return;

        // Prevent friendly fire
        if (attacker.Owner == target.Owner)
            return;

        CardInstance attackerInst = attacker.GetComponentInChildren<CardInstance>();

        int attackerDmg = attackerInst.CurrentAttack;
        int targetDmg = target.CurrentAttack;
        attackerInst.HasAttackedThisTurn = true;
        attackerInst.TakeDamage(targetDmg);

        // Deal damage
        target.TakeDamage(attackerDmg);

        // Mark attacker as having attacked
        attacker.HasAttackedThisTurn = true;
    }

    public List<IAttackable> GetValidTargets(CardInstance attacker)
    {
        List<IAttackable> targets = new();

        var defendingBoard = GetBoardForOwner(
            attacker.Owner == PlayerOwner.Player
                ? PlayerOwner.Enemy
                : PlayerOwner.Player
        );

        bool hasProtect = defendingBoard.HasProtectUnits();

        foreach (var go in defendingBoard.GetCards())
        {
            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci == null)
                continue;

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
    private void ResolveAttackOnCore(CardInstance attacker, CoreInstance core)
    {
        int damage = attacker.CurrentAttack;

        core.TakeDamage(damage);

        Debug.Log($"{attacker.name} hits {core.Owner} core for {damage}");
    }
    public void HandleBoardCardClick(Card card)
    {
        if (!isTargettingAttack)
            BeginAttack(card);
        else if (CanAttackUnit(card.GetComponent<CardInstance>()))
            ResolveAttack(card);
    }
    public bool CanAttackUnit(CardInstance target)
    {
        // Basic checks (turn, owner, already attacked, etc.)

        if (target.Owner == PlayerOwner.Player)
        {
            if (!target.HasKeyword("protect") && FindFirstObjectByType<AllyCardDropArea>().HasProtectUnits())
                return false;
            else return true;
        }
        else
        {
            if (!target.HasKeyword("protect") && FindFirstObjectByType<EnemyCardDropArea>().HasProtectUnits())
                return false;
            else return true;
        }
    }
    public void TryAttackCore(CoreInstance targetCore)
    {
        if (currentAttacker == null)
            return;

        if (currentAttacker.GetComponent<CardInstance>().Owner == targetCore.Owner)
            return;

        if (currentAttacker.GetComponent<CardInstance>().Owner == PlayerOwner.Player && FindFirstObjectByType<EnemyCardDropArea>().HasProtectUnits())
        {
            return;
        }
        else if (currentAttacker.GetComponent<CardInstance>().Owner == PlayerOwner.Enemy && FindFirstObjectByType<AllyCardDropArea>().HasProtectUnits())
        {
            return;
        }
        ResolveAttackOnCore(currentAttacker.GetComponent<CardInstance>(), targetCore);

        currentAttacker.GetComponent<CardInstance>().HasAttackedThisTurn = true;
        attackCursor.gameObject.SetActive(false);
        currentAttacker = null; isTargettingAttack = false;
    }
    #endregion
}
