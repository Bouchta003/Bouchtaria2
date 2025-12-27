using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandManager handManager;
    [SerializeField] private HandManager handManagerEnemy;
    [SerializeField] private TraitsDetection traitsDetection;

    [Header("Debug / Test")]
    [SerializeField] private PlayerOwner deckOwner;
    private Dictionary<PlayerOwner, Queue<CardData>> decks
    = new Dictionary<PlayerOwner, Queue<CardData>>();

    private void Start()
    {
        traitsDetection.RetrieveTraitTiersFromDeck(decks[PlayerOwner.Player], PlayerOwner.Player);
        traitsDetection.RetrieveTraitTiersFromDeck(decks[PlayerOwner.Enemy], PlayerOwner.Enemy);

        Draw(3, PlayerOwner.Player);
        Draw(3, PlayerOwner.Enemy);
    }
    public void InitializeDecks()
    {
        InitializeDeck(PlayerOwner.Player);
        InitializeDeck(PlayerOwner.Enemy);
    }

    private void InitializeDeck(PlayerOwner owner)
    {
        List<CardData> deckList = GetTestDeckFromServer();
        Shuffle(deckList);

        Queue<CardData> deck = new Queue<CardData>();

        foreach (var card in deckList)
        {
            deck.Enqueue(card);
            deck.Enqueue(card);
        }

        decks[owner] = deck;

        Debug.Log($"{owner} deck initialized with {deck.Count} cards.");
    }

    private void OnEnable()
    {
        TurnManager.Instance.OnTurnStarted += HandleTurnStart;
    }
    private void HandleTurnStart(PlayerOwner owner)
    {
        DrawCard(owner);
    }

    public void Draw(int count, PlayerOwner owner)
    {
        for (int i = 0; i < count; i++) DrawCard(owner);
    }
    private void DrawCard(PlayerOwner owner)
    {
        Queue<CardData> deck = decks[owner];
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

        card.SetZone(CardZone.Hand);
        hand.AddCard(card.gameObject);
        hand.UpdateCardPositions();
    }

    private void Shuffle(List<CardData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = Random.Range(i, list.Count);
            (list[i], list[rnd]) = (list[rnd], list[i]);
        }
    }

    private List<CardData> GetTestDeckFromServer()
    {
        List<CardData> deck = new List<CardData>();

        foreach (CardData card in CardDatabase.Instance.Cards.Values)
        {
            if (card.cardType == "minion")
                deck.Add(card);
        }

        return deck;
    }

}
