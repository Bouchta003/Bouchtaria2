using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening.Core.Easing;
using UnityEngine;
using UnityEngine.Rendering;

public class DeckManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public HandManager handManager;
    [SerializeField] public HandManager handManagerEnemy;
    [SerializeField] private TraitsDetection traitsDetection;

    [Header("Debug / Test")]
    [SerializeField] private PlayerOwner deckOwner;
    public Dictionary<PlayerOwner, Queue<CardData>> decks = new Dictionary<PlayerOwner, Queue<CardData>>();

    public Dictionary<CardData.Trait, int> AllyTraitsUnlockable;
    public Dictionary<CardData.Trait, int> EnemyTraitsUnlockable;
    private bool isDrawingPlayer = false;
    private bool isDrawingEnemy = false;
    public event System.Action<CardInstance> OnCardDrawn;

    public int TruthEffect = -1;
    public int IdealEffect = -1;
    public void DetectUnlockableTraits()
    {
        AllyTraitsUnlockable =
            traitsDetection.RetrieveTraitTiersFromDeck(
                decks[PlayerOwner.Player],
                PlayerOwner.Player
            );

        EnemyTraitsUnlockable =
            traitsDetection.RetrieveTraitTiersFromDeck(
                decks[PlayerOwner.Enemy],
                PlayerOwner.Enemy
            );

        Debug.Log($"Traits detected: Player={AllyTraitsUnlockable.Count}, Enemy={EnemyTraitsUnlockable.Count}");
    }
    public void RefreshUnlockableTraitsForOwner(PlayerOwner owner)
    {
        Dictionary<CardData.Trait, int> refreshed =
            traitsDetection.RetrieveTraitTiersFromDeck(
                decks[owner],
                owner
            );

        if (owner == PlayerOwner.Player)
            AllyTraitsUnlockable = refreshed;
        else
            EnemyTraitsUnlockable = refreshed;
    }

    public void InitializeDecks()
    {
        InitializeDeck(PlayerOwner.Player);
        InitializeDeck(PlayerOwner.Enemy);
    }
    private bool RefillDeckForFatigue(PlayerOwner owner)
    {
        List<CardData> refillList =
            owner == PlayerOwner.Player
            ? GetDeckForPlayer()
            : GetDeckForEnemy();

        if (refillList == null || refillList.Count == 0)
        {
            Debug.LogError($"Failed to refill deck for {owner}");
            return false;
        }

        Shuffle(refillList);

        Queue<CardData> newDeck = new Queue<CardData>();

        foreach (CardData card in refillList)
        {
            if (card == null)
                continue;

            newDeck.Enqueue(card);
        }

        decks[owner] = newDeck;

        Debug.Log($"{owner} deck refilled with {newDeck.Count} cards.");

        return newDeck.Count > 0;
    }
    private void InitializeDeck(PlayerOwner owner)
    {
        List<CardData> deckList = new List<CardData>();
        if (owner == PlayerOwner.Player)
            deckList = GetDeckForPlayer();
        else
            deckList = GetDeckForEnemy();

        Shuffle(deckList);

        Queue<CardData> deck = new Queue<CardData>();

        foreach (var card in deckList)
        {
            deck.Enqueue(card);
        }

        decks[owner] = deck;

        Debug.Log($"{owner} deck initialized with {deck.Count} cards.");
    }
    private void OnEnable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted += HandleTurnStart;
    }
    public bool HasMinionInDeck(PlayerOwner owner)
    {
        Queue<CardData> deck = decks[owner];
        if (deck == null || deck.Count == 0) return false;
        return deck.Any(card => {
            CardData data = CardDatabase.Instance.GetCardById(card.id);
            return data != null && data.cardType.Equals("minion", StringComparison.OrdinalIgnoreCase);
        });
    }
    private void OnDisable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted -= HandleTurnStart;
    }
        private void HandleTurnStart(PlayerOwner owner)
    {
        //Normal start of turn card draw
        if ((owner == PlayerOwner.Player && !TurnManager.Instance.PlayerSkipsNextDraw) || (owner != PlayerOwner.Player && !TurnManager.Instance.EnemySkipsNextDraw))
        {
            StartCoroutine(Draw(1, owner));
            if (GameManager.Instance.OwnerHasTrait(owner, CardData.Trait.Neutral, 3))
            {
                StartCoroutine(Draw(1, owner));
            }
        }
        else
        {
            switch (owner)
            {
                case PlayerOwner.Player: TurnManager.Instance.PlayerSkipsNextDraw = false;break;
                case PlayerOwner.Enemy: TurnManager.Instance.EnemySkipsNextDraw = false;break;
            }
        }
    }

    public IEnumerator Draw(int count, PlayerOwner owner)
    {
        // Prevent simultaneous draw corruption
        if (owner == PlayerOwner.Player)
        {
            if (isDrawingPlayer)
                yield break;

            isDrawingPlayer = true;
        }
        else
        {
            if (isDrawingEnemy)
                yield break;

            isDrawingEnemy = true;
        }

        try
        {
            for (int i = 0; i < count; i++)
            {
                TryDrawCard(owner);

                yield return new WaitForSeconds(0.2f);
            }
        }
        finally
        {
            if (owner == PlayerOwner.Player)
                isDrawingPlayer = false;
            else
                isDrawingEnemy = false;
        }
    }
    public IEnumerator DrawEffect(string effect, PlayerOwner owner)
    {
        DrawNextCardWithEffect(owner, effect+"*");
        yield return new WaitForSeconds(0.2f);
    }

    public bool TrySummonRandomMinionFromDeck(PlayerOwner owner, bool isDeploy = false)
    {
        return TrySummonRandomMinionFromDeck(owner, card => true,isDeploy);
    }

    public bool TrySummonRandomMinionFromDeckByMaxMana(PlayerOwner owner, int maxMana)
    {
        return TrySummonRandomMinionFromDeck(owner, card => card.manaCost <= maxMana);
    }

    public bool TrySummonRandomMinionFromDeckByEffect(PlayerOwner owner, string effectSearch)
    {
        if (string.IsNullOrWhiteSpace(effectSearch))
            return false;

        return TrySummonRandomMinionFromDeck(owner, card =>
            !string.IsNullOrEmpty(card.effect)
            && card.effect.IndexOf(effectSearch, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    public bool TrySummonRandomMinionFromDeckByTrait(PlayerOwner owner, string traitSearch)
    {
        if (string.IsNullOrWhiteSpace(traitSearch))
            return false;

        return TrySummonRandomMinionFromDeck(owner, card =>
        {
            if (card.traits == null)
                return false;

            for (int i = 0; i < card.traits.Count; i++)
            {
                if (string.Equals(card.traits[i], traitSearch, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        });
    }

    private bool TrySummonRandomMinionFromDeck(PlayerOwner owner, Func<CardData, bool> extraPredicate, bool isDeploy = false)
    {
        if (!decks.TryGetValue(owner, out Queue<CardData> deck))
        {
            Debug.LogError($"Tried to summon from deck for {owner}, but no deck is initialized.");
            return false;
        }

        if (deck.Count == 0)
            return false;

        List<CardData> deckSnapshot = new List<CardData>(deck);
        List<CardData> candidates = new List<CardData>();

        for (int i = 0; i < deckSnapshot.Count; i++)
        {
            CardData card = deckSnapshot[i];

            if (card == null || card.cardType != "minion")
                continue;

            if (extraPredicate != null && !extraPredicate(card))
                continue;

            candidates.Add(card);
        }

        if (candidates.Count == 0)
            return false;

        CardData selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        if (selected == null)
            return false;

        Queue<CardData> rebuiltDeck = new Queue<CardData>(deckSnapshot.Count);
        bool removed = false;

        for (int i = 0; i < deckSnapshot.Count; i++)
        {
            CardData card = deckSnapshot[i];

            if (!removed && card == selected)
            {
                removed = true;
                continue;
            }

            rebuiltDeck.Enqueue(card);
        }

        if (!removed)
            return false;

        if (!GameManager.Instance.TrySummonForOwnerSafe(owner, selected.id, isDeploy))
            return false;

        decks[owner] = rebuiltDeck;
        return true;
    }

    private void DrawNextCardWithEffect(PlayerOwner owner, string effectSearch)
    {
        if (!decks.TryGetValue(owner, out Queue<CardData> deck))
        {
            Debug.LogError($"Tried to draw for {owner}, but no deck is initialized.");
            return;
        }

        HandManager hand = owner == PlayerOwner.Player
            ? handManager
            : handManagerEnemy;

        if (hand.handCards.Count >= hand.maxHandSize)
        {
            Debug.Log($"{owner} hand is full.");
            return;
        }

        CardData foundCard = null;
        int originalCount = deck.Count;

        // We temporarily cycle through the deck
        for (int i = 0; i < originalCount; i++)
        {
            if (deck.Count == 0)
            {
                Debug.LogError("Attempted dequeue on empty deck.");
                return;
            }
            CardData top = deck.Dequeue();

            if (foundCard == null &&
                !string.IsNullOrEmpty(top.effect) &&
                top.effect.Contains(effectSearch))
            {
                foundCard = top;
                continue; // don't re-enqueue this one
            }

            deck.Enqueue(top);
        }

        if (foundCard == null)
        {
            Debug.Log($"No card found with effect containing '{effectSearch}'");
            return;
        }

        // Use your existing draw logic
        CardInstance card = CardFactory.Instance.CreateCard(foundCard, owner);

        switch (IdealEffect)
        {
            case -1: break;
            case 0:
                if (owner == PlayerOwner.Player)
                    card.AddTemporaryManaModifier(-2);
                break;
            case 1:
                if (owner == PlayerOwner.Enemy)
                    card.AddTemporaryManaModifier(-2);
                break;
            case 2:
                card.AddTemporaryManaModifier(-2);
                break;
        }

        switch (TruthEffect)
        {
            case -1: break;
            case 0:
                if (owner == PlayerOwner.Enemy)
                    card.AddTemporaryManaModifier(2);
                break;
            case 1:
                if (owner == PlayerOwner.Player)
                    card.AddTemporaryManaModifier(2);
                break;
            case 2:
                card.AddTemporaryManaModifier(2);
                break;
        }

        card.SetZone(CardZone.Hand);
        hand.AddCard(card.gameObject);
        hand.UpdateCardPositions();
        OnCardDrawn?.Invoke(card);
    }
    private bool TryDrawCard(PlayerOwner owner)
    {
        if (!decks.TryGetValue(owner, out Queue<CardData> deck))
        {
            Debug.LogError($"Tried to draw for {owner}, but no deck is initialized.");
            return false;
        }

        if (deck == null)
        {
            Debug.LogError($"Deck is null for {owner}");
            return false;
        }

        HandManager hand = owner == PlayerOwner.Player
            ? handManager
            : handManagerEnemy;

        if (deck.Count == 0)
        {
            // Increase fatigue
            if (owner == PlayerOwner.Player)
                GameManager.Instance.PlayerFatigue++;
            else
                GameManager.Instance.EnemyFatigue++;

            GameManager.Instance.DisplayFatigue(owner);

            int fatigueValue =
                owner == PlayerOwner.Player
                ? GameManager.Instance.PlayerFatigue
                : GameManager.Instance.EnemyFatigue;

            // Fatigue damage
            if (owner == PlayerOwner.Player)
                GameManager.Instance.PlayerCore.TakeDamage(fatigueValue);
            else
                GameManager.Instance.EnemyCore.TakeDamage(fatigueValue);

            Debug.Log($"{owner} fatigued and refills deck.");

            // SAFE REFILL
            bool refillSuccess = RefillDeckForFatigue(owner);

            if (!refillSuccess)
            {
                Debug.LogError($"Could not refill deck for {owner}");
                return false;
            }

            // IMPORTANT:
            // Re-fetch the NEW queue reference
            deck = decks[owner];

            // SAFETY CHECK
            if (deck == null || deck.Count == 0)
            {
                Debug.LogError($"Deck still empty after refill for {owner}");
                return false;
            }
        }

        // Burn if hand full
        if (hand.handCards.Count >= hand.maxHandSize)
        {
            if (deck.Count == 0)
            {
                Debug.LogError($"Attempted burn draw with empty deck for {owner}");
                return false;
            }

            CardData burned = deck.Dequeue();

            Debug.Log($"{owner} burned {burned?.name}");

            return true;
        }

        // SAFE DEQUEUE
        if (deck.Count == 0)
        {
            Debug.LogError($"Deck became empty unexpectedly for {owner}");
            return false;
        }

        CardData data = deck.Dequeue();

        if (data == null)
        {
            Debug.LogError($"Null card drawn for {owner}");
            return false;
        }

        CardInstance card =
            CardFactory.Instance.CreateCard(data, owner);

        if (card == null)
        {
            Debug.LogError($"Failed to create card instance for {data.name}");
            return false;
        }

        // IDEAL EFFECT
        switch (IdealEffect)
        {
            case 0:
                if (owner == PlayerOwner.Player)
                    card.AddTemporaryManaModifier(-2);
                break;

            case 1:
                if (owner == PlayerOwner.Enemy)
                    card.AddTemporaryManaModifier(-2);
                break;

            case 2:
                card.AddTemporaryManaModifier(-2);
                break;
        }

        // TRUTH EFFECT
        switch (TruthEffect)
        {
            case 0:
                if (owner == PlayerOwner.Enemy)
                    card.AddTemporaryManaModifier(2);
                break;

            case 1:
                if (owner == PlayerOwner.Player)
                    card.AddTemporaryManaModifier(2);
                break;

            case 2:
                card.AddTemporaryManaModifier(2);
                break;
        }

        card.SetZone(CardZone.Hand);

        hand.AddCard(card.gameObject);

        SFXManager.Instance.PlayRandomSFXClip(
            GameManager.Instance.drawSFX,
            transform,
            1f);

        hand.UpdateCardPositions();

        OnCardDrawn?.Invoke(card);

        // FATIGUE DAMAGE BONUS
        int fatigue =
            owner == PlayerOwner.Player
            ? GameManager.Instance.PlayerFatigue
            : GameManager.Instance.EnemyFatigue;

        if (fatigue > 0)
        {
            int dmg = card.CurrentManaCost * fatigue;

            if (owner == PlayerOwner.Player)
                GameManager.Instance.PlayerCore.TakeDamage(dmg);
            else
                GameManager.Instance.EnemyCore.TakeDamage(dmg);
        }

        return true;
    }
    private void DrawCard(PlayerOwner owner)
    {
        if (!decks.TryGetValue(owner, out Queue<CardData> deck))
        {
            Debug.LogError($"Tried to draw for {owner}, but no deck is initialized for that owner.");
            return;
        }

        HandManager hand = owner == PlayerOwner.Player
            ? handManager
            : handManagerEnemy;

        if (deck.Count == 0)
        {
            InitializeDeck(owner);
            deck = decks[owner]; // ✅ re-fetch the newly created queue
            if (owner == PlayerOwner.Player) GameManager.Instance.PlayerFatigue++;
            else GameManager.Instance.EnemyFatigue++;
            GameManager.Instance.DisplayFatigue(owner);

            if (deck.Count == 0) // still empty = all IDs were invalid
            {
                Debug.LogError($"[Deck] {owner} deck is empty even after re-init. No valid cards.");
                return;
            }
        }

        if (hand.handCards.Count >= hand.maxHandSize)
        {
            deck.Dequeue(); // burn
            Debug.Log($"{owner} hand is full.");
            return;
        }

        CardData data = deck.Dequeue();

        CardInstance card =
            CardFactory.Instance.CreateCard(data, owner);

        switch (IdealEffect)
        {
            case -1: break;
            case 0://Only Player has Truth
                if (owner == PlayerOwner.Player)
                    card.AddTemporaryManaModifier(-2);
                break;
            case 1://Only Enemy has Truth
                if (owner == PlayerOwner.Enemy)
                    card.AddTemporaryManaModifier(-2);
                break;
            case 2:
                card.AddTemporaryManaModifier(-2);
                break;
        }
        switch (TruthEffect)
        {
            case -1: break;
            case 0://Only Player has Truth
                if (owner == PlayerOwner.Enemy)
                    card.AddTemporaryManaModifier(2);
                break;
            case 1://Only Enemy has Truth
                if (owner == PlayerOwner.Player)
                    card.AddTemporaryManaModifier(2);
                break;
            case 2:
                card.AddTemporaryManaModifier(2);
                break;
        }

        card.SetZone(CardZone.Hand);
        hand.AddCard(card.gameObject);
        SFXManager.Instance.PlayRandomSFXClip(GameManager.Instance.drawSFX, transform, 1f);
        hand.UpdateCardPositions();
        OnCardDrawn?.Invoke(card);
        //Fatigue effect
        if (owner == PlayerOwner.Player) GameManager.Instance.PlayerCore.TakeDamage(card.CurrentManaCost*GameManager.Instance.PlayerFatigue);
        else GameManager.Instance.EnemyCore.TakeDamage(card.CurrentManaCost * GameManager.Instance.EnemyFatigue);
    }
    public void ReplaceCardsInDeck(PlayerOwner owner,Dictionary<int, int> replacements)
    {
        Queue<CardData> deck = decks[owner];
        int count = deck.Count;

        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0)
            {
                Debug.LogError("Attempted dequeue on empty deck.");
                return;
            }
            CardData card = deck.Dequeue();

            if (replacements.TryGetValue(card.id, out int newId))
            {
                card = CardDatabase.Instance.GetCardById(newId);
            }

            deck.Enqueue(card);
        }
    }
    /// <summary>
    /// Verifies if the deck has any duplicate cards.
    /// Returns false if any card appears more than once, true otherwise.
    /// </summary>
    public bool HasNoDuplicates(PlayerOwner owner)
    {
        if (!decks.TryGetValue(owner, out Queue<CardData> deck))
        {
            Debug.LogError($"Attempted to check duplicates for {owner}, but no deck is initialized.");
            return false;
        }

        if (deck.Count == 0)
            return true;

        // Convert queue to list for analysis
        List<CardData> deckSnapshot = new List<CardData>(deck);
        Dictionary<int, int> cardCounts = new Dictionary<int, int>();

        // Count each card ID
        foreach (CardData card in deckSnapshot)
        {
            if (card == null)
                continue;

            if (cardCounts.ContainsKey(card.id))
                cardCounts[card.id]++;
            else
                cardCounts[card.id] = 1;
        }

        // Check if any card appears more than once
        foreach (var count in cardCounts.Values)
        {
            if (count > 1)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Verifies if the deck is "pure" (has only one active trait, regardless of tier).
    /// Returns false if more than 1 distinct trait is active, true otherwise.
    /// </summary>
    public bool HasPureDeck(PlayerOwner owner)
    {
        Dictionary<CardData.Trait, int> traitsUnlockable =
            owner == PlayerOwner.Player
                ? AllyTraitsUnlockable
                : EnemyTraitsUnlockable;

        if (traitsUnlockable == null || traitsUnlockable.Count == 0)
            return true; // No traits = pure

        // If more than 1 distinct trait is unlocked, deck is not pure
        return traitsUnlockable.Count == 1;
    }

    /// <summary>
    /// Verifies if the deck is "polyvalent" (has at least 3 active traits).
    /// Returns true if 3 or more distinct traits are active, false otherwise.
    /// </summary>
    public bool HasPolyvalentDeck(PlayerOwner owner)
    {
        Dictionary<CardData.Trait, int> traitsUnlockable =
            owner == PlayerOwner.Player
                ? AllyTraitsUnlockable
                : EnemyTraitsUnlockable;

        if (traitsUnlockable == null || traitsUnlockable.Count == 0)
            return false;

        // Return true if 3 or more traits are present
        return traitsUnlockable.Count >= 3;
    }
    public void ReplaceCardsEverywhere(
    PlayerOwner owner,Dictionary<int, int> replacements)
    {
        ReplaceCardsInDeck(owner, replacements);
        ReplaceCardsInHand(owner, replacements);
    }
    public void ReplaceCardsInHand(
    PlayerOwner owner,
    Dictionary<int, int> replacements
)
    {
        HandManager hand =
            owner == PlayerOwner.Player
                ? handManager
                : handManagerEnemy;

        // Iterate over COPY (hand mutates)
        foreach (GameObject go in new List<GameObject>(hand.handCards))
        {
            CardInstance inst = go.GetComponent<CardInstance>();
            if (inst == null)
                continue;

            if (!replacements.TryGetValue(inst.Data.id, out int newId))
                continue;

            int oldSorting = go.GetComponent<SortingGroup>()?.sortingOrder ?? 0;

            // Remove old card
            hand.RemoveCardFromHand(go);
            Destroy(go);

            // Create replacement
            CardData newData = CardDatabase.Instance.GetCardById(newId);
            CardInstance newCard =
                CardFactory.Instance.CreateCard(newData, owner);

            newCard.SetZone(CardZone.Hand);
            hand.AddCard(newCard.gameObject);

            SortingGroup sg = newCard.GetComponent<SortingGroup>();
            if (sg != null)
                sg.sortingOrder = oldSorting;
        }

        hand.UpdateCardPositions();
    }

    private void Shuffle(List<CardData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[rnd]) = (list[rnd], list[i]);
        }
    }
    public void Shuffle(Queue<CardData> deck)
    {
        if (deck == null || deck.Count <= 1)
            return;

        // Convert queue to list
        List<CardData> list = new List<CardData>(deck);

        // Reuse existing shuffle logic
        Shuffle(list);

        // Rebuild queue
        deck.Clear();
        foreach (var card in list)
        {
            deck.Enqueue(card);
        }
    }

    private List<CardData> GetDeckForPlayer()
    {
        List<CardData> deck = new();

        if (DeckSelectionCache.SelectedPlayerDeck != null)
        {
            foreach (int id in DeckSelectionCache.SelectedPlayerDeck)
            {
                CardData card = CardDatabase.Instance.GetCardById(id);
                if (card == null)
                {
                    Debug.LogWarning($"[Deck] Player: card ID {id} not found in database, skipping.");
                    continue;
                }
                deck.Add(card);
            }

            if (deck.Count == 0)
                Debug.LogError("[Deck] Player deck built with 0 valid cards!");

            return deck;
        }
        // Fallback (editor / debug)
        ErrorPopup.Show("Couldn't load deck");
        foreach (CardData card in CardDatabase.Instance.Cards.Values)
        {
            if (card.traits.Contains("MonsterHunter") && card.packable)
                deck.Add(card);
        }
        Debug.LogWarning("Selected default deck");
        return deck;
    }
    private List<CardData> GetDeckForEnemy()
    {
        List<CardData> deck = new();

        if (DeckSelectionCache.SelectedEnemyDeck != null)
        {
            foreach (int id in DeckSelectionCache.SelectedEnemyDeck)
            {
                CardData card = CardDatabase.Instance.GetCardById(id);
                if (card == null)
                {
                    Debug.LogWarning($"[Deck] Player: card ID {id} not found in database, skipping.");
                    continue;
                }
                deck.Add(card);
            }
            Debug.Log($"Selected enemy's deck from db");
            return deck;
        }

        // Fallback (editor / debug)
        foreach (CardData card in CardDatabase.Instance.Cards.Values)
        {
            if (card.traits.Contains("MonsterHunter") && card.packable)
                deck.Add(card);
        }
        Debug.LogWarning("Selected default deck");
        return deck;
    }
}
