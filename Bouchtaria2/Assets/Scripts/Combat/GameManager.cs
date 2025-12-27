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
    void Start()
    {
        isTargettingAttack = false;

        if (TurnManager.Instance == null)
        {
            Debug.LogError("TurnManager missing!");
            return;
        }

        //Setup cores mana and deck before the turn logic
        SetupCores(); 
        PlayerCore.GetComponent<CoreView>().Bind(PlayerCore);
        EnemyCore.GetComponent<CoreView>().Bind(EnemyCore);
        deckManager.InitializeDecks();
        InitializeMana();

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
        attackerInst.HasAttackedThisTurn=true;
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

        Debug.Log(
            $"{attacker.name} ({attacker.Owner}) attacks " +
            $"{target.GetType().Name} ({target.Owner}) for {attackerDmg}"
        );
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
            Debug.Log("Possible Target :" + ci.name);
            targets.Add(ci);
        }

        if (!hasProtect)
        {
            CoreInstance core = GetCoreForOwner(defendingBoard.Owner);
            targets.Add(core);
            Debug.Log("Possible Target : Core" );
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
        else if(CanAttackUnit(card.GetComponent<CardInstance>()))
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
