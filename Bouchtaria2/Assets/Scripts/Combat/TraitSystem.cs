using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    void PushInitialState();
    void ResetProgression();
    int CurrentProgress { get; }

    event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    void Register();
    void Unregister();
}

public class TraitSystem : MonoBehaviour
{
    public event System.Action<CardData.Trait, int> OnTraitTierActivated;

    public PlayerOwner Owner { get; private set; }

    public readonly List<IDeckTraitEffect> activeEffects = new();

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
    public void DeactivateLowerTiers(CardData.Trait trait, int tierToKeep)
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];

            if (effect.Trait == trait && effect.Tier < tierToKeep)
            {
                effect.OnUnregister();
                activeEffects.RemoveAt(i);
            }
        }
    }

    public bool HasTraitAtTier(CardData.Trait trait, int minTier)
    {
        foreach (var effect in activeEffects)
        {
            if (effect.Trait == trait && effect.Tier >= minTier)
                return true;
        }
        return false;
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
    private int neutralPlayed=0;

    public int CurrentProgress => neutralPlayed;

    public event System.Action<CardData.Trait, int,int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;
    private readonly AllyCardDropArea allyBoard;
    private readonly EnemyCardDropArea enemyBoard;

    public NeutralProgression(PlayerOwner owner,int maxTier,TraitSystem traitSystem,AllyCardDropArea allyBoard,EnemyCardDropArea enemyBoard)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
        this.allyBoard = allyBoard;
        this.enemyBoard = enemyBoard;
    }
    public void ResetProgression()
    {
        neutralPlayed = 0;
    }
    public void PushInitialState()
    {
        int cap = GetCurrentCap();
        OnProgressUpdated?.Invoke(Trait, neutralPlayed, cap, Owner);
    }

    public void Register()
    {
        Debug.Log($"[NeutralProgression] Register for {Owner}");

        allyBoard.OnCardPlayed += OnCardPlayed;
        enemyBoard.OnCardPlayed += OnCardPlayed;

        OnProgressUpdated?.Invoke(Trait, neutralPlayed, GetCurrentCap(), Owner);
    }
    public void Unregister()
    {
        Debug.Log($"[NeutralProgression] Unregister for {Owner}");

        allyBoard.OnCardPlayed -= OnCardPlayed;
        enemyBoard.OnCardPlayed -= OnCardPlayed;
    }
    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 5,
            1 => 10,
            2 => 15,
            _ => 999
        };
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (card.Owner != Owner)
            return;

        if (!card.HasTrait("neutral"))
            return;
        neutralPlayed++;
        OnProgressUpdated?.Invoke(Trait, neutralPlayed, GetCurrentCap(), Owner);

        if (neutralPlayed >= 5 && CurrentTier < 1 && maxTier >= 1)
        {
            UnlockTier1();
        }

        if (neutralPlayed >= 10 && CurrentTier < 2 && maxTier >= 2)
        {
            UnlockTier2();
        }

        if (neutralPlayed >= 15 && CurrentTier < 3 && maxTier >= 3)
        {
            UnlockTier3();
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
    private void UnlockTier3()
    {
        CurrentTier = 3;
        DeckManager deckManager = Object.FindFirstObjectByType<DeckManager>();
        traitSystem.ActivateEffect(
            new NeutralTier3Effect(Owner, deckManager)
        );

        Debug.Log($"{Owner} unlocked Neutral Tier 3");
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
        card.ModifyStats(0,2);
        used = true;
    }
}
public class NeutralTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Neutral;
    public int Tier => 2;

    private readonly PlayerOwner owner;
    private readonly DeckManager deckManager;

    public NeutralTier2Effect(PlayerOwner owner, DeckManager deckManager)
    {
        this.owner = owner;
        this.deckManager = deckManager;
    }

    public void OnRegister()
    {
        TurnManager.Instance.OnTurnEnded += OnTurnEnded;
    }

    public void OnUnregister()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnEnded -= OnTurnEnded;
        }
    }

    private void OnTurnEnded(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        TryDiscountRandomCardInHand("end");
    }

    private void TryDiscountRandomCardInHand(string timing)
    {
        if (deckManager == null)
            return;

        List<CardInstance> handCards = new();
        foreach (CardInstance handCard in GetHand(owner))
            if (handCard != null)
                handCards.Add(handCard);
        if (handCards.Count == 0)
            return;

        CardInstance card = handCards[Random.Range(0, handCards.Count)];
        if (card == null)
            return;

        card.AddTemporaryManaModifier(-1);
        Debug.Log($"[Neutral T2] Reduced cost of {card.name} for {owner} at {timing} of turn");
    }

    private IEnumerable<CardInstance> GetHand(PlayerOwner handOwner)
    {
        if (deckManager == null)
            yield break;

        HandManager hand = handOwner == PlayerOwner.Player ? deckManager.handManager : deckManager.handManagerEnemy;
        if (hand == null)
            yield break;

        foreach (var go in hand.handCards)
            if (go != null)
                yield return go.GetComponent<CardInstance>();
    }
}
public class NeutralTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Neutral;
    public int Tier => 3;

    private readonly PlayerOwner owner;

    private readonly DeckManager deckManager;
    private bool buffUsedThisTurn;

    public NeutralTier3Effect(PlayerOwner owner, DeckManager deckManager)
    {
        this.owner = owner;
        this.deckManager = deckManager;
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

        buffUsedThisTurn = false;
        TryDiscountRandomCardInHand();
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (buffUsedThisTurn)
            return;

        if (card.Owner != owner)
            return;

        if (card.Data.cardType != "minion")
            return;

        card.ModifyStats(2, 0);
        buffUsedThisTurn = true;
    }

    private void TryDiscountRandomCardInHand()
    {
        if (deckManager == null)
            return;

        HandManager hand = owner == PlayerOwner.Player ? deckManager.handManager : deckManager.handManagerEnemy;
        if (hand == null || hand.handCards.Count == 0)
            return;

        int index = Random.Range(0, hand.handCards.Count);
        CardInstance card = hand.handCards[index]?.GetComponent<CardInstance>();
        if (card == null)
            return;

        card.AddTemporaryManaModifier(-1);
        Debug.Log($"[Neutral T3] Reduced cost of {card.name} for {owner} at start of turn");

    }
}
#endregion
#region SoulForce Trait
public class SoulForceProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.SoulForce;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }
    public int CurrentProgress => soulsCollected;

    private readonly int maxTier;
    private readonly TraitSystem traitSystem;
    private readonly DeckManager deckManager;
    private int soulsCollected;
    private int soulsCollectedThisTurn;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    public SoulForceProgression(PlayerOwner owner, int maxTier, TraitSystem traitSystem, DeckManager deckManager)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
        this.deckManager = deckManager;
    }

    public void Register()
    {
        GameManager.Instance.OnCardKilled += OnCardKill;
        GameManager.Instance.SetSouls(Owner, 0);
        TurnManager.Instance.OnTurnStarted += OnTurnStarted;
        PushInitialState();
    }

    public void Unregister()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnCardKilled -= OnCardKill;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted -= OnTurnStarted;
    }

    public void ResetProgression()
    {
        soulsCollected = 0;
        soulsCollectedThisTurn = 0;
    }

    public void PushInitialState()
    {
        OnProgressUpdated?.Invoke(Trait, soulsCollected, GetCurrentCap(), Owner);
    }

    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 3,
            1 => 6,
            2 => 12,
            _ => 999
        };
    }

    private void OnTurnStarted(PlayerOwner turnOwner)
    {
        if (turnOwner != Owner)
            return;

        soulsCollectedThisTurn = 0;
    }

    private void OnCardKill(CardInstance killed)
    {
        if (killed == null || killed.Owner == Owner)
            return;


        int currentSouls = GameManager.Instance.GetSouls(Owner);
        GameManager.Instance.SetSouls(Owner, currentSouls + 1);

        soulsCollected++;
        soulsCollectedThisTurn++;
        OnProgressUpdated?.Invoke(Trait, soulsCollected, GetCurrentCap(), Owner);

        if (CurrentTier >= 1 && soulsCollectedThisTurn == 1)
            DiscountRandomCardInHand();

        if (soulsCollected >= 3 && CurrentTier < 1 && maxTier >= 1)
            UnlockTier1();

        if (soulsCollected >= 6 && CurrentTier < 2 && maxTier >= 2)
            UnlockTier2();

        if (soulsCollected >= 12 && CurrentTier < 3 && maxTier >= 3)
            UnlockTier3();
    }

    private void DiscountRandomCardInHand()
    {
        HandManager hand = Owner == PlayerOwner.Player ? deckManager.handManager : deckManager.handManagerEnemy;
        if (hand == null || hand.handCards.Count == 0)
            return;

        int randomIndex = Random.Range(0, hand.handCards.Count);
        CardInstance randomCard = hand.handCards[randomIndex]?.GetComponent<CardInstance>();
        if (randomCard == null)
            return;

        randomCard.AddTemporaryManaModifier(-2);
    }

    private void UnlockTier1()
    {
        CurrentTier = 1;
        traitSystem.ActivateEffect(new SoulForceTier1Effect(Owner));
    }

    private void UnlockTier2()
    {
        CurrentTier = 2;
        traitSystem.ActivateEffect(new SoulForceTier2Effect(Owner));
    }

    private void UnlockTier3()
    {
        CurrentTier = 3;
        traitSystem.ActivateEffect(new SoulForceTier3Effect(Owner));
    }
}

public class SoulForceTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.SoulForce;
    public int Tier => 1;

    private readonly PlayerOwner owner;

    public SoulForceTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        Debug.Log($"[SoulForce T1] Activated for {owner}");
    }

    public void OnUnregister() { }
}

public class SoulForceTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.SoulForce;
    public int Tier => 2;

    private readonly PlayerOwner owner;

    public SoulForceTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        Debug.Log($"[SoulForce T2] Activated for {owner}");
    }

    public void OnUnregister() { }
}

