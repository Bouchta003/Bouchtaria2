using System.Collections.Generic;
using UnityEngine;

public class DeckBuilding : MonoBehaviour
{
    [Header("Chest Animator")]
    [SerializeField] ChestAnimation chestAnimation;

    [SerializeField] Collider2D chestCOllider;
    public static DeckBuilding Instance;
    public List<int> CurrentDeck;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
}
