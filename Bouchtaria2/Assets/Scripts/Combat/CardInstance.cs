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
    public int CurrentManaCost { get; private set; }
    public int CurrentHealth { get; private set; }

    public PlayerOwner Owner { get; set; }
    public CardZone CurrentZone { get; private set; }

    public bool HasAttackedThisTurn { get; set; }
    public bool IsSummoningSick { get; set; }

    // -------------------------
    // Initialization (called by CardFactory ONLY)
    // -------------------------
    public void Initialize(CardData data, PlayerOwner owner)
    {
        Data = data;
        Owner = owner;
        view = gameObject.GetComponent<CardView>();
        CurrentManaCost = data.manaCost;
        CurrentAttack = data.atkValue;
        CurrentHealth = data.hpValue;

        CurrentZone = CardZone.Deck;

        HasAttackedThisTurn = false;
        IsSummoningSick = true;
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

                TriggerRequiem();
            }
            else {
                EnemyCardDropArea board =
                 FindFirstObjectByType<EnemyCardDropArea>();

                if (board != null)
                    board.HandleEnemyDeath(this);
            }
        }
    }

}