public class SoulForceTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.SoulForce;
    public int Tier => 3;

    private readonly PlayerOwner owner;

    public SoulForceTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        TurnManager.Instance.OnTurnEnded += OnTurnEnded;
    }

    public void OnUnregister()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnEnded -= OnTurnEnded;
    }
    private void OnTurnEnded(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        int stock = GameManager.Instance.GetSouls(owner);
        if (stock < 5)
            return;

        // Use the ownerless overload — no card gets credit, no Tier 2 buff fires
        int consumed = GameManager.Instance.ConsumeSoul(owner, 5);
        if (consumed <= 0)
            return;

        List<CardData> graceSpells = CardDatabase.Instance.Cards.Values
            .Where(card =>
                card != null &&
                string.Equals(card.cardType, "spell", System.StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(card.effect) &&
                card.effect.IndexOf("evangelistgrace", System.StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();

        if (graceSpells.Count == 0)
            return;

        CardData randomGrace = graceSpells[Random.Range(0, graceSpells.Count)];
        GameManager.Instance.AddCardToHand(owner, randomGrace.id, -1);
    }
}
#endregion
#region Combo Trait
public class ComboProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Combo;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int tier1TurnsCompleted;
    private int tier2TurnsCompleted;
    private int tier3TurnsCompleted;
    private int cardsPlayedThisTurn;
    private readonly HashSet<int> countedCardIdsThisTurn = new();
    private bool reachedTwoCardsThisTurn;
    private bool reachedThreeCardsThisTurn;
    private bool reachedFourCardsThisTurn;
    private EnemyAIController enemyAIController;

    public int CurrentProgress => CurrentTier switch
    {
        0 => tier1TurnsCompleted,
        1 => tier2TurnsCompleted,
        2 => tier3TurnsCompleted,
        _ => 9999
    };

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;
    private readonly AllyCardDropArea allyBoard;
    private readonly EnemyCardDropArea enemyBoard;

    public ComboProgression(PlayerOwner owner, int maxTier, TraitSystem traitSystem)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
        allyBoard = GameManager.Instance.allyDropArea;
        enemyBoard = GameManager.Instance.enemyDropArea;
    }

    public void ResetProgression()
    {
        tier1TurnsCompleted = 0;
        tier2TurnsCompleted = 0;
        tier3TurnsCompleted = 0;
        cardsPlayedThisTurn = 0;
        countedCardIdsThisTurn.Clear();
        reachedTwoCardsThisTurn = false;
        reachedThreeCardsThisTurn = false;
        reachedFourCardsThisTurn = false;
    }

    public void PushInitialState()
    {
        OnProgressUpdated?.Invoke(Trait, CurrentProgress, GetCurrentCap(), Owner);
    }

    public void Register()
    {
        Debug.Log($"[ComboProgression] Register for {Owner}");

        allyBoard.OnCardPlayed += OnCardPlayed;
        enemyBoard.OnCardPlayed += OnCardPlayed;

        enemyAIController = Object.FindFirstObjectByType<EnemyAIController>();
        if (enemyAIController != null)
            enemyAIController.OnCardPlayed += OnEnemyAICardPlayed;

        TurnManager.Instance.OnTurnStarted += OnTurnStarted;
        TurnManager.Instance.OnTurnEnded += OnTurnEnded;

        OnProgressUpdated?.Invoke(Trait, CurrentProgress, GetCurrentCap(), Owner);
    }

    public void Unregister()
    {
        Debug.Log($"[ComboProgression] Unregister for {Owner}");

        allyBoard.OnCardPlayed -= OnCardPlayed;
        enemyBoard.OnCardPlayed -= OnCardPlayed;

        if (enemyAIController != null)
            enemyAIController.OnCardPlayed -= OnEnemyAICardPlayed;

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted -= OnTurnStarted;
            TurnManager.Instance.OnTurnEnded -= OnTurnEnded;
        }
    }

    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 3,
            1 => 3,
            2 => 3,
            _ => 999
        };
    }

    private void OnTurnStarted(PlayerOwner turnOwner)
    {
        if (turnOwner != Owner)
            return;

        cardsPlayedThisTurn = 0;
        countedCardIdsThisTurn.Clear();
        reachedTwoCardsThisTurn = false;
        reachedThreeCardsThisTurn = false;
        reachedFourCardsThisTurn = false;
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (card.Owner != Owner)
            return;

        if (!countedCardIdsThisTurn.Add(card.GetInstanceID()))
            return;

        cardsPlayedThisTurn++;
    }

    private void OnEnemyAICardPlayed(CardInstance card)
    {
        if (Owner != PlayerOwner.Enemy)
            return;

        OnCardPlayed(card);
    }

    private void OnTurnEnded(PlayerOwner turnOwner)
    {
        if (turnOwner != Owner)
            return;

        if (!reachedTwoCardsThisTurn && cardsPlayedThisTurn >= 2)
        {
            reachedTwoCardsThisTurn = true;
            if (CurrentTier == 0)
            {
                tier1TurnsCompleted++;
                OnProgressUpdated?.Invoke(Trait, tier1TurnsCompleted, GetCurrentCap(), Owner);

                if (tier1TurnsCompleted >= 3 && CurrentTier < 1 && maxTier >= 1)
                    UnlockTier1();
            }
        }

        if (!reachedThreeCardsThisTurn && cardsPlayedThisTurn >= 3)
        {
            reachedThreeCardsThisTurn = true;
            if (CurrentTier == 1)
            {
                tier2TurnsCompleted++;
                OnProgressUpdated?.Invoke(Trait, tier2TurnsCompleted, GetCurrentCap(), Owner);

                if (tier2TurnsCompleted >= 3 && CurrentTier < 2 && maxTier >= 2)
                    UnlockTier2();
            }
        }

        if (!reachedFourCardsThisTurn && cardsPlayedThisTurn >= 4)
        {
            reachedFourCardsThisTurn = true;
            if (CurrentTier == 2)
            {
                tier3TurnsCompleted++;
                OnProgressUpdated?.Invoke(Trait, tier3TurnsCompleted, GetCurrentCap(), Owner);

                if (tier3TurnsCompleted >= 3 && CurrentTier < 3 && maxTier >= 3)
                    UnlockTier3();
            }
        }
        cardsPlayedThisTurn++;
    }

    private void UnlockTier1()
    {
        CurrentTier = 1;
        OnProgressUpdated?.Invoke(Trait, tier2TurnsCompleted, GetCurrentCap(), Owner);

        traitSystem.ActivateEffect(new ComboTier1Effect(Owner));

        Debug.Log($"{Owner} unlocked Combo Tier 1");
    }

    private void UnlockTier2()
    {
        CurrentTier = 2;
        OnProgressUpdated?.Invoke(Trait, tier3TurnsCompleted, GetCurrentCap(), Owner);

        DeckManager deckManager = Object.FindFirstObjectByType<DeckManager>();
        traitSystem.ActivateEffect(new ComboTier2Effect(Owner, deckManager));

        Debug.Log($"{Owner} unlocked Combo Tier 2");
    }

    private void UnlockTier3()
    {
        CurrentTier = 3;

        DeckManager deckManager = Object.FindFirstObjectByType<DeckManager>();
        traitSystem.ActivateEffect(new ComboTier3Effect(Owner, deckManager));

        Debug.Log($"{Owner} unlocked Combo Tier 3");
    }
}

public class ComboTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Combo;
    public int Tier => 1;

    private readonly PlayerOwner owner;
    private readonly DeckManager deckManager;

    private bool waitingForSecondCard;
    private bool secondCardPlayed;
    private int cardsPlayedThisTurn;
    private readonly List<CardInstance> discountedCards = new();
    private EnemyAIController enemyAIController;

    public ComboTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
        deckManager = Object.FindFirstObjectByType<DeckManager>();
    }

    public void OnRegister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed += OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed += OnCardPlayed;

        if (deckManager != null)
            deckManager.OnCardDrawn += OnCardDrawn;

        enemyAIController = Object.FindFirstObjectByType<EnemyAIController>();
        if (enemyAIController != null && owner==PlayerOwner.Enemy)
            enemyAIController.OnCardPlayed += OnEnemyAICardPlayed;

        TurnManager.Instance.OnTurnStarted += OnTurnStarted;
        TurnManager.Instance.OnTurnEnded += OnTurnEnded;
    }

    public void OnUnregister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed -= OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed -= OnCardPlayed;

        if (deckManager != null)
            deckManager.OnCardDrawn -= OnCardDrawn;

        if (enemyAIController != null)
            enemyAIController.OnCardPlayed -= OnEnemyAICardPlayed;

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted -= OnTurnStarted;
            TurnManager.Instance.OnTurnEnded -= OnTurnEnded;
        }

        RemoveSecondCardDiscountFromHand();
    }

    private void OnTurnStarted(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        cardsPlayedThisTurn = 0;
        waitingForSecondCard = false;
        secondCardPlayed = false;
    }

    private void OnTurnEnded(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        RemoveSecondCardDiscountFromHand();
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (card.Owner != owner)
            return;

        cardsPlayedThisTurn++;

        if (!waitingForSecondCard && cardsPlayedThisTurn == 1)
        {
            waitingForSecondCard = true;
            ApplySecondCardDiscountToHand();
            return;
        }

        if (waitingForSecondCard && !secondCardPlayed && cardsPlayedThisTurn == 2)
        {
            secondCardPlayed = true;
            RemoveSecondCardDiscountFromHand();
        }
    }

    private void OnEnemyAICardPlayed(CardInstance card)
    {
        if (owner != PlayerOwner.Enemy)
            return;

        OnCardPlayed(card);
    }

    private void OnCardDrawn(CardInstance card)
    {
        if (!waitingForSecondCard || secondCardPlayed)
            return;

        if (card.Owner != owner)
            return;

        card.AddTemporaryManaModifier(-1);
        discountedCards.Add(card);
    }

    private void ApplySecondCardDiscountToHand()
    {
        foreach (var card in GetHand(owner))
        {
            card.AddTemporaryManaModifier(-1);
            discountedCards.Add(card);
        }
    }

    private void RemoveSecondCardDiscountFromHand()
    {
        for (int i = discountedCards.Count - 1; i >= 0; i--)
        {
            CardInstance discountedCard = discountedCards[i];
            if (discountedCard == null)
            {
                discountedCards.RemoveAt(i);
                continue;
            }

            if (discountedCard.CurrentZone == CardZone.Hand)
                discountedCard.AddTemporaryManaModifier(1);

            discountedCards.RemoveAt(i);
        }

        waitingForSecondCard = false;
    }

    private IEnumerable<CardInstance> GetHand(PlayerOwner handOwner)
    {
        DeckManager deck = Object.FindFirstObjectByType<DeckManager>();
        if (deck == null)
            yield break;

        HandManager hand =
            handOwner == PlayerOwner.Player
                ? deck.handManager
                : deck.handManagerEnemy;

        foreach (var go in hand.handCards)
            yield return go.GetComponent<CardInstance>();
    }
}

public class ComboTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Combo;
    public int Tier => 2;

    private readonly PlayerOwner owner;
    private readonly DeckManager deckManager;
    private int cardsPlayedThisTurn;
    private bool triggeredThisTurn;
    private EnemyAIController enemyAIController;

    public ComboTier2Effect(PlayerOwner owner, DeckManager deckManager)
    {
        this.owner = owner;
        this.deckManager = deckManager;
    }

    public void OnRegister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed += OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed += OnCardPlayed;

        enemyAIController = Object.FindFirstObjectByType<EnemyAIController>();
        if (enemyAIController != null)
            enemyAIController.OnCardPlayed += OnEnemyAICardPlayed;

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

        if (enemyAIController != null)
            enemyAIController.OnCardPlayed -= OnEnemyAICardPlayed;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted -= OnTurnStarted;
    }

    private void OnTurnStarted(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        cardsPlayedThisTurn = 0;
        triggeredThisTurn = false;
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (card.Owner != owner)
            return;

        cardsPlayedThisTurn++;

        if (triggeredThisTurn || cardsPlayedThisTurn < 3)
            return;

        triggeredThisTurn = true;
        TryIncreaseEnemyHandCardCost();
    }

    private void OnEnemyAICardPlayed(CardInstance card)
    {
        if (owner != PlayerOwner.Enemy)
            return;

        OnCardPlayed(card);
    }

    private void TryIncreaseEnemyHandCardCost()
    {
        if (deckManager == null)
            return;

        HandManager enemyHand =
            owner == PlayerOwner.Player
                ? deckManager.handManagerEnemy
                : deckManager.handManager;

        if (enemyHand == null || enemyHand.handCards.Count == 0)
            return;

        int index = Random.Range(0, enemyHand.handCards.Count);
        CardInstance target = enemyHand.handCards[index].GetComponent<CardInstance>();
        if (target == null)
            return;

        target.AddTemporaryManaModifier(1);
        Debug.Log($"[Combo T2] Increased cost of {target.name} for {target.Owner}");
    }
}

