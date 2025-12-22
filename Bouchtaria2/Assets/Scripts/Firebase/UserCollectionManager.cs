using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
[System.Serializable]
public class OwnedCard
{
    public int cardId;
    public bool owned;
}

public class UserCollectionManager : MonoBehaviour
{
    public static UserCollectionManager Instance;

    private FirebaseFirestore db;
    private string userId;

    public bool IsReady { get; private set; }
    public event System.Action OnCollectionReady;

    // cardId -> OwnedCard
    private Dictionary<int, OwnedCard> collection = new Dictionary<int, OwnedCard>();
    public IReadOnlyDictionary<int, OwnedCard> Collection => collection;
    #region INITIALIZATION
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

    public void Initialize(string userId)
    {
        this.userId = userId;
        db = FirebaseFirestore.DefaultInstance;
        LoadAndSyncCollection();
    }

    private void LoadAndSyncCollection()
    {
        CollectionReference userCollectionRef =
            db.Collection("users").Document(userId).Collection("collection");

        userCollectionRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("❌ Failed to load user collection: " + task.Exception);
                return;
            }

            collection.Clear();

            // 1️⃣ Load existing collection
            foreach (DocumentSnapshot doc in task.Result.Documents)
            {
                int cardId = int.Parse(doc.Id);
                bool owned = doc.GetValue<bool>("owned");

                collection[cardId] = new OwnedCard
                {
                    cardId = cardId,
                    owned = owned
                };
            }

            // 2️⃣ Sync against global card list
            SyncWithGlobalCards(userCollectionRef);
        });
    }

    private void SyncWithGlobalCards(CollectionReference userCollectionRef)
    {
        IReadOnlyDictionary<int, CardData> allCards = CardDatabase.Instance.Cards;

        WriteBatch batch = db.StartBatch();

        int addedCount = 0;

        foreach (int cardId in allCards.Keys)
        {
            if (!collection.ContainsKey(cardId))
            {
                DocumentReference cardDoc =
                    userCollectionRef.Document(cardId.ToString());

                batch.Set(cardDoc, new Dictionary<string, object>
                {
                    { "owned", false }
                });

                collection[cardId] = new OwnedCard
                {
                    cardId = cardId,
                    owned = false
                };

                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            batch.CommitAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("❌ Failed to sync new cards: " + task.Exception);
                }
                else
                {
                    Debug.Log($"🔁 Synced {addedCount} new cards (set as not owned)");
                }
            });
        }
        else
        {
            Debug.Log("✅ Collection already up-to-date");
        }
        IsReady = true;
        OnCollectionReady?.Invoke();
    }

    // ===== Helpers =====

    public bool IsOwned(int cardId)
    {
        return collection.TryGetValue(cardId, out var card) && card.owned;
    }

    public void SetOwned(int cardId, bool owned)
    {
        if (!collection.ContainsKey(cardId))
            return;

        collection[cardId].owned = owned;

        db.Collection("users")
          .Document(userId)
          .Collection("collection")
          .Document(cardId.ToString())
          .UpdateAsync("owned", owned);
    }
    #endregion
    /// <summary>
    /// Unlocks a card for the player if not already owned.
    /// Safe to call multiple times.
    /// </summary>
    public void UnlockCard(int cardId)
    {
        if (!collection.ContainsKey(cardId))
        {
            Debug.LogWarning($"⚠️ Tried to unlock unknown card ID: {cardId}");
            return;
        }

        if (collection[cardId].owned)
        {
            // Already unlocked → do nothing
            return;
        }

        collection[cardId].owned = true;

        db.Collection("users")
          .Document(userId)
          .Collection("collection")
          .Document(cardId.ToString())
          .UpdateAsync("owned", true);

        Debug.Log($"🔓 Card unlocked: {cardId}");
    }

}
