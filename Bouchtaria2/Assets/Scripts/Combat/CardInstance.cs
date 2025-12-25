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

public class CardInstance : MonoBehaviour
{
    // Immutable reference
    public CardData Data { get; private set; }

    // Runtime state
    public int CurrentAttack { get; private set; }
    public int CurrentManaCost { get; private set; }
    public int CurrentHealth { get; private set; }

    public PlayerOwner Owner { get; private set; }
    public CardZone CurrentZone { get; private set; }

    public bool HasAttackedThisTurn { get; private set; }
    public bool IsSummoningSick { get; private set; }

    // -------------------------
    // Initialization (called by CardFactory ONLY)
    // -------------------------
    public void Initialize(CardData data, PlayerOwner owner)
    {
        Data = data;
        Owner = owner;

        CurrentManaCost = data.manaCost;
        CurrentAttack = data.atkValue;
        CurrentHealth = data.hpValue;

        CurrentZone = CardZone.Deck;

        HasAttackedThisTurn = false;
        IsSummoningSick = true;
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
    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        SetZone(CardZone.Graveyard);
        gameObject.SetActive(false); // or send to graveyard visual
    }
}