public class ComboTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Combo;
    public int Tier => 3;

    private readonly PlayerOwner owner;
    private readonly DeckManager deckManager;
    private bool hasTriggeredThisGame;
    private int cardsPlayedThisTurn;
    private EnemyAIController enemyAIController;

    public ComboTier3Effect(PlayerOwner owner, DeckManager deckManager)
    {
        this.owner = owner;
        this.deckManager = deckManager;
    }

    public void OnRegister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed += OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed += OnCardPlayed;

        enemyAIController = Object.FindFirstObjectByType<EnemyAIController>();
        if (enemyAIController != null)
            enemyAIController.OnCardPlayed += OnEnemyAICardPlayed;

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

        if (enemyAIController != null)
            enemyAIController.OnCardPlayed -= OnEnemyAICardPlayed;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted -= OnTurnStarted;
    }

    private void OnTurnStarted(PlayerOwner turnOwner)
    {
        if (turnOwner == owner)
            cardsPlayedThisTurn = 0;
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (hasTriggeredThisGame)
            return;

        if (card.Owner != owner)
            return;

        cardsPlayedThisTurn++;

        if (cardsPlayedThisTurn < 4)
            return;

        hasTriggeredThisGame = true;

        if (deckManager != null)
            GameManager.Instance.StartCoroutine(deckManager.Draw(2, owner));

        Debug.Log($"[Combo T3] {owner} played 4 cards in a turn and drew 2 cards.");
    }

    private void OnEnemyAICardPlayed(CardInstance card)
    {
        if (owner != PlayerOwner.Enemy)
            return;

        OnCardPlayed(card);
    }
}
#endregion
#region Fighter Trait
public class FighterProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Fighter;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int FighterPlayed = 0;

    public int CurrentProgress => FighterPlayed;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;
    private readonly AllyCardDropArea allyBoard;
    private readonly EnemyCardDropArea enemyBoard;

    public FighterProgression(PlayerOwner owner, int maxTier, TraitSystem traitSystem, AllyCardDropArea allyBoard, EnemyCardDropArea enemyBoard)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
        this.allyBoard = allyBoard;
        this.enemyBoard = enemyBoard;
    }
    public void ResetProgression()
    {
        FighterPlayed = 0;
    }
    public void PushInitialState()
    {
        int cap = GetCurrentCap();
        OnProgressUpdated?.Invoke(Trait, FighterPlayed, cap, Owner);
    }

    public void Register()
    {
        Debug.Log($"[FighterProgression] Register for {Owner}");

        allyBoard.OnCardPlayed += OnCardPlayed;
        enemyBoard.OnCardPlayed += OnCardPlayed;

        OnProgressUpdated?.Invoke(Trait, FighterPlayed, GetCurrentCap(), Owner);
    }
    public void Unregister()
    {
        Debug.Log($"[FighterProgression] Unregister for {Owner}");

        allyBoard.OnCardPlayed -= OnCardPlayed;
        enemyBoard.OnCardPlayed -= OnCardPlayed;
    }
    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 5,
            1 => 10,
            2 => 15,
            _ => 999
        };
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (card.Owner != Owner)
            return;

        if (!card.HasTrait("Fighter") || card.Data.cardType!="minion")
            return;
        FighterPlayed++;
        OnProgressUpdated?.Invoke(Trait, FighterPlayed, GetCurrentCap(), Owner);

        if (FighterPlayed >= 5 && CurrentTier < 1 && maxTier >= 1)
        {
            UnlockTier1();
        }

        if (FighterPlayed >= 10 && CurrentTier < 2 && maxTier >= 2)
        {
            UnlockTier2();
        }

        if (FighterPlayed >= 15 && CurrentTier < 3 && maxTier >= 3)
        {
            UnlockTier3();
        }
    }

    private void UnlockTier1()
    {
        CurrentTier = 1;

        traitSystem.ActivateEffect(
            new FighterTier1Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked Fighter Tier 1");
    }
    private void UnlockTier2()
    {
        CurrentTier = 2;
        traitSystem.ActivateEffect(
            new FighterTier2Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked Fighter Tier 2");
    }
    private void UnlockTier3()
    {
        CurrentTier = 3;
        traitSystem.ActivateEffect(
            new FighterTier3Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked Fighter Tier 3");
    }

}
public class FighterTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Fighter;
    public int Tier => 1;

    private readonly PlayerOwner owner;
    private bool used;

    public FighterTier1Effect(PlayerOwner owner)
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

        if (card.Data.cardType != "minion")
            return;
        card.ModifyStats(1,1);
        used = true;
    }
}
public class FighterTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Fighter;
    public int Tier => 2;

    private readonly PlayerOwner owner;

    public FighterTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        DiscoverFighterArtifact();
    }
    public void OnUnregister()
    {

    }
    void DiscoverFighterArtifact()
    {
        GameManager.Instance.DiscoverEffectDiscount("fighterartifact", owner,3);
    }
}
public class FighterTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Fighter;
    public int Tier => 3;

    private readonly PlayerOwner owner;
    public FighterTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }
    public void OnRegister()
    {
        TurnManager.Instance.OnTurnEnded += OnTurnEnded;
    }
    public void OnUnregister()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnEnded -= OnTurnEnded;
    }

    private void OnTurnEnded(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        GameManager.Instance.BuffAllAllies(1,1,owner);
    }
}
#endregion
#region Chaos Trait
public class ChaosProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Chaos;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int randomPlayed = 0;

    public int CurrentProgress => randomPlayed;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;
    private readonly AllyCardDropArea allyBoard;
    private readonly EnemyCardDropArea enemyBoard;

    public ChaosProgression(PlayerOwner owner, int maxTier, TraitSystem traitSystem, AllyCardDropArea allyBoard, EnemyCardDropArea enemyBoard)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
        this.allyBoard = allyBoard;
        this.enemyBoard = enemyBoard;
    }
    public void ResetProgression()
    {
        randomPlayed = 0;
    }
    public void PushInitialState()
    {
        int cap = GetCurrentCap();
        OnProgressUpdated?.Invoke(Trait, randomPlayed, cap, Owner);
    }

    public void Register()
    {
        Debug.Log($"[chaosProgression] Register for {Owner}");

        allyBoard.OnCardPlayed += OnCardPlayed;
        enemyBoard.OnCardPlayed += OnCardPlayed;

        OnProgressUpdated?.Invoke(Trait, randomPlayed, GetCurrentCap(), Owner);
    }
    public void Unregister()
    {
        Debug.Log($"[chaosProgression] Unregister for {Owner}");

        allyBoard.OnCardPlayed -= OnCardPlayed;
        enemyBoard.OnCardPlayed -= OnCardPlayed;
    }
    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 4,
            1 => 8,
            2 => 14,
            _ => 999
        };
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (card.Owner != Owner)
            return;

        if (!card.HasText("random"))
            return;
        randomPlayed++;
        OnProgressUpdated?.Invoke(Trait, randomPlayed, GetCurrentCap(), Owner);

        if (randomPlayed >= 4 && CurrentTier < 1 && maxTier >= 1)
        {
            UnlockTier1();
        }

        if (randomPlayed >= 8 && CurrentTier < 2 && maxTier >= 2)
        {
            UnlockTier2();
        }

        if (randomPlayed >= 14 && CurrentTier < 3 && maxTier >= 3)
        {
            UnlockTier3();
        }
    }

    private void UnlockTier1()
    {
        CurrentTier = 1;

        traitSystem.ActivateEffect(
            new ChaosTier1Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked chaos Tier 1");
    }
    private void UnlockTier2()
    {
        CurrentTier = 2;
        DeckManager deckManager = Object.FindFirstObjectByType<DeckManager>();
        traitSystem.ActivateEffect(new ChaosTier2Effect(Owner));

        Debug.Log($"{Owner} unlocked chaos Tier 2");
    }
    private void UnlockTier3()
    {
        CurrentTier = 3;
        DeckManager deckManager = Object.FindFirstObjectByType<DeckManager>();
        traitSystem.ActivateEffect(new ChaosTier3Effect(Owner));

        Debug.Log($"{Owner} unlocked chaos Tier 3");
    }

}
public class ChaosTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Chaos;
    public int Tier => 1;

    private readonly PlayerOwner owner;

    public ChaosTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }
    public void OnRegister()
    {
        TurnManager.Instance.OnTurnStarted += OnTurnStarted;
    }
    public void OnUnregister()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted -= OnTurnStarted;
    }

    private void OnTurnStarted(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;
        if((GameManager.Instance.AllyCurrentMana >=10 && owner==PlayerOwner.Player) || 
            (GameManager.Instance.EnemyCurrentMana >= 10 && owner == PlayerOwner.Enemy))
        GameManager.Instance.TrySummonForOwnerManaCost(owner,5);
        else
            GameManager.Instance.TrySummonForOwnerManaCost(owner, 2);
    }
}
public class ChaosTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Chaos;
    public int Tier => 2;

    private readonly PlayerOwner owner;

    public ChaosTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }
    public void OnRegister() { }
    public void OnUnregister() { }
}
public class ChaosTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Chaos;
    public int Tier => 3;

    private readonly PlayerOwner owner;

    public ChaosTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }
    public void OnRegister()
    {
        TurnManager.Instance.OnTurnEnded += OnTurnEnded;
    }
    public void OnUnregister()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnEnded -= OnTurnEnded;
    }

    private void OnTurnEnded(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        GameManager.Instance.AddCardToHand(owner, 127);
    }
}
#endregion
#region Speedster Trait
public class SpeedsterProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Speedster;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int speedsterAttacks = 0;
    public int CurrentProgress => speedsterAttacks;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;

    public SpeedsterProgression(PlayerOwner owner, int maxTier, TraitSystem traitSystem)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
    }
    public void ResetProgression()
    {
        speedsterAttacks = 0;
    }
    public void PushInitialState()
    {
        int cap = GetCurrentCap();
        OnProgressUpdated?.Invoke(Trait, speedsterAttacks, cap, Owner);
    }
    public void Register()
    {
        Debug.Log($"[SpeedsterProgression] Register for {Owner}");

        GameManager.Instance.OnCardAttack += OnSpeedsterAttack;

        OnProgressUpdated?.Invoke(Trait, speedsterAttacks, GetCurrentCap(), Owner);
    }
    public void Unregister()
    {
        Debug.Log($"[SpeedsterProgression] Unregister for {Owner}");

        GameManager.Instance.OnCardAttack -= OnSpeedsterAttack;
    }
    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 4,
            1 => 8,
            2 => 12,
            _ => 999
        };
    }

    private void OnSpeedsterAttack(CardInstance card)
    {
        if (card.Owner != Owner)
            return;

        if (!card.HasTrait("Speedster"))
            return;
        speedsterAttacks++;
        OnProgressUpdated?.Invoke(Trait, speedsterAttacks, GetCurrentCap(), Owner);

        if (speedsterAttacks >= 4 && CurrentTier < 1 && maxTier >= 1)
        {
            UnlockTier1();
        }
        if (speedsterAttacks >= 8 && CurrentTier < 2 && maxTier >= 2)
        {
            UnlockTier2();
        }
        if (speedsterAttacks >= 12 && CurrentTier < 3 && maxTier >= 3)
        {
            UnlockTier3();
        }
    }

    private void UnlockTier1()
    {
        CurrentTier = 1;

        traitSystem.ActivateEffect(
            new SpeedsterTier1Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked speedster Tier 1");
    }
    private void UnlockTier2()
    {
        CurrentTier = 2;

        traitSystem.ActivateEffect(
            new SpeedsterTier2Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked speedster Tier 2");
    }
    private void UnlockTier3()
    {
        CurrentTier = 3;

        traitSystem.ActivateEffect(
            new SpeedsterTier3Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked speedster Tier 3");
    }
}
public class SpeedsterTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Speedster;
    public int Tier => 1;

    private readonly PlayerOwner owner;

    public SpeedsterTier1Effect(PlayerOwner owner)
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
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (card.Owner != owner)
            return;

        if (card.Data.cardType != "minion" || !card.HasTrait("Speedster"))
            return;
        Debug.Log($"Buffing first card {card.name}, for {owner}");
        card.ModifyStats(1, 0);
    }
}
public class SpeedsterTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Speedster;
    public int Tier =>2 ;

    private readonly PlayerOwner owner;

    public SpeedsterTier2Effect(PlayerOwner owner)
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
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (card.Owner != owner)
            return;

        if (card.Data.cardType != "minion" || !card.HasTrait("Speedster"))
            return;

        if (!card.HasKeyword("quickstrike")){
            card.CurrentEffect += " quickstrike";
            card.CurrentEffectText += "\nQuickstrike";
        }
    }
}
public class SpeedsterTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Speedster;
    public int Tier => 3;

    private readonly PlayerOwner owner;

    public SpeedsterTier3Effect(PlayerOwner owner)
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
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (card.Owner != owner)
            return;

        if (card.Data.cardType != "minion")
            return;

        if (!card.HasKeyword("quickstrike"))
        {
            card.CurrentEffect += " quickstrike";
            card.CurrentEffectText += "\nQuickstrike";
        }
        card.ModifyStats(1, 0);
    }
}
#endregion
#region Pokemon Trait
public class PokemonProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Pokemon;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int pokemonKills = 0;

    public int CurrentProgress => pokemonKills;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;
    private readonly AllyCardDropArea allyBoard;
    private readonly EnemyCardDropArea enemyBoard;

    public PokemonProgression(PlayerOwner owner, int maxTier, TraitSystem traitSystem, AllyCardDropArea allyBoard, EnemyCardDropArea enemyBoard)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
        this.allyBoard = allyBoard;
        this.enemyBoard = enemyBoard;
    }
    public void ResetProgression()
    {
        pokemonKills = 0;
    }
    public void PushInitialState()
    {
        int cap = GetCurrentCap();
        OnProgressUpdated?.Invoke(Trait, pokemonKills, cap, Owner);
    }
    public void Register()
    {
        Debug.Log($"[PokemonProgression] Register for {Owner}");

        GameManager.Instance.OnCardKiller += OnCardKill;

        OnProgressUpdated?.Invoke(Trait, pokemonKills, GetCurrentCap(), Owner);
    }
    public void Unregister()
    {
        GameManager.Instance.OnCardKiller -= OnCardKill;
    }
    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 3,
            1 => 6,
            2 => 12,
            _ => 999
        };
    }
    private void OnCardKill(CardInstance card)
    {
        if (card.Owner != Owner)
            return;

        if (!card.HasTrait("Pokemon"))
            return;

        AddProgress(1, "kill");
    }

    public void AddCatchProgress(int amount)
    {
        AddProgress(amount, "catch");
    }

    private void AddProgress(int amount, string source)
    {
        if (amount <= 0)
            return;

        pokemonKills += amount;
        OnProgressUpdated?.Invoke(Trait, pokemonKills, GetCurrentCap(), Owner);

        Debug.Log($"Pokemon progression for {Owner} via {source}, count is now {pokemonKills}");

        if (pokemonKills >= 3 && CurrentTier < 1 && maxTier >= 1)
        {
            UnlockTier1();
        }

        if (pokemonKills >= 6 && CurrentTier < 2 && maxTier >= 2)
        {
            UnlockTier2();
        }
        if (pokemonKills >= 12 && CurrentTier < 3 && maxTier >= 3)
        {
            UnlockTier3();
        }
    }
    private void UnlockTier1()
    {
        CurrentTier = 1;

        traitSystem.ActivateEffect(
            new PokemonTier1Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked Pokemon Tier 1");
    }
    private void UnlockTier2()
    {
        CurrentTier = 2;
        traitSystem.ActivateEffect(
            new PokemonTier2Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked Pokemon Tier 2");
    }
    private void UnlockTier3()
    {
        CurrentTier = 3;
        traitSystem.ActivateEffect(
            new PokemonTier3Effect(Owner)
        );
        Debug.Log($"{Owner} unlocked Pokemon Tier 3");
    }
}
public class PokemonTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Pokemon;
    public int Tier => 1;

    private readonly PlayerOwner owner;
    private bool used;

    public PokemonTier1Effect(PlayerOwner owner)
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
    }
    public void OnUnregister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed -= OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed -= OnCardPlayed;
    }
    private void OnCardPlayed(CardInstance card)
    {
        if (used)
            return;

        if (card.Owner != owner)
            return;

        if (card.Data.cardType != "minion")
            return;

        Debug.Log($"Evolving instant card {card.name}, for {owner}");
        int detectedID = GetEvolutionId(card.CurrentEffect);
        card.MorphTo(detectedID);
        used = true;
    }
    private int GetEvolutionId(string effect)
    {
        int value = -1;

        if (!effect.Contains("morphto"))
        {
            return -1;
        }

        int startEffect = effect.IndexOf("morphto");

        string morphEffect = effect.Substring(startEffect);

        int startID = morphEffect.IndexOf('(');
        int endID = morphEffect.IndexOf(')');

        if (startID < 0 || endID < 0 || endID <= startID + 1)
        {
            return -1;
        }

        string valueStr = morphEffect.Substring(startID + 1, endID - startID - 1);

        if (int.TryParse(valueStr, out value))
        {
            return value;
        }
        else return -1;
    }
}
public class PokemonTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Pokemon;
    public int Tier => 2;

    private readonly PlayerOwner owner;
    private bool used;

    public PokemonTier2Effect(PlayerOwner owner)
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
    }
    public void OnUnregister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed -= OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed -= OnCardPlayed;
    }
    private void OnCardPlayed(CardInstance card)
    {
        if (used)
            return;

        if (card.Owner != owner)
            return;

        if (card.Data.cardType != "minion" || !card.HasTrait("Pokemon"))
            return;

        Debug.Log($"Evolving instant card {card.name}, for {owner}");
        card.MorphTo(GetEvolutionId(card.CurrentEffect));
        card.ModifyStats(3,3);
        used = true;
    }
    private int GetEvolutionId(string effect)
    {
        int value = -1;

        if (!effect.Contains("morphto"))
        {
            return -1;
        }

        int startEffect = effect.IndexOf("morphto");

        string morphEffect = effect.Substring(startEffect);

        int startID = morphEffect.IndexOf('(');
        int endID = morphEffect.IndexOf(')');

        if (startID < 0 || endID < 0 || endID <= startID + 1)
        {
            return -1;
        }

        string valueStr = morphEffect.Substring(startID + 1, endID - startID - 1);

        if (int.TryParse(valueStr, out value))
        {
            return value;
        }
        else return -1;
    
}
}
public class PokemonTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Pokemon;
    public int Tier => 3;

    private readonly PlayerOwner owner;
    private bool used;


    public PokemonTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }
    public void OnRegister()
    {
        used = false;
        DiscoverLegendary();
    }
    void DiscoverLegendary()
    {

        if (used) return;
        GameManager.Instance.DiscoverEffect("legendarypokemon", owner);//FIX IDs

        used = true;
    }
    public void OnUnregister()
    {
        throw new System.NotImplementedException();
    }
}
#endregion
#region MonsterHunter
public class MonsterHunterProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.MonsterHunter;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int colossusDeaths;

    public int CurrentProgress => colossusDeaths;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;

    public MonsterHunterProgression(
        PlayerOwner owner,
        int maxTier,
        TraitSystem traitSystem,
        AllyCardDropArea allyBoard,
        EnemyCardDropArea enemyBoard)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
    }

    public void Register()
    {
        Debug.Log($"[MonsterHunterProgression] Register for {Owner}");
        GameManager.Instance.OnCardKilled += OnCardKilled;
        PushInitialState();
    }

    public void Unregister()
    {
        GameManager.Instance.OnCardKilled -= OnCardKilled;
    }

    public void ResetProgression()
    {
        colossusDeaths = 0;
    }

    public void PushInitialState()
    {
        OnProgressUpdated?.Invoke(Trait, colossusDeaths, GetCurrentCap(), Owner);
    }

    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 4,
            1 => 8,
            2 => 12,
            3 => 999,
            _ => 9999,
        };
    }

    private void OnCardKilled(CardInstance card)
    {
        // 1. Must be ally
        if (card.Owner != Owner)
            return;

        // 2. Must be colossus
        if (card.BaseManaCost < 5)//Change to 5
            return;

        // 3. Must have MonsterHunter trait
        if (!card.HasTrait("MonsterHunter"))
            return;

        colossusDeaths++;
        OnProgressUpdated?.Invoke(Trait, colossusDeaths, GetCurrentCap(), Owner);

        Debug.Log($"[MH] Colossus death for {Owner}: {colossusDeaths}");

        if (colossusDeaths >= 4 && CurrentTier < 1 && maxTier >= 1)
            UnlockTier1();

        if (colossusDeaths >= 8 && CurrentTier < 2 && maxTier >= 2)
            UnlockTier2(); 
        
        if (colossusDeaths >= 12 && CurrentTier < 3 && maxTier >= 3)
            UnlockTier3();
    }

    private void UnlockTier1()
    {
        CurrentTier = 1;
        traitSystem.ActivateEffect(new MonsterHunterTier1Effect(Owner));
    }

    private void UnlockTier2()
    {
        CurrentTier = 2;
        traitSystem.ActivateEffect(new MonsterHunterTier2Effect(Owner));
    }
    private void UnlockTier3()
    {
        CurrentTier = 3;
        traitSystem.ActivateEffect(new MonsterHunterTier3Effect(Owner));
    }
}
public class MonsterHunterTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.MonsterHunter;
    public int Tier => 1;
    private readonly PlayerOwner owner;
    private bool used;
    public MonsterHunterTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }
    public void OnRegister()
    {
        if (used) return;

         

        GameManager.Instance.EnqueueDeferredAction(() =>
        {
            SummonTierMonster(1);
        });

        used = true;
    }

    public void OnUnregister() { }
    private void SummonTierMonster(int tier)
    {
         

        GameManager.Instance.BeginEffect();

        try
        {
            List<CardData> options =
                CardDatabase.Instance.GetCardsByEffect($"tier{tier}monster*");

            if (options == null || options.Count == 0)
                return;

            CardData chosen = options[Random.Range(0, options.Count)];

            GameManager.Instance.TrySummonForOwner(owner, chosen.id, true);
        }
        finally
        {
            GameManager.Instance.EndEffect();
        }
    }

}
public class MonsterHunterTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.MonsterHunter;
    public int Tier => 2;

    private readonly PlayerOwner owner;
    private bool used;

    public MonsterHunterTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }
    public void OnRegister()
    {
        if (used) return;

         

        GameManager.Instance.EnqueueDeferredAction(() =>
        {
            SummonTierMonster(2);
        });

        used = true;
    }

    public void OnUnregister() { }
    private void SummonTierMonster(int tier)
    {
         

        GameManager.Instance.BeginEffect();

        try
        {
            List<CardData> options =
                CardDatabase.Instance.GetCardsByEffect($"tier{tier}monster*");

            if (options == null || options.Count == 0)
                return;

            CardData chosen = options[Random.Range(0, options.Count)];

            GameManager.Instance.TrySummonForOwner(owner, chosen.id, true);
        }
        finally
        {
            GameManager.Instance.EndEffect();
        }
    }

}
public class MonsterHunterTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.MonsterHunter;
    public int Tier => 3;

    private readonly PlayerOwner owner;
    private bool used;

    public MonsterHunterTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }
    public void OnRegister()
    {
        if (used) return;

         

        GameManager.Instance.EnqueueDeferredAction(() =>
        {
            SummonTierMonster(3);
        });

        used = true;
    }

    public void OnUnregister() { }
    private void SummonTierMonster(int tier)
    {
         

        GameManager.Instance.BeginEffect();

        try
        {
            List<CardData> options =
                CardDatabase.Instance.GetCardsByEffect($"tier{tier}monster*");

            if (options == null || options.Count == 0)
                return;

            CardData chosen = options[Random.Range(0, options.Count)];

            GameManager.Instance.TrySummonForOwner(owner, chosen.id, true);
        }
        finally
        {
            GameManager.Instance.EndEffect();
        }
    }

}
#endregion
#region Healer
public class HealerProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Healer;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int healAmount;

    public int CurrentProgress => healAmount;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;

    public HealerProgression(
        PlayerOwner owner,
        int maxTier,
        TraitSystem traitSystem)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
    }

    public void Register()
    {
        Debug.Log($"[Healer] Register for {Owner}");
        GameManager.Instance.OnOwnerHeal += OnAllyHeal;
        PushInitialState();
    }

    public void Unregister()
    {
        GameManager.Instance.OnOwnerHeal -= OnAllyHeal;
    }

    public void ResetProgression()
    {
        healAmount = 0;
    }

    public void PushInitialState()
    {
        OnProgressUpdated?.Invoke(Trait, healAmount, GetCurrentCap(), Owner);
    }

    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 8,
            1 => 16,
            2 => 25,
            3 => 9999,
            _ => 9999,
        };
    }

    private void OnAllyHeal(PlayerOwner owner, int amount)
    {
        // 1. Must be ally
        if (owner != Owner)
            return;

        healAmount+=amount;
        OnProgressUpdated?.Invoke(Trait, healAmount, GetCurrentCap(), Owner);

        Debug.Log($"[Healer] Heal Amount for {Owner}: {healAmount}");

        if (healAmount >= 8 && CurrentTier < 1 && maxTier >= 1)
            UnlockTier1();

        if (healAmount >= 16 && CurrentTier < 2 && maxTier >= 2)
            UnlockTier2(); 
        
        if (healAmount >= 25 && CurrentTier < 3 && maxTier >= 3)
            UnlockTier3();
    }

    private void UnlockTier1()
    {
        CurrentTier = 1;
        traitSystem.ActivateEffect(new HealerTier1Effect(Owner));
    }
    private void UnlockTier2()
    {
        CurrentTier = 2;
        traitSystem.ActivateEffect(new HealerTier2Effect(Owner));
    }
    private void UnlockTier3()
    {
        CurrentTier = 3;
        traitSystem.ActivateEffect(new HealerTier3Effect(Owner));
    }
}
public class HealerTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Healer;
    public int Tier => 1;

    private readonly PlayerOwner owner;
    private bool usedThisTurn;

    public HealerTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        GameManager.Instance.OnOwnerHealResolved += OnOwnerHealResolved;
        TurnManager.Instance.OnTurnStarted += OnTurnStarted;
    }

    public void OnUnregister()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnOwnerHealResolved -= OnOwnerHealResolved;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted -= OnTurnStarted;
    }

    private void OnTurnStarted(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        usedThisTurn = false;
    }

    private void OnOwnerHealResolved(PlayerOwner healedOwner, IAttackable target, int healedAmount, int overhealAmount)
    {
        if (usedThisTurn || healedOwner != owner || healedAmount <= 0)
            return;

        DeckManager deckManager = Object.FindFirstObjectByType<DeckManager>();
        if (deckManager != null)
            deckManager.StartCoroutine(deckManager.Draw(1, owner));

        usedThisTurn = true;
    }
}
public class HealerTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Healer;
    public int Tier => 2;

    private readonly PlayerOwner owner;

    public HealerTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        GameManager.Instance.OnOwnerHealResolved += OnOwnerHealResolved;
    }

    public void OnUnregister()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnOwnerHealResolved -= OnOwnerHealResolved;
    }

    private void OnOwnerHealResolved(PlayerOwner healedOwner, IAttackable target, int healedAmount, int overhealAmount)
    {
        if (healedOwner != owner || overhealAmount <= 0 || target == null)
            return;

        if (target is CoreInstance core)
        {
            core.AddShield(overhealAmount/2);
            return;
        }

        if (target is CardInstance unit)
            unit.ModifyStats(0, overhealAmount/2);
    }
}
public class HealerTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Healer;
    public int Tier => 3;

    private readonly PlayerOwner owner;

    public HealerTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        GameManager.Instance.OnOwnerHealResolved += OnOwnerHealResolved;
    }

    public void OnUnregister()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnOwnerHealResolved -= OnOwnerHealResolved;
    }

    private void OnOwnerHealResolved(PlayerOwner healedOwner, IAttackable target, int healedAmount, int overhealAmount)
    {
        if (healedOwner != owner || healedAmount <= 0)
            return;

        int darkHealDamage = healedAmount;
        if (darkHealDamage <= 0)
            return;

        GameManager.Instance.DamageRandomEnemyAmount(darkHealDamage, owner);
    }
}
#endregion
#region Faith
public class FaithProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Faith;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int discoverCount;

    public int CurrentProgress => discoverCount;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;

    public FaithProgression(
        PlayerOwner owner,
        int maxTier,
        TraitSystem traitSystem)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
    }

    public void Register()
    {
        Debug.Log($"[Faith] Register for {Owner}");
        GameManager.Instance.OnDiscover += OnAllyDiscover;
        PushInitialState();
    }

    public void Unregister()
    {
        GameManager.Instance.OnDiscover -= OnAllyDiscover;
    }

    public void ResetProgression()
    {
        discoverCount = 0;
    }

    public void PushInitialState()
    {
        OnProgressUpdated?.Invoke(Trait, discoverCount, GetCurrentCap(), Owner);
    }
    public void OnAllyDiscover(PlayerOwner owner)
    {
        // 1. Must be ally
        if (owner != Owner)
            return;

        discoverCount ++;
        OnProgressUpdated?.Invoke(Trait, discoverCount, GetCurrentCap(), Owner);
        if (discoverCount >= 3 && CurrentTier < 1 && maxTier >= 1)
            UnlockTier1();

        if (discoverCount >= 6 && CurrentTier < 2 && maxTier >= 2)
            UnlockTier2(); 
        
        if (discoverCount >= 10 && CurrentTier < 3 && maxTier >= 3)
            UnlockTier3();
    }
    private void UnlockTier1()
    {
        CurrentTier = 1;
        traitSystem.ActivateEffect(new FaithTier1Effect(Owner));
    }
    private void UnlockTier2()
    {
        CurrentTier = 2;
        traitSystem.ActivateEffect(new FaithTier2Effect(Owner));
    }
    private void UnlockTier3()
    {
        CurrentTier = 3;
        traitSystem.ActivateEffect(new FaithTier3Effect(Owner));
    }
    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 3,
            1 => 6,
            2 => 10,
            3 => 9999,
            _ => 9999,
        };
    }
}

