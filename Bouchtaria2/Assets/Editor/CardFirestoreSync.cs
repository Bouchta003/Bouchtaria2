using UnityEditor;
using UnityEngine;
using Firebase;
using Firebase.Firestore;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;

public class CardFirestoreSync
{
    [MenuItem("Tools/Sync Cards To Firestore")]
    public static async void SyncCards()
    {
        string path = "Assets/Data/cardgeneration.json";

        if (!File.Exists(path))
        {
            Debug.LogError("❌ cardgeneration.json not found");
            return;
        }

        // 🔹 Ensure Firebase is initialized (VERY IMPORTANT in Editor)
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus != DependencyStatus.Available)
        {
            Debug.LogError("❌ Firebase dependencies not available: " + dependencyStatus);
            return;
        }

        string json = File.ReadAllText(path);

        CardDictionaryWrapper wrapper =
            JsonConvert.DeserializeObject<CardDictionaryWrapper>(json);

        if (wrapper == null || wrapper.cards == null)
        {
            Debug.LogError("❌ Failed to parse cards JSON");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        WriteBatch batch = db.StartBatch();

        foreach (var kvp in wrapper.cards)
        {
            CardData card = kvp.Value;

            DocumentReference doc =
                db.Collection("cards").Document(card.id.ToString());

            batch.Set(doc, new Dictionary<string, object>
            {
                { "id", card.id },
                { "name", card.name },
                { "cardType", card.cardType },
                { "manaCost", card.manaCost },
                { "atkValue", card.atkValue },
                { "hpValue", card.hpValue },
                { "traits", card.traits },
                { "effect", card.effect },
                { "effectText", card.effectText },
                { "artPath", card.artPath },
                { "artCompactPath", card.artCompactPath },
                { "packable", card.packable },
                { "token", card.token },
                { "signature", card.signature }
            });
        }

        try
        {
            await batch.CommitAsync();
            Debug.Log($"🔥 Successfully synced {wrapper.cards.Count} cards to Firestore");
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Firestore sync failed: " + e);
        }
    }

    [System.Serializable]
    private class CardDictionaryWrapper
    {
        public Dictionary<string, CardData> cards;
    }
}
