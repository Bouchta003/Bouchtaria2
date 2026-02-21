using System;
using System.Collections;
using System.Collections.Generic;
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

    public void InitializeDecks()
    {
        InitializeDeck(PlayerOwner.Player);
        InitializeDeck(PlayerOwner.Enemy);
    }

    private void InitializeDeck(PlayerOwner owner)
    {
        List<CardData> deckList = new List<CardData>();
        if (owner == PlayerOwner.Player)
            deckList = GetTestDeckForPlayer();
        else
            deckList = GetTestDeckForEnemy();

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
        for (int i = 0; i < count; i++)
        {
            DrawCard(owner);
            yield return new WaitForSeconds(0.2f);
        }
    }
    public IEnumerator DrawEffect(string effect, PlayerOwner owner)
    {
        DrawNextCardWithEffect(owner, effect+"*");
        yield return new WaitForSeconds(0.2f);
    }
    private void DrawNextCardWithEffect(PlayerOwner owner, string effectSearch)
    {
        if (!decks.TryGetValue(owner, out Queue<CardData> deck))
        {
            Debug.LogError($"Tried to draw for {owner}, but no deck is initialized.");
            return;
        }

        if (deck.Count == 0)
        {
            Debug.Log($"{owner} deck is empty.");
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
            Debug.Log($"{owner} deck is empty.");
            return;
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
        hand.UpdateCardPositions();
        OnCardDrawn?.Invoke(card);
    }
    public void ReplaceCardsInDeck(PlayerOwner owner,Dictionary<int, int> replacements)
    {
        Queue<CardData> deck = decks[owner];
        int count = deck.Count;

        for (int i = 0; i < count; i++)
        {
            CardData card = deck.Dequeue();

            if (replacements.TryGetValue(card.id, out int newId))
            {
                card = CardDatabase.Instance.GetCardById(newId);
            }

            deck.Enqueue(card);
        }
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

    private List<CardData> GetTestDeckForPlayer()
    {
        List<CardData> deck = new();

        if (DeckSelectionCache.SelectedPlayerDeck != null)
        {
            foreach (int id in DeckSelectionCache.SelectedPlayerDeck)
            {
                deck.Add(CardDatabase.Instance.GetCardById(id));
            }
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
    private List<CardData> GetTestDeckForEnemy()
    {
        List<CardData> deck = new();

        if (DeckSelectionCache.SelectedEnemyDeck != null)
        {
            foreach (int id in DeckSelectionCache.SelectedEnemyDeck)
            {
                deck.Add(CardDatabase.Instance.GetCardById(id));
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