public class FaithTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Faith;
    public int Tier => 1;

    private readonly PlayerOwner owner;
    private bool used;

    public FaithTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        if (used) return;

         
        GameManager.Instance.AddCardToHand(owner, 64);
        GameManager.Instance.AddCardToHand(owner, 60);
        used = true;
    }

    public void OnUnregister() { }

}
public class FaithTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Faith;
    public int Tier => 2;

    private readonly PlayerOwner owner;

    public FaithTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        //No visible effect since it discounts discoveries
    }

    public void OnUnregister() { }

}
public class FaithTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Faith;
    public int Tier => 3;

    private readonly PlayerOwner owner;

    public FaithTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        //No visible effect since it discounts discoveries
    }

    public void OnUnregister() { }

}

#endregion
#region Avatar
public class AvatarProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Avatar;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int praiseCount;

    public int CurrentProgress => praiseCount;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;

    public AvatarProgression(
        PlayerOwner owner,
        int maxTier,
        TraitSystem traitSystem)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
    }

    public void Register()
    {
        Debug.Log($"[Avatar] Register for {Owner}");
        GameManager.Instance.OnPraise += OnAllyPraise;
        PushInitialState();
    }

    public void Unregister()
    {
        GameManager.Instance.OnPraise -= OnAllyPraise;
    }

    public void ResetProgression()
    {
        praiseCount = 0;
    }

    public void PushInitialState()
    {
        OnProgressUpdated?.Invoke(Trait, praiseCount, GetCurrentCap(), Owner);
    }
    public void OnAllyPraise(PlayerOwner owner)
    {
        // 1. Must be ally
        if (owner != Owner)
            return;

        praiseCount++;
        OnProgressUpdated?.Invoke(Trait, praiseCount, GetCurrentCap(), Owner);
        if (praiseCount >= 4 && CurrentTier < 1 && maxTier >= 1)
            UnlockTier1();

        if (praiseCount >= 8 && CurrentTier < 2 && maxTier >= 2)
            UnlockTier2();

        if (praiseCount >= 12 && CurrentTier < 3 && maxTier >= 3)
            UnlockTier3();
    }
    private void UnlockTier1()
    {
        CurrentTier = 1;
        traitSystem.ActivateEffect(new AvatarTier1Effect(Owner, () => praiseCount));
    }
    private void UnlockTier2()
    {
        CurrentTier = 2;
        traitSystem.ActivateEffect(new AvatarTier2Effect(Owner));
    }
    private void UnlockTier3()
    {
        CurrentTier = 3;
        traitSystem.ActivateEffect(new AvatarTier3Effect(Owner));
    }
    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 4,
            1 => 8,
            2 => 12,
            3 => 9999,
            _ => 9999,
        };
    }
}

