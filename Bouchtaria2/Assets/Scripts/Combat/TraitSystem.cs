using System.Collections.Generic;
using UnityEngine;
public interface IDeckTraitEffect
{
    CardData.Trait Trait { get; }
    int Tier { get; }

    void OnRegister();
    void OnUnregister();
}
public interface ITraitProgression
{
    CardData.Trait Trait { get; }
    PlayerOwner Owner { get; }
    int CurrentTier { get; }

    void Register();
    void Unregister();
}

public class TraitSystem : MonoBehaviour
{
    public event System.Action<CardData.Trait, int> OnTraitTierActivated;

    [SerializeField] GameObject traitLayout;
    public PlayerOwner Owner { get; private set; }

    private readonly List<IDeckTraitEffect> activeEffects = new();

    public void Initialize(PlayerOwner owner)
    {
        Owner = owner;
    }
    public void ActivateEffect(IDeckTraitEffect effect)
    {
        effect.OnRegister();
        activeEffects.Add(effect);

        OnTraitTierActivated?.Invoke(effect.Trait, effect.Tier);
    }

    public void ClearAll()
    {
        foreach (var effect in activeEffects)
            effect.OnUnregister();

        activeEffects.Clear();
    }
}
#region Neutral Trait
public class NeutralProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Neutral;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int neutralPlayed;

    private readonly TraitSystem traitSystem;
    private readonly AllyCardDropArea allyBoard;
    private readonly EnemyCardDropArea enemyBoard;

    public NeutralProgression(
        PlayerOwner owner,
        int maxTier,
        TraitSystem traitSystem,
        AllyCardDropArea allyBoard,
        EnemyCardDropArea enemyBoard)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
        this.allyBoard = allyBoard;
        this.enemyBoard = enemyBoard;
    }
    public void Register()
    {
        Debug.Log($"[NeutralProgression] Register for {Owner}");

        allyBoard.OnCardPlayed += OnCardPlayed;
        enemyBoard.OnCardPlayed += OnCardPlayed;
    }
    public void Unregister()
    {
        Debug.Log($"[NeutralProgression] Unregister for {Owner}");

        allyBoard.OnCardPlayed -= OnCardPlayed;
        enemyBoard.OnCardPlayed -= OnCardPlayed;
    }

    private void OnCardPlayed(CardInstance card)
    {
            if (card.Owner != Owner)
            return;

        if (!card.HasTrait("neutral"))
            return;
        neutralPlayed++;
        //Debug.Log($"Neutral card played for :{Owner}, count is now {neutralPlayed}");

        if (neutralPlayed >= 5 && CurrentTier < 1 && maxTier >= 1)
        {
            UnlockTier1();
        }

        if (neutralPlayed >= 10 && CurrentTier < 2 && maxTier >= 2)
        {
            UnlockTier2();
        }

    }

    private void UnlockTier1()
    {
        CurrentTier = 1;

        traitSystem.ActivateEffect(
            new NeutralTier1Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked Neutral Tier 1");
    }
    private void UnlockTier2()
    {
        CurrentTier = 2;
        DeckManager deckManager = Object.FindFirstObjectByType<DeckManager>();
        traitSystem.ActivateEffect(
            new NeutralTier2Effect(Owner, deckManager)
        );

        Debug.Log($"{Owner} unlocked Neutral Tier 2");
    }

}
public class NeutralTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Neutral;
    public int Tier => 1;

    private readonly PlayerOwner owner;
    private bool used;

    public NeutralTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }
    public void OnRegister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed += OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed += OnCardPlayed;

        TurnManager.Instance.OnTurnStarted += OnTurnStarted;
    }
    public void OnUnregister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed -= OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed -= OnCardPlayed;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted -= OnTurnStarted;
    }

    private void OnTurnStarted(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        used = false;
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (used)
            return;

        if (card.Owner != owner)
            return;

        if (card.Data.cardType!="minion")
            return;
        Debug.Log($"Buffing first card {card.name}, for {owner}");
        card.ModifyStats(0,1);
        used = true;
    }
}
public class NeutralTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Neutral;
    public int Tier => 2;

    private readonly PlayerOwner owner;
    private bool usedThisTurn;

    private readonly DeckManager deckManager;

    public NeutralTier2Effect(PlayerOwner owner, DeckManager deckManager)
    {
        this.owner = owner;
        this.deckManager = deckManager;
    }

    public void OnRegister()
    {
        deckManager.OnCardDrawn += OnCardDrawn;
        TurnManager.Instance.OnTurnStarted += OnTurnStarted;
        TurnManager.Instance.OnTurnEnded += OnTurnEnded;
    }

    public void OnUnregister()
    {
        deckManager.OnCardDrawn -= OnCardDrawn;

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted -= OnTurnStarted;
            TurnManager.Instance.OnTurnEnded -= OnTurnEnded;
        }
    }

    private void OnTurnStarted(PlayerOwner turnOwner)
    {
        if (turnOwner == owner)
            usedThisTurn = false;
    }

    private void OnTurnEnded(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        // Clear temporary cost reductions at end of turn
        foreach (var card in GetHand(owner))
        {
            card.ClearTemporaryManaModifiers();
        }
    }

    private void OnCardDrawn(CardInstance card)
    {
        if (usedThisTurn)
            return;

        if (card.Owner != owner)
            return;

        if (card.CurrentManaCost < 2)
            return;

        card.AddTemporaryManaModifier(-1);
        usedThisTurn = true;

        Debug.Log($"[Neutral T2] Reduced cost of {card.name} for {owner}");
    }

    private IEnumerable<CardInstance> GetHand(PlayerOwner owner)
    {
        HandManager hand =
            owner == PlayerOwner.Player
                ? Object.FindFirstObjectByType<DeckManager>().handManager
                : Object.FindFirstObjectByType<DeckManager>().handManagerEnemy;

        foreach (var go in hand.handCards)
            yield return go.GetComponent<CardInstance>();
    }
}

#endregion
