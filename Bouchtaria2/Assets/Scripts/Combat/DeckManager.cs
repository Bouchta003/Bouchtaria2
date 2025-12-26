using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandManager handManager;
    [SerializeField] private TraitsDetection traitsDetection;

    [Header("Debug / Test")]
    [SerializeField] private PlayerOwner playerOwner = PlayerOwner.Player; 
    
    public Queue<CardData> playerDeck = new Queue<CardData>();
    private void Start()
    {
        InitializeTestDeck();
        traitsDetection.RetrieveTraitTiersFromDeck(playerDeck);
        Draw(3);
    }
    private void InitializeTestDeck()
    {
        // TEMP: replace with server-loaded data later
        List<CardData> deckList = GetTestDeckFromServer();

        Shuffle(deckList);

        foreach (var card in deckList)
        {
            playerDeck.Enqueue(card);
        }

        Debug.Log($"Deck initialized with {playerDeck.Count} cards.");
    }
    private void OnEnable()
    {
        TurnManager.Instance.OnTurnStarted += HandleTurnStart;
    }
    private void HandleTurnStart(PlayerOwner owner)
    {
        if (owner != PlayerOwner.Player)
            return;

        DrawCard();
    }
    public void Draw(int count)
    {
        for (int i = 0; i < count; i++) DrawCard();
    }
    private void DrawCard()
    {
        if (playerDeck.Count == 0)
        {
            Debug.Log("Deck is empty.");
            return;
        }
        if (handManager.handCards.Count >= handManager.maxHandSize)
        {
            playerDeck.Dequeue();//Burn
            Debug.Log("Hand is full.");
            return;
        }
        CardData data = playerDeck.Dequeue();

        CardInstance card = CardFactory.Instance.CreateCard(data, playerOwner);
        if (card == null)
            return;

        card.SetZone(CardZone.Hand);
        handManager.handCards.Add(card.gameObject);
        handManager.UpdateCardPositions();
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