public class AvatarTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Avatar;
    public int Tier => 1;

    private readonly PlayerOwner owner;
    private bool used;
    private readonly System.Func<int> getPraiseCount;

    public AvatarTier1Effect(PlayerOwner owner, System.Func<int> getPraiseCount)
    {
        this.owner = owner;
        this.getPraiseCount = getPraiseCount;
    }

    public void OnRegister()
    {
        if (used) return;

         
        GameManager.Instance.ShuffleInDeck(74, owner);
        GameManager.Instance.ShuffleInDeck(78, owner);
        GameManager.Instance.ShuffleInDeck(178, owner);

        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed += OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed += OnCardPlayed;

        used = true;
    }

    public void OnUnregister() {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed -= OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed -= OnCardPlayed;
    }
    private void OnCardPlayed(CardInstance card)
    {
        if (card.Owner != owner)
            return;

        int praiseCount = getPraiseCount();

        if (card.HasKeyword("avatar2"))
            card.ModifyStats(praiseCount, praiseCount);
        else if (card.HasKeyword("avatar"))
            card.ModifyStats(praiseCount / 2, praiseCount / 2);
    }

}
public class AvatarTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Avatar;
    public int Tier => 2;

    private readonly PlayerOwner owner;
    private bool used;
    AllyCardDropArea allyBoard = GameManager.Instance.allyDropArea;
    EnemyCardDropArea enemyBoard = GameManager.Instance.enemyDropArea;
    DeckManager deckManager = GameManager.Instance.deckManager;
    public AvatarTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        if (used) return;

        GameManager.Instance.ShuffleInDeck(74, owner);
        GameManager.Instance.ShuffleInDeck(78, owner);
        GameManager.Instance.ShuffleInDeck(178, owner);

        if (allyBoard != null)
        { allyBoard.OnCardPlayed += OnCardPlayed; deckManager.OnCardDrawn += OnCardDrawn; }

        if (enemyBoard != null)
        { enemyBoard.OnCardPlayed += OnCardPlayed; deckManager.OnCardDrawn += OnCardDrawn; }

        used = true;
    }

    public void OnUnregister()
    {

        if (allyBoard != null)
        { allyBoard.OnCardPlayed -= OnCardPlayed; deckManager.OnCardDrawn -= OnCardDrawn; }

        if (enemyBoard != null)
        { enemyBoard.OnCardPlayed -= OnCardPlayed; deckManager.OnCardDrawn -= OnCardDrawn; }
    }
    public void OnCardDrawn(CardInstance card)
    {
        if (card.Owner != owner)
            return;

        if (card.Data.name == "Aang")
        {
            card.CurrentEffect = "avatar d[draw(1)] d[autoheal(5)] d[autodmg(3)] d[summon(76)] d[summon(76)]";
            card.CurrentEffectText = "Avatar.\nDraw 1, Heal 5 HP to your core, Deal 3 damage to enemy core.\nSummon two 1/1 golemites.";
            card.ParseEffects();
        }
        if (card.Data.name == "Korra")
        {
            card.CurrentEffect = "avatar quickstrike lifesteal protect blessed";
            card.CurrentEffectText = "Avatar.\nQuickstrike Blessed\nProtect Lifesteal.";
        }
        if (card.Data.name == "Wan")
        {
            card.CurrentEffect = "avatar sot[draw(1)] s[buffall(1,0)] eot[summon(76)] b[autoheal(2)]";
            card.CurrentEffectText = "Avatar.\nStart of turn : Draw 1\nStrike : Give all allies +1/+0\nEnd of turn : Summon a golemite\nBerserk : Heal your core for 3";
        }
    }
    private void OnCardPlayed(CardInstance card)
    {
        if (card.Owner != owner)
            return;

        if (card.Data.name == "Aang") {
            card.CurrentEffect = "avatar d[draw(1)] d[autoheal(5)] d[autodmg(3)] d[summon(76)] d[summon(76)]";
            card.CurrentEffectText = "Avatar.\nDraw 1, Heal 5 HP to your core, Deal 3 damage to enemy core.\nSummon two 1/1 golemites.";
            card.ParseEffects(); card.TriggerDeploy();
        }
        if (card.Data.name == "Korra")
        {
            card.CurrentEffect = "avatar quickstrike lifesteal protect blessed";
            card.CurrentEffectText = "Avatar.\nQuickstrike Blessed \nProtect Lifesteal.";
        }
        if (card.Data.name == "Wan")
        {
            card.CurrentEffect = "avatar sot[draw(1)] s[buffall(1,0)] eot[summon(76)] b[autoheal(2)]";
            card.CurrentEffectText = "Avatar.\nStart of turn : Draw 1\nStrike : Give all allies +1/+0\nEnd of turn : Summon a golemite\nBerserk : Heal your core for 3";
        }
    }

}
public class AvatarTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Avatar;
    public int Tier => 3;

    private readonly PlayerOwner owner;

    public AvatarTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        var deckManager = Object.FindFirstObjectByType<DeckManager>();

        deckManager.ReplaceCardsEverywhere(
            owner,
            new Dictionary<int, int>
            {
            { 74, 75 },
            { 78, 79 },
            { 178, 179 },
            }
        );
    }
    public void OnUnregister() { }
}

