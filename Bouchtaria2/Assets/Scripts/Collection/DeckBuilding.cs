using System.Collections.Generic;
using UnityEngine;

public class DeckBuilding : MonoBehaviour
{
    [Header("Chest Animator")]
    [SerializeField] ChestAnimation chestAnimation;
    [SerializeField] public GameObject CollectionLayout;
    [SerializeField] public GameObject DeckUI;

    [SerializeField] Collider2D chestCOllider;
    public static DeckBuilding Instance;
    public List<int> CurrentDeck;
    public CollectionScreen collection;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        collection = CollectionLayout.GetComponentInChildren<CollectionScreen>();
    }
    // Update is called once per frame
    void Update()
    {

    }

    public void DropCardToChest(Card card)
    {
        CurrentDeck.Add(card.GetComponent<CardView>().CardData.id);
        Debug.Log(DisplayDeckCardIDs(CurrentDeck));
    }
    public void RemoveCardFromChest(Card card)
    {
        if (CurrentDeck.Contains(card.GetComponent<CardView>().CardData.id))
        {
            CurrentDeck.Remove(card.GetComponent<CardView>().CardData.id);
            collection.ShowPage(collection.currentPage);
        }
        else Debug.LogWarning("Couldn't remove card of id " + card.GetComponent<CardView>().CardData.id);
        
        Debug.Log(DisplayDeckCardIDs(CurrentDeck));
    }
    public string DisplayDeckCardIDs(List<int> deck)
    {
        string result = "Current deck contains : ";
        foreach(int id in deck)
        {
            result += id.ToString()+", ";
        }
        result += $"for a total of {deck.Count} cards.";

        return result;
    }
    public void DisplayDeck()
    {
        collection.isDeck = !collection.isDeck;
        collection.ShowPage(collection.currentPage);
        DeckUI.SetActive(collection.isDeck);
    }
}
