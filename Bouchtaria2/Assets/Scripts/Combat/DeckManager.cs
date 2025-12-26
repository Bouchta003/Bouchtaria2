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
        foreach (var card in CardDatabase.Instance.Cards.Values)
        { 
        
        }
        traitsDetection.RetrieveTraitTiersFromDeck(playerDeck);
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
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            DrawCard();
        }
    }
    private void DrawCard()
    {
        if (playerDeck.Count == 0)
        {
            Debug.Log("Deck is empty.");
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
        // TEMP MOCK
        // Replace with Firestore fetch later
        return new List<CardData>(CardDatabase.Instance.Cards.Values);
    }
}