#endregion
#region Inazuma
public class InazumaProgression : ITraitProgression
{
    private const int Tier3CycleCap = 5;

    public CardData.Trait Trait => CardData.Trait.Inazuma;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int hissatsuCount;
    private int tier3CycleProgress;

    public int CurrentProgress => CurrentTier >= 3 ? tier3CycleProgress : hissatsuCount;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;

    public InazumaProgression(
        PlayerOwner owner,
        int maxTier,
        TraitSystem traitSystem)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
    }

    public void Register()
    {
        Debug.Log($"[Inazuma] Register for {Owner}");
        GameManager.Instance.OnHissatsuPlayed += OnAllyhissatsu;
        PushInitialState();
    }

    public void Unregister()
    {
        GameManager.Instance.OnHissatsuPlayed -= OnAllyhissatsu;
    }

    public void ResetProgression()
    {
        hissatsuCount = 0;
        tier3CycleProgress = 0;
    }

    public void PushInitialState()
    {
        OnProgressUpdated?.Invoke(Trait, hissatsuCount, GetCurrentCap(), Owner);
    }
    public void OnAllyhissatsu(PlayerOwner owner)
    {
        // 1. Must be ally
        if (owner != Owner)
            return;

        hissatsuCount++;
        bool unlockedTier3ThisHissatsu = false;

        if (hissatsuCount >= 3 && CurrentTier < 1 && maxTier >= 1)
            UnlockTier1();

        if (hissatsuCount >= 7 && CurrentTier < 2 && maxTier >= 2)
            UnlockTier2();

        if (hissatsuCount >= 15 && CurrentTier < 3 && maxTier >= 3)
        {
            UnlockTier3();
            unlockedTier3ThisHissatsu = true;
        }

        if (CurrentTier >= 3 && !unlockedTier3ThisHissatsu)
            tier3CycleProgress = (tier3CycleProgress + 1) % Tier3CycleCap;

        OnProgressUpdated?.Invoke(Trait, CurrentProgress, GetCurrentCap(), Owner);
    }
    private void UnlockTier1()
    {
        CurrentTier = 1;
        traitSystem.ActivateEffect(new InazumaTier1Effect(Owner));
    }
    private void UnlockTier2()
    {
        CurrentTier = 2;
        traitSystem.ActivateEffect(new InazumaTier2Effect(Owner));
    }
    private void UnlockTier3()
    {
        CurrentTier = 3;
        tier3CycleProgress = 0;
        traitSystem.ActivateEffect(new InazumaTier3Effect(Owner));
    }
    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 3,
            1 => 7,
            2 => 15,
            3 => Tier3CycleCap,
            _ => 9999,
        };
    }
}

