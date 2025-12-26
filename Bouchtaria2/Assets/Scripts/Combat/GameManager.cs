using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public int CurrentMana { get; private set; } = 10;
    public int CurrentMaxMana { get; private set; } = 10;
    [SerializeField] TextMeshProUGUI manacounter;
    [SerializeField] Image attackCursor;
    public bool isTargettingAttack;
    void Start()
    {
        isTargettingAttack = false;
    }

    // Update is called once per frame
    void Update()
    {
        manacounter.text = $"{CurrentMana}/{CurrentMaxMana}";
        attackCursor.transform.position = Input.mousePosition;
    }

    public void RefreshMaxMana()
    {
        CurrentMana = CurrentMaxMana;
    }
    public void UseMana(int mana)
    {
        CurrentMana -= mana;
    }
    Card currentAttacker;

    public void BeginAttack(Card attacker)
    {
        CardInstance attackerInst = currentAttacker.GetComponentInChildren<CardInstance>();
        if (attackerInst.Data.cardType.ToLower() == "spell") return;
        if (attackerInst.CurrentZone != CardZone.Board) return;
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
        attackerInst.TakeDamage(targetDmg);
        targetInst.TakeDamage(attackerDmg);

        //Reset Attack process
        isTargettingAttack = false;
        currentAttacker = null;
    }
    public void HandleBoardCardClick(Card card)
    {
        if (!isTargettingAttack)
            BeginAttack(card);
        else
            ResolveAttack(card);
    }

}
