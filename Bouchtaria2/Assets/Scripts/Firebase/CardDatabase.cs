using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System;
using System.Linq;

public class CardDatabase : MonoBehaviour
{
    public static CardDatabase Instance;

    private FirebaseFirestore db; 
    private Dictionary<int, CardData> cards = new Dictionary<int, CardData>();
    private Sprite defaultCardArt;

    public IReadOnlyDictionary<int, CardData> Cards => cards;

    public bool IsReady { get; private set; }
    public event System.Action OnCardsLoaded;

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

        defaultCardArt = Resources.Load<Sprite>("CardArt/default");
        if (defaultCardArt == null)
        {
            Debug.LogError("❌ Default card art not found at Resources/CardArt/default");
        }

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
            IsReady = true;
            OnCardsLoaded?.Invoke();

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
        if (!string.IsNullOrEmpty(card.artPath))
        {
            card.artSprite = Resources.Load<Sprite>(card.artPath);
        }

        if (!string.IsNullOrEmpty(card.artCompactPath))
        {
            card.artSpriteCompact = Resources.Load<Sprite>(card.artCompactPath);
        }

        if (card.artSprite == null)
        {
            card.artSprite = defaultCardArt;
            Debug.LogWarning($"⚠️ Missing art for card {card.id}, using default");
        }
        if (card.artSpriteCompact == null)
        {
            card.artSpriteCompact = defaultCardArt;
            Debug.LogWarning($"⚠️ Missing art for card {card.id}, using default");
        }
        return card;
    }
    public CardData GetCardById(int id)
    {
        if (Cards == null)
        {
            Debug.LogError("CardDatabase: Cards not initialized");
            return null;
        }

        if (!Cards.TryGetValue(id, out CardData data))
        {
            Debug.LogWarning($"CardDatabase: Card with id {id} not found");
            return null;
        }

        return data;
    }
    public List<CardData> GetCardsByEffect(string effect)
    {
        if (Cards == null)
        {
            Debug.LogError("CardDatabase: Cards not initialized");
            return null;
        }

        List<CardData> result = new List<CardData>();

        foreach (CardData card in Cards.Values)
        {
            if (!string.IsNullOrEmpty(card.effect) &&
                card.effect.Contains(effect))
            {
                result.Add(card);

            }
        }

        return result;
    }
    public List<CardData> GetCardsByTraitPackable(string trait)
    {
        if (Cards == null)
        {
            Debug.LogError("CardDatabase: Cards not initialized");
            return null;
        }

        List<CardData> result = new List<CardData>();

        foreach (CardData card in Cards.Values)
        {
            if (card.traits != null && card.packable &&
                card.traits.Any(t =>
                    t.Equals(trait, StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(card);
            }
        }

        return result;
    }
    public List<CardData> GetCardsByTypePackable(string type)
    {
        if (Cards == null)
        {
            Debug.LogError("CardDatabase: Cards not initialized");
            return null;
        }

        List<CardData> result = new List<CardData>();

        foreach (CardData card in Cards.Values)
        {
            if (card.cardType != null && card.packable && card.cardType==type)
            {
                result.Add(card);
            }
        }

        return result;
    }
    public List<CardData> GetCardsByTextPackable(string text)
    {
        if (Cards == null)
        {
            Debug.LogError("CardDatabase: Cards not initialized");
            return null;
        }

        List<CardData> result = new List<CardData>();

        foreach (CardData card in Cards.Values)
        {
            if (card.cardType != null && card.packable && card.effectText.ToLower().Contains(text.ToLower()))
            {
                result.Add(card);
            }
        }

        return result;
    }

    CardData.Trait TryParseTrait(string traitString, out CardData.Trait trait)
    {
        Enum.TryParse(traitString, true, out trait); return trait;
    }
}
