using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

public class CardDatabase : MonoBehaviour
{
    public static CardDatabase Instance;

    private FirebaseFirestore db; 
    private Dictionary<int, CardData> cards = new Dictionary<int, CardData>();

    public IReadOnlyDictionary<int, CardData> Cards => cards;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize()
    {
        db = FirebaseFirestore.DefaultInstance;
        LoadAllCards();
    }

    private void LoadAllCards()
    {
        db.Collection("cards").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("❌ Failed to load cards: " + task.Exception);
                return;
            }

            foreach (DocumentSnapshot doc in task.Result.Documents)
            {
                CardData card = ParseCard(doc);
                cards[card.id] = card;
            }

            Debug.Log($"✅ Loaded {cards.Count} cards");
        });
    }
    private CardData ParseCard(DocumentSnapshot doc)
    {
        CardData card = new CardData();

        // ID (safe even if doc ID is string)
        card.id = doc.GetValue<int>("id");

        card.name = doc.GetValue<string>("name");
        card.cardType = doc.GetValue<string>("cardType");

        card.manaCost = doc.GetValue<int>("manaCost");
        card.atkValue = doc.GetValue<int>("atkValue");
        card.hpValue = doc.GetValue<int>("hpValue");

        card.traits = doc.ContainsField("traits")
            ? new List<string>(doc.GetValue<List<string>>("traits"))
            : new List<string>();

        card.effect = doc.GetValue<string>("effect");
        card.effectText = doc.GetValue<string>("effectText");

        card.artPath = doc.GetValue<string>("artPath");
        card.artCompactPath = doc.GetValue<string>("artCompactPath");

        card.packable = doc.GetValue<bool>("packable");
        card.token = doc.GetValue<bool>("token");
        card.signature = doc.GetValue<bool>("signature");

        // Load sprite locally
        card.artSprite = Resources.Load<Sprite>(card.artPath);

        if (card.artSprite == null)
        {
            Debug.LogWarning($"⚠️ Missing sprite at path: {card.artPath}");
        }

        return card;
    }

}
