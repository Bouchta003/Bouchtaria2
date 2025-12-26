using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public CoreInstance PlayerCore;
    public CoreInstance EnemyCore;

    [SerializeField] private GameObject corePrefab;
    [SerializeField] private int startingCoreHealth = 20;
    [SerializeField] private GameObject spawnPlayerCore;
    [SerializeField] private GameObject spawnEnemyCore;
    public int CurrentMana { get; private set; } = 10;
    public int CurrentMaxMana { get; private set; } = 10;
    [SerializeField] TextMeshProUGUI manacounter;
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
        SetupCores(); 
        PlayerCore.GetComponent<CoreView>().Bind(PlayerCore);
        EnemyCore.GetComponent<CoreView>().Bind(EnemyCore);

        TurnManager.Instance.OnTurnStarted += HandleTurnStart;

        TurnManager.Instance.StartFirstTurn();
    }
    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted -= HandleTurnStart;
    }
    public void OnCoreDestroyed(PlayerOwner owner)
    {
        if (owner == PlayerOwner.Player)
            Debug.Log("PLAYER LOSES");
        else
            Debug.Log("PLAYER WINS");
    }
    // Update is called once per frame
    void Update()
    {
        manacounter.text = $"{CurrentMana}/{CurrentMaxMana}";
        attackCursor.transform.position = Input.mousePosition;
    }
    private void SetupCores()
    {
        //PlayerCore = Instantiate(corePrefab, spawnPlayerCore.transform).GetComponent<CoreInstance>();
        PlayerCore.Initialize(PlayerOwner.Player, startingCoreHealth);

        //EnemyCore = Instantiate(corePrefab, spawnEnemyCore.transform).GetComponent<CoreInstance>();
        EnemyCore.Initialize(PlayerOwner.Enemy, startingCoreHealth);
    }

    private void HandleTurnStart(PlayerOwner owner)
    {
        if (owner != PlayerOwner.Player)
            return;

        RefreshMaxMana();
    }
    public void RefreshMaxMana()
    {
        CurrentMana = CurrentMaxMana;
    }
    public void UseMana(int mana)
    {
        CurrentMana -= mana;
    }

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

}