public class InazumaTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Inazuma;
    public int Tier => 1;

    private readonly PlayerOwner owner;

    public InazumaTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        GameManager.Instance.UnlockTensionBar(owner);
        GameManager.Instance.OnCardAttack += HandleCardAttack;
    }

    public void OnUnregister()
    {
        GameManager.Instance.OnCardAttack -= HandleCardAttack;

        if (owner == PlayerOwner.Player)
            GameManager.Instance.fillImageAlly.transform.parent.gameObject.SetActive(false);
        else
            GameManager.Instance.fillImageEnemy.transform.parent.gameObject.SetActive(false);
    }

    private void HandleCardAttack(CardInstance attacker)
    {
        if (attacker == null || attacker.Owner != owner)
            return;

        if (!GameManager.Instance.IsTensionBarVisible(owner))
            return;

        GameManager.Instance.IncreaseFill(10, owner);
    }
}
public class InazumaTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Inazuma;
    public int Tier => 2;

    private readonly PlayerOwner owner;
    private readonly HashSet<int> playedThisTurnID = new();
    private readonly HashSet<string> grantedCombinesThisTurn = new();
    private int hissatsuPowerBonusThisTurn;
    private readonly Dictionary<int, HissatsuSnapshot> hissatsuSnapshots = new();

    private sealed class HissatsuSnapshot
    {
        public CardInstance Card;
        public string BaseEffect;
        public string BaseEffectText;
    }

    public InazumaTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        GameManager.Instance.OnCardPlayed += OnCardPlayed;
        GameManager.Instance.OnSpellPlayed += OnSpellPlayed;
        TurnManager.Instance.OnTurnEnded += TurnEnd;
    }
    public void TurnEnd(PlayerOwner turnOwner)
    {
        RestoreTrackedHissatsuCards();
        playedThisTurnID.Clear();
        grantedCombinesThisTurn.Clear();
        hissatsuPowerBonusThisTurn = 0;
        hissatsuSnapshots.Clear();
    }
    public void OnUnregister()
    {
        GameManager.Instance.OnCardPlayed -= OnCardPlayed;
        GameManager.Instance.OnSpellPlayed -= OnSpellPlayed;
        TurnManager.Instance.OnTurnEnded -= TurnEnd;
    }

    private void OnSpellPlayed(CardInstance spell)
    {
        if (spell == null || spell.Owner != owner || !spell.HasKeyword("hissatsu*"))
            return;

        if (hissatsuPowerBonusThisTurn > 0)
            ApplyTemporaryHissatsuBonus(spell, hissatsuPowerBonusThisTurn);

        hissatsuPowerBonusThisTurn++;
        RefreshOwnerHissatsuCards(spell);
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (card == null || card.Owner != owner || !card.HasKeyword("hissatsu*"))
            return;

        playedThisTurnID.Add(card.Data.id);
        TryGrantCombinationHissatsu();
    }

    private void TryGrantCombinationHissatsu()
    {
        foreach (int hissatsuId in playedThisTurnID)
        {
            CardData hissatsuData = CardDatabase.Instance.GetCardById(hissatsuId);
            if (hissatsuData == null)
                continue;

            List<int> required = hissatsuData.relatedCards ?? new List<int>();
            if (required.Count == 0 || !required.All(id => playedThisTurnID.Contains(id)))
                continue;

            foreach (int combineId in ParseCombineIds(hissatsuData.effect))
            {
                string key = $"{hissatsuId}:{combineId}";
                if (!grantedCombinesThisTurn.Add(key))
                    continue;

                GameManager.Instance.AddCardToHand(owner, combineId);
            }
        }
    }

    private static IEnumerable<int> ParseCombineIds(string effect)
    {
        if (string.IsNullOrWhiteSpace(effect))
            yield break;

        MatchCollection matches = Regex.Matches(effect, @"combine\((\d+)\)", RegexOptions.IgnoreCase);
        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out int combineId))
                yield return combineId;
        }
    }
    private void ApplyTemporaryHissatsuBonus(CardInstance card, int amount)
    {
        if (amount <= 0 || card == null)
            return;

        int key = card.GetInstanceID();
        if (!hissatsuSnapshots.TryGetValue(key, out HissatsuSnapshot snapshot))
        {
            snapshot = new HissatsuSnapshot
            {
                Card = card,
                BaseEffect = card.CurrentEffect,
                BaseEffectText = card.CurrentEffectText,
            };
            hissatsuSnapshots[key] = snapshot;
        }

        card.CurrentEffect = IncreaseNumbersOutsideProtectedPatterns(snapshot.BaseEffect, amount);
        card.CurrentEffectText = IncreaseNumbersOutsideProtectedPatterns(snapshot.BaseEffectText, amount);
        card.ParseEffects();
        GameManager.Instance.ShowChainHissatsu(amount);
        CardView cardView = card.GetComponent<CardView>();
        if (cardView != null)
            cardView.Refresh();
    }
    private void RefreshOwnerHissatsuCards(CardInstance ignoredCard)
    {
        if (hissatsuPowerBonusThisTurn <= 0)
            return;

        foreach (CardInstance card in EnumerateOwnedHissatsuCards())
        {
            if (card == null || ReferenceEquals(card, ignoredCard))
                continue;

            int key = card.GetInstanceID();
            if (!hissatsuSnapshots.TryGetValue(key, out HissatsuSnapshot snapshot))
            {
                snapshot = new HissatsuSnapshot
                {
                    Card = card,
                    BaseEffect = card.CurrentEffect,
                    BaseEffectText = card.CurrentEffectText,
                };
                hissatsuSnapshots[key] = snapshot;
            }

            card.CurrentEffect = IncreaseNumbersOutsideProtectedPatterns(snapshot.BaseEffect, hissatsuPowerBonusThisTurn);
            card.CurrentEffectText = IncreaseNumbersOutsideProtectedPatterns(snapshot.BaseEffectText, hissatsuPowerBonusThisTurn);
            card.ParseEffects();
            card.GetComponent<CardView>()?.Refresh();
        }
    }

    private IEnumerable<CardInstance> EnumerateOwnedHissatsuCards()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
            yield break;

        HandManager hand = owner == PlayerOwner.Player ? gm.allyHand : gm.enemyHand;
        if (hand != null)
        {
            foreach (GameObject go in hand.handCards)
            {
                if (go == null)
                    continue;

                CardInstance card = go.GetComponent<CardInstance>();
                if (card != null && card.Owner == owner && card.HasKeyword("hissatsu*"))
                    yield return card;
            }
        }

        IEnumerable<GameObject> boardCards = owner == PlayerOwner.Player
            ? gm.allyDropArea?.GetCards()
            : gm.enemyDropArea?.GetCards();

        if (boardCards == null)
            yield break;

        foreach (GameObject go in boardCards)
        {
            if (go == null)
                continue;

            CardInstance card = go.GetComponent<CardInstance>();
            if (card != null && card.Owner == owner && card.HasKeyword("hissatsu*"))
                yield return card;
        }
    }

    private void RestoreTrackedHissatsuCards()
    {
        foreach (HissatsuSnapshot snapshot in hissatsuSnapshots.Values)
        {
            if (snapshot?.Card == null)
                continue;

            snapshot.Card.CurrentEffect = snapshot.BaseEffect;
            snapshot.Card.CurrentEffectText = snapshot.BaseEffectText;
            snapshot.Card.ParseEffects();
            snapshot.Card.GetComponent<CardView>()?.Refresh();
        }
    }

    private static string IncreaseNumbersOutsideProtectedPatterns(string source, int amount)
    {
        if (string.IsNullOrWhiteSpace(source) || amount <= 0)
            return source;

        List<(int Start, int End)> protectedRanges = new();
        foreach (Match protectedMatch in Regex.Matches(source, @"hissatsu\*\(\d+\)|combine\(\d+\)", RegexOptions.IgnoreCase))
            protectedRanges.Add((protectedMatch.Index, protectedMatch.Index + protectedMatch.Length));

        return Regex.Replace(source, @"\d+", m =>
        {
            int matchIndex = m.Index;
            bool isProtected = protectedRanges.Any(range => matchIndex >= range.Start && matchIndex < range.End);
            if (isProtected)
                return m.Value;

            return int.TryParse(m.Value, out int parsed)
                ? (parsed + amount).ToString()
                : m.Value;
        });
    }

}
public class InazumaTier3Effect : IDeckTraitEffect
{
    private const int HissatsuForAuraDiscovery = 5;

    public CardData.Trait Trait => CardData.Trait.Inazuma;
    public int Tier => 3;

    private readonly PlayerOwner owner;
    private int tier3HissatsuCount;

    public InazumaTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        GameManager.Instance.OnHissatsuPlayed += OnHissatsuPlayed;
    }

    public void OnUnregister()
    {
        GameManager.Instance.OnHissatsuPlayed -= OnHissatsuPlayed;
    }

    private void OnHissatsuPlayed(PlayerOwner playedOwner)
    {
        if (playedOwner != owner)
            return;

        tier3HissatsuCount++;
        if (tier3HissatsuCount < HissatsuForAuraDiscovery)
            return;

        tier3HissatsuCount = 0;
        GameManager.Instance.DiscoverEffect("aura", "aura", "aura", owner);
    }
}

#endregion
#region Gunner
public class GunnerProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Gunner;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int damageCount;

    public int CurrentProgress => damageCount;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;

    public GunnerProgression(
        PlayerOwner owner,
        int maxTier,
        TraitSystem traitSystem)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
    }

    public void Register()
    {
        Debug.Log($"[Gunner] Register for {Owner}");
        GameManager.Instance.OnDamageCard += OnDamageDealt;
        PushInitialState();
    }

    public void Unregister()
    {
        GameManager.Instance.OnDamageCard -= OnDamageDealt;
    }

    public void ResetProgression()
    {
        damageCount = 0;
    }

    public void PushInitialState()
    {
        OnProgressUpdated?.Invoke(Trait, damageCount, GetCurrentCap(), Owner);
    }
    public void OnDamageDealt(PlayerOwner owner)
    {
        // 1. Must be ally
        if (owner != Owner)
            return;

        damageCount++;
        OnProgressUpdated?.Invoke(Trait, damageCount, GetCurrentCap(), Owner);
        if (damageCount >= 3 && CurrentTier < 1 && maxTier >= 1)
            UnlockTier1();

        if (damageCount >= 6 && CurrentTier < 2 && maxTier >= 2)
            UnlockTier2();

        if (damageCount >= 10 && CurrentTier < 3 && maxTier >= 3)
            UnlockTier3();
    }
    private void UnlockTier1()
    {
        CurrentTier = 1;
        traitSystem.ActivateEffect(new GunnerTier1Effect(Owner));
    }
    private void UnlockTier2()
    {
        CurrentTier = 2;

        traitSystem.DeactivateLowerTiers(CardData.Trait.Gunner, 2);

        traitSystem.ActivateEffect(new GunnerTier2Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked Gunner Tier 2");
    }

    private void UnlockTier3()
    {
        CurrentTier = 3;

        traitSystem.DeactivateLowerTiers(CardData.Trait.Gunner, 3);

        traitSystem.ActivateEffect(
            new GunnerTier3Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked Gunner Tier 3");
    }

    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 3,
            1 => 6,
            2 => 10,
            3 => 9999,
            _ => 9999,
        };
    }
}
public class GunnerTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Gunner;
    public int Tier => 1;

    private readonly PlayerOwner owner;

    public GunnerTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        TurnManager.Instance.OnTurnEnded += OnTurnEnded;

        // 🔑 CRITICAL FIX:
        // If we are already in End phase for this owner, trigger immediately
        if (TurnManager.Instance.CurrentPhase == TurnPhase.End &&
            TurnManager.Instance.CurrentPlayer == owner)
        {
            TriggerGunDamage();
        }
    }

    public void OnUnregister()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnEnded -= OnTurnEnded;
    }

    private void OnTurnEnded(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        TriggerGunDamage();
    }

    private void TriggerGunDamage()
    {
        // Example: 1 damage tick
        GameManager.Instance.StartCoroutine(
            GameManager.Instance.DamageRandomEnemy(andCore: false, ticsDmg: 1, owner)
        );

        Debug.Log($"[GUNNER] Gun fired for {owner}");
    }
}
public class GunnerTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Gunner;
    public int Tier => 2;

    private readonly PlayerOwner owner;

    public GunnerTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        TurnManager.Instance.OnTurnEnded += OnTurnEnded;

        // 🔑 CRITICAL FIX:
        // If we are already in End phase for this owner, trigger immediately
        if (TurnManager.Instance.CurrentPhase == TurnPhase.End &&
            TurnManager.Instance.CurrentPlayer == owner)
        {
            TriggerGunDamage();
        }
    }

    public void OnUnregister()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnEnded -= OnTurnEnded;
    }

    private void OnTurnEnded(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        TriggerGunDamage();
    }

    private void TriggerGunDamage()
    {
        // Example: 2 damage tick
        GameManager.Instance.StartCoroutine(
            GameManager.Instance.DamageRandomEnemy(andCore: true, ticsDmg: 1, owner)
        );

        Debug.Log($"[GUNNER] Gun fired for {owner}");
    }
}
public class GunnerTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Gunner;
    public int Tier => 3;

    private readonly PlayerOwner owner;

    public GunnerTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        TurnManager.Instance.OnTurnEnded += OnTurnEnded;

        // 🔑 CRITICAL FIX:
        // If we are already in End phase for this owner, trigger immediately
        if (TurnManager.Instance.CurrentPhase == TurnPhase.End &&
            TurnManager.Instance.CurrentPlayer == owner)
        {
            TriggerGunDamage();
        }
    }

    public void OnUnregister()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnEnded -= OnTurnEnded;
    }

    private void OnTurnEnded(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        TriggerGunDamage();
    }

    private void TriggerGunDamage()
    {
        // Example: 3 damage tick
        GameManager.Instance.StartCoroutine(
            GameManager.Instance.DamageRandomEnemy(andCore: true, ticsDmg: 3, owner)
        );

        Debug.Log($"[GUNNER] Gun fired for {owner}");
    }
}
#endregion

