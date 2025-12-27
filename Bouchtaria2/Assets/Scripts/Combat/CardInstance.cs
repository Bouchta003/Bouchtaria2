using System;
using System.Linq;
using UnityEngine;

public enum CardZone
{
    Deck,
    Hand,
    Board,
    Graveyard
}

public enum PlayerOwner
{
    Player,
    Enemy
}

public class CardInstance : MonoBehaviour, IAttackable
{
    // Immutable reference
    public CardData Data { get; private set; }
    CardView view;
    // Runtime state
    public int CurrentAttack { get; private set; }
    public int BaseManaCost { get; private set; }
    public int CurrentHealth { get; private set; }
    public int CurrentManaCost => Mathf.Max(0, BaseManaCost + temporaryManaModifier);

    public PlayerOwner Owner { get; set; }
    public CardZone CurrentZone { get; private set; }
    private int temporaryManaModifier = 0;
    public bool HasAttackedThisTurn { get; set; }
    public bool IsSummoningSick { get; set; }
    public void AddTemporaryManaModifier(int amount)
    {
        temporaryManaModifier += amount;
    }
    public void ClearTemporaryManaModifiers()
    {
        temporaryManaModifier = 0;
    }
    // -------------------------
    // Initialization (called by CardFactory ONLY)
    // -------------------------
    public void Initialize(CardData data, PlayerOwner owner)
    {
        Data = data;
        Owner = owner;
        view = gameObject.GetComponent<CardView>();
        BaseManaCost = data.manaCost;
        CurrentAttack = data.atkValue;
        CurrentHealth = data.hpValue;

        CurrentZone = CardZone.Deck;

        HasAttackedThisTurn = false;
        IsSummoningSick = true;
    }
    private void Update()
    {
        if (CurrentManaCost < BaseManaCost) this.GetComponent<CardView>().manaText.color = Color.green;
        if (CurrentManaCost > BaseManaCost) this.GetComponent<CardView>().manaText.color = Color.red;
        if (CurrentManaCost == BaseManaCost) this.GetComponent<CardView>().manaText.color = Color.white;
        this.GetComponent<CardView>().manaText.text = CurrentManaCost.ToString();
    }
    public bool HasTrait(string trait)
    {
        return Data.traits != null && Data.traits.Contains(trait, StringComparer.OrdinalIgnoreCase);
    }
    public bool HasKeyword(string keywordString)
    {
        if (Data == null || Data.effect == null)
            return false;

        if (Data.effect.Contains(keywordString))
        {
            return true;
        }

        return false;
    }
    // -------------------------
    // Zone management
    // -------------------------
    public void SetZone(CardZone newZone)
    {
        CurrentZone = newZone;

        if (newZone == CardZone.Board)
        {
            IsSummoningSick = true;
        }
    }

    // -------------------------
    // Turn logic
    // -------------------------
    public void OnTurnStart()
    {
        HasAttackedThisTurn = false;

        if (CurrentZone == CardZone.Board)
            IsSummoningSick = false;
    }

    // -------------------------
    // Combat helpers
    // -------------------------
    public void OnEnterBoard()
    {
        TriggerDeploy();
    }
    private void TriggerDeploy()
    {
        //add effect here

    }
    private void TriggerRequiem()
    {
        //add effect here
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        CurrentHealth -= amount;

        view.hpTextBoard.text = CurrentHealth.ToString();
        if (CurrentHealth < Data.hpValue) view.hpTextBoard.color = Color.red;
        if (CurrentHealth > Data.hpValue) view.hpTextBoard.color = Color.green;
        if (CurrentAttack > Data.atkValue) view.hpTextBoard.color = Color.green;

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }
    public void Die()
    {
        if (CurrentZone == CardZone.Board)
        {
            if (Owner == PlayerOwner.Player)
            {
                AllyCardDropArea board =
                    FindFirstObjectByType<AllyCardDropArea>();

                if (board != null)
                    board.HandleAllyDeath(this);
            }
            else {
                EnemyCardDropArea board =
                 FindFirstObjectByType<EnemyCardDropArea>();

                if (board != null)
                    board.HandleEnemyDeath(this);
            }
                TriggerRequiem();
        }
    }

    internal void ModifyStats(int atk, int hp)
    {
        CurrentAttack += atk;
        CurrentHealth+=hp;
        if (CurrentHealth < Data.hpValue) view.hpTextBoard.color = Color.red;
        if (CurrentHealth > Data.hpValue) view.hpTextBoard.color = Color.green;
        if (CurrentAttack > Data.atkValue) view.hpTextBoard.color = Color.green;
    }
}
