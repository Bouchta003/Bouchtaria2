using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

[Serializable]
public class OwnedCard
{
    public int cardId;
    public bool owned;
}

public class UserCollectionManager : MonoBehaviour
{
    public static UserCollectionManager Instance;

    public event Action OnCollectionReady;
    public event Action OnCollectionUpdated;

    private FirebaseFirestore db;
    private string uid;
    private bool isReady;

    public bool IsReady => isReady;

    // cardId -> OwnedCard
    private readonly Dictionary<int, OwnedCard> collection = new();
    public IReadOnlyDictionary<int, OwnedCard> Collection => collection;

    private const string COLLECTION_PATH = "cards";

    #region LIFECYCLE
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        db = FirebaseFirestore.DefaultInstance;
        Debug.Log("📚 UserCollectionManager initialized");
    }
    #endregion

    #region PUBLIC API
    public void Initialize(string newUid)
    {
        Debug.Log($"📚 Initialize collection for UID: {newUid}");

        ResetForNewUser();
        uid = newUid;

        if (!CardDatabase.Instance.IsReady)
        {
            Debug.Log("⏳ Waiting for CardDatabase...");
            CardDatabase.Instance.OnCardsLoaded += OnCardsLoaded;
            return;
        }

        LoadOrCreateCollection();
    }

    public void ResetForNewUser()
    {
        Debug.Log("🧹 Resetting UserCollectionManager state");

        uid = null;
        isReady = false;
        collection.Clear();
    }
    #endregion

    #region INTERNAL FLOW
    private void OnCardsLoaded()
    {
        CardDatabase.Instance.OnCardsLoaded -= OnCardsLoaded;
        Debug.Log("✅ CardDatabase ready (callback)");
        LoadOrCreateCollection();
    }
    public void InitializeForUser(string uid, System.Action onReady)
    {
        Debug.Log($"📚 InitializeForUser → {uid}");

        ResetForNewUser();

        CollectionReference colRef =
            db.Collection("users").Document(uid).Collection("collection");

        colRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("❌ Failed to read collection");
                Debug.LogException(task.Exception);
                return;
            }

            if (task.Result.Count == 0)
            {
                Debug.Log("🆕 No collection found → creating default");
                CreateDefaultCollection(colRef);
            }
            else
            {
                Debug.Log("📦 Existing collection found → loading");
                LoadExistingCollection(task.Result);
            }

            OnCollectionReady += onReady;
        });
    }
    private void MarkReady()
    {
        Debug.Log("📦 Collection READY");
        OnCollectionReady?.Invoke();
    }


    private void LoadExistingCollection(QuerySnapshot snapshot)
    {
        collection.Clear();

        foreach (var doc in snapshot.Documents)
        {
            int cardId = int.Parse(doc.Id);
            bool owned = doc.GetValue<bool>("owned");

            collection[cardId] = new OwnedCard
            {
                cardId = cardId,
                owned = owned
            };
        }

        Debug.Log($"📦 Loaded collection ({collection.Count} cards)");
        MarkReady();
    }

    private void LoadOrCreateCollection()
    {
        Debug.Log("📦 Loading user collection...");

        CollectionReference colRef =
            db.Collection("users").Document(uid).Collection(COLLECTION_PATH);

        colRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("❌ Failed to read collection");
                Debug.LogException(task.Exception);
                return;
            }

            if (task.Result.Count == 0)
            {
                Debug.Log("🆕 No cards found → creating default collection");
                CreateDefaultCollection(colRef);
            }
            else
            {
                LoadCollection(task.Result);
            }
        });
    }

    public void CreateDefaultCollection(CollectionReference colRef)
    {
        WriteBatch batch = db.StartBatch();

        collection.Clear();

        foreach (var kvp in CardDatabase.Instance.Cards)
        {
            int cardId = kvp.Key;

            // change this rule if you want different starters
            bool owned = false;

            batch.Set(
                colRef.Document(cardId.ToString()),
                new Dictionary<string, object>
                {
                    { "owned", owned }
                }
            );

            collection[cardId] = new OwnedCard
            {
                cardId = cardId,
                owned = owned
            };
        }

        batch.CommitAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("❌ Failed to create default collection");
                Debug.LogException(task.Exception);
                return;
            }

            Debug.Log($"✅ Default collection created ({collection.Count} cards)");
            MarkReady();
        });
    }

    private void LoadCollection(QuerySnapshot snapshot)
    {
        collection.Clear();

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            int cardId = int.Parse(doc.Id);
            bool owned = doc.GetValue<bool>("owned");

            collection[cardId] = new OwnedCard
            {
                cardId = cardId,
                owned = owned
            };
        }

        Debug.Log($"📦 Collection loaded ({collection.Count} cards)");
        MarkReady();
    }
    public void LoadCollectionFromFirestore(CollectionReference colRef)
    {
        colRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("❌ Failed to load collection");
                return;
            }

            collection.Clear();

            foreach (var doc in task.Result.Documents)
            {
                int cardId = int.Parse(doc.Id);
                bool owned = doc.GetValue<bool>("owned");

                collection[cardId] = new OwnedCard
                {
                    cardId = cardId,
                    owned = owned
                };
            }

            Debug.Log($"✅ Collection loaded ({collection.Count} cards)");
            MarkReady();
        });
    }
    public void InitializeForUser(string userid)
    {
        Debug.Log($"📚 Initialize collection for user: {userid}");

        uid = userid;
        isReady = false;

        CollectionReference colRef =
            FirebaseFirestore.DefaultInstance
            .Collection("users")
            .Document(uid)
            .Collection("collection");

        colRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("❌ Failed to load collection");
                Debug.LogException(task.Exception);
                return;
            }

            if (task.Result.Count == 0)
            {
                Debug.Log("🆕 No collection found → creating default");
                CreateDefaultCollection(colRef);
            }
            else
            {
                Debug.Log($"📦 Loaded collection ({task.Result.Count} cards)");
                LoadCollectionFromSnapshot(task.Result);
            }
        });
    }
    private void LoadCollectionFromSnapshot(QuerySnapshot snapshot)
    {
        collection.Clear();

        foreach (var doc in snapshot.Documents)
        {
            // Document ID = cardId
            if (!int.TryParse(doc.Id, out int cardId))
            {
                Debug.LogWarning($"⚠️ Invalid card id in collection: {doc.Id}");
                continue;
            }

            bool owned = false;

            if (doc.ContainsField("owned"))
            {
                owned = doc.GetValue<bool>("owned");
            }

            collection[cardId] = new OwnedCard
            {
                cardId = cardId,
                owned = owned
            };
        }

        Debug.Log($"✅ Collection loaded from Firestore ({collection.Count} cards)");
        MarkReady();
    }


    #endregion

    #region GAMEPLAY
    public bool IsOwned(int cardId)
    {
        return collection.TryGetValue(cardId, out var c) && c.owned;
    }

    public void UnlockCard(int cardId)
    {
        if (!collection.ContainsKey(cardId))
        {
            Debug.LogWarning($"⚠️ Card {cardId} not in collection");
            return;
        }

        if (collection[cardId].owned)
            return;

        collection[cardId].owned = true;

        db.Collection("users")
          .Document(uid)
          .Collection(COLLECTION_PATH)
          .Document(cardId.ToString())
          .UpdateAsync("owned", true)
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted)
              {
                  Debug.LogError($"❌ Failed to unlock card {cardId}");
                  Debug.LogException(task.Exception);
                  return;
              }

              Debug.Log($"🎉 Card {cardId} unlocked");
              OnCollectionUpdated?.Invoke();
          });
    }
    #endregion
}