#region Cozy
public class CozyProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Cozy;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private readonly TraitSystem traitSystem;
    private int cozySkippedAttacks;

    public int CurrentProgress => cozySkippedAttacks;
    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    public CozyProgression(PlayerOwner owner, int maxTier, TraitSystem traitSystem)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
    }

    public void Register()
    {
        TurnManager.Instance.OnTurnEnded += OnTurnEnded;
        PushInitialState();
    }

    public void Unregister()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnEnded -= OnTurnEnded;
    }

    public void ResetProgression()
    {
        cozySkippedAttacks = 0;
    }

    public void PushInitialState()
    {
        OnProgressUpdated?.Invoke(Trait, cozySkippedAttacks, GetCurrentCap(), Owner);
    }

    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 3,
            1 => 6,
            2 => 10,
            _ => 9999
        };
    }

    private void OnTurnEnded(PlayerOwner turnOwner)
    {
        if (turnOwner != Owner)
            return;

        GameManager gm = GameManager.Instance;
        if (gm == null)
            return;

        List<CardInstance> unitsThatCouldAttackButDidNot = GetUnitsThatCouldAttackButDidNot(gm, Owner);
        if (unitsThatCouldAttackButDidNot.Count <= 0)
            return;

        cozySkippedAttacks += unitsThatCouldAttackButDidNot.Count;
        OnProgressUpdated?.Invoke(Trait, cozySkippedAttacks, GetCurrentCap(), Owner);

        if (cozySkippedAttacks >= 3 && CurrentTier < 1 && maxTier >= 1)
            UnlockTier1();
        if (cozySkippedAttacks >= 6 && CurrentTier < 2 && maxTier >= 2)
            UnlockTier2();
        if (cozySkippedAttacks >= 10 && CurrentTier < 3 && maxTier >= 3)
            UnlockTier3();
    }

    private void UnlockTier1()
    {
        CurrentTier = 1;
        traitSystem.ActivateEffect(new CozyTier1Effect(Owner));
    }

    private void UnlockTier2()
    {
        CurrentTier = 2;
        traitSystem.ActivateEffect(new CozyTier2Effect(Owner));
    }

    private void UnlockTier3()
    {
        CurrentTier = 3;
        traitSystem.ActivateEffect(new CozyTier3Effect(Owner));
    }

    private static List<CardInstance> GetUnitsThatCouldAttackButDidNot(GameManager gm, PlayerOwner owner)
    {
        IEnumerable<GameObject> cards =
            owner == PlayerOwner.Player ? gm.allyDropArea?.GetCards() : gm.enemyDropArea?.GetCards();

        List<CardInstance> result = new();
        if (cards == null)
            return result;

        foreach (GameObject go in cards)
        {
            CardInstance card = go?.GetComponent<CardInstance>();
            if (card == null || card.Owner != owner || card.IsDead || card.IsAsleep || card.CurrentZone != CardZone.Board)
                continue;

            if (CouldHaveAttackedThisTurn(gm, card) && !card.HasAttackedThisTurn)
                result.Add(card);
        }

        return result;
    }

    private static bool CouldHaveAttackedThisTurn(GameManager gm, CardInstance card)
    {
        if (gm == null || card == null)
            return false;

        if (card.CurrentAttack <= 0 || card.IsAsleep)
            return false;

        if (card.IsSummoningSick)
        {
            bool canAttackUnitOnSummon = card.CanAttackUnitOnSummon();
            bool canAttackCoreOnSummon = card.CanAttackCoreOnSummon();
            if (!canAttackUnitOnSummon && !canAttackCoreOnSummon)
                return false;
        }

        return gm.GetValidTargets(card).Count > 0;
    }
}

public class CozyTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Cozy;
    public int Tier => 1;
    private readonly PlayerOwner owner;

    public CozyTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        TurnManager.Instance.OnTurnEnded += OnTurnEnded;
    }

    public void OnUnregister()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnEnded -= OnTurnEnded;
    }

    private void OnTurnEnded(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        foreach (CardInstance card in GetUnitsThatCouldAttackButDidNot(owner))
        {
            int hpGain = card.HasTrait("Cozy") ? 2 : 1;
            card.ModifyStats(0, hpGain);
        }
    }

    public static List<CardInstance> GetUnitsThatCouldAttackButDidNot(PlayerOwner owner)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
            return new List<CardInstance>();

        IEnumerable<GameObject> cards =
            owner == PlayerOwner.Player ? gm.allyDropArea?.GetCards() : gm.enemyDropArea?.GetCards();

        List<CardInstance> result = new();
        if (cards == null)
            return result;

        foreach (GameObject go in cards)
        {
            CardInstance card = go?.GetComponent<CardInstance>();
            if (card == null || card.Owner != owner || card.IsDead || card.CurrentZone != CardZone.Board)
                continue;

            if (CouldHaveAttackedThisTurn(gm, card) && !card.HasAttackedThisTurn)
                result.Add(card);
        }

        return result;
    }

    private static bool CouldHaveAttackedThisTurn(GameManager gm, CardInstance card)
    {
        if (gm == null || card == null)
            return false;

        if (card.CurrentAttack <= 0 || card.IsAsleep)
            return false;

        if (card.IsSummoningSick)
        {
            bool canAttackUnitOnSummon = card.CanAttackUnitOnSummon();
            bool canAttackCoreOnSummon = card.CanAttackCoreOnSummon();
            if (!canAttackUnitOnSummon && !canAttackCoreOnSummon)
                return false;
        }

        return gm.GetValidTargets(card).Count > 0;
    }
}

public class CozyTier2Effect : IDeckTraitEffect
{
    public int Tier => 2;
    public CardData.Trait Trait => CardData.Trait.Cozy;
    private readonly PlayerOwner owner;

    public CozyTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        TurnManager.Instance.OnTurnEnded += OnTurnEnded;
    }

    public void OnUnregister()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnEnded -= OnTurnEnded;
    }

    private void OnTurnEnded(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        List<CardInstance> skippedUnits = CozyTier1Effect.GetUnitsThatCouldAttackButDidNot(owner);
        int triggers = skippedUnits.Count / 2;
        if (triggers <= 0)
            return;

        GameManager gm = GameManager.Instance;
        if (gm == null)
            return;

        DeckManager deck = gm.deckManager;
        HandManager hand = owner == PlayerOwner.Player ? deck.handManager : deck.handManagerEnemy;
        CoreInstance core = owner == PlayerOwner.Player ? gm.PlayerCore : gm.EnemyCore;

        for (int i = 0; i < triggers; i++)
        {
            DiscountRandomCardInHand(hand);
            core?.Heal(2);
        }
    }

    private static void DiscountRandomCardInHand(HandManager hand)
    {
        if (hand == null || hand.handCards.Count == 0)
            return;

        int index = Random.Range(0, hand.handCards.Count);
        CardInstance card = hand.handCards[index]?.GetComponent<CardInstance>();
        if (card == null)
            return;

        card.AddTemporaryManaModifier(-1);
    }
}

public class CozyTier3Effect : IDeckTraitEffect
{
    public int Tier => 3;
    public CardData.Trait Trait => CardData.Trait.Cozy;
    private readonly PlayerOwner owner;

    public CozyTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        TurnManager.Instance.OnTurnEnded += OnTurnEnded;
    }

    public void OnUnregister()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnEnded -= OnTurnEnded;
    }

    private void OnTurnEnded(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        foreach (CardInstance card in CozyTier1Effect.GetUnitsThatCouldAttackButDidNot(owner))
        {
            if (string.IsNullOrWhiteSpace(card.CurrentEffect))
                card.CurrentEffect = "blessed";
            else if (!card.HasKeyword("blessed"))
                card.CurrentEffect += " blessed";

            card.GetComponent<CardView>()?.UpdateMode();
        }
    }
}
#endregion

#region Swordsman
public class SwordsmanProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Swordsman;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private readonly TraitSystem traitSystem;
    private int appliedbleeds;

    public int CurrentProgress => appliedbleeds;
    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    public SwordsmanProgression(PlayerOwner owner, int maxTier, TraitSystem traitSystem)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
    }

    public void Register()
    {
        GameManager.Instance.OnBleedApplied += OnApplyBleed;
        PushInitialState();
    }

    public void Unregister()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnBleedApplied -= OnApplyBleed;
    }

    public void ResetProgression()
    {
        appliedbleeds = 0;
    }

    public void PushInitialState()
    {
        OnProgressUpdated?.Invoke(Trait, appliedbleeds, GetCurrentCap(), Owner);
    }

    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 3,
            1 => 6,
            2 => 10,
            _ => 9999
        };
    }

    private void OnApplyBleed(PlayerOwner owner)
    {
        if (owner != Owner)
            return;

        appliedbleeds++;
        OnProgressUpdated?.Invoke(Trait, appliedbleeds, GetCurrentCap(), Owner);

        if (appliedbleeds >= 3 && CurrentTier < 1 && maxTier >= 1)
            UnlockTier1();
        if (appliedbleeds >= 6 && CurrentTier < 2 && maxTier >= 2)
            UnlockTier2();
        if (appliedbleeds >= 10 && CurrentTier < 3 && maxTier >= 3)
            UnlockTier3();
    }

    private void UnlockTier1()
    {
        CurrentTier = 1;
        traitSystem.ActivateEffect(new SwordsmanTier1Effect(Owner));
    }

    private void UnlockTier2()
    {
        CurrentTier = 2;
        traitSystem.ActivateEffect(new SwordsmanTier2Effect(Owner));
    }

    private void UnlockTier3()
    {
        CurrentTier = 3;
        traitSystem.ActivateEffect(new SwordsmanTier3Effect(Owner));
    }
}

public class SwordsmanTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Swordsman;
    public int Tier => 1;
    private readonly PlayerOwner owner;

    public SwordsmanTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister() { }
    public void OnUnregister() { }
}

public class SwordsmanTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Swordsman;
    public int Tier => 2;
    private readonly PlayerOwner owner;

    public SwordsmanTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister() { }
    public void OnUnregister() { }
}

public class SwordsmanTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Swordsman;
    public int Tier => 3;
    private readonly PlayerOwner owner;

    public SwordsmanTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister() { }
    public void OnUnregister() { }
}
#endregion
