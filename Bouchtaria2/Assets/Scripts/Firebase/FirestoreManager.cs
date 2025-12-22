using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

public class FirestoreManager : MonoBehaviour
{
    public static FirestoreManager Instance;

    private FirebaseFirestore db;

    private const int CURRENT_USER_SCHEMA_VERSION = 1;

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
            return;
        }
    }
    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
    }
    public void Initialize(string userId)
    {
        db = FirebaseFirestore.DefaultInstance;
        CreateOrLoadUser(userId);
    }
    public void CreateOrLoadUser(string userId)
    {
        DocumentReference userDoc = db.Collection("users").Document(userId);

        userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("❌ Failed to read user document: " + task.Exception);
                return;
            }

            DocumentSnapshot snapshot = task.Result;
            Dictionary<string, object> schema = GetUserSchema();

            Dictionary<string, object> updates = MergeSchemaWithDocument(schema, snapshot);

            // Always refresh last login
            updates["lastLoginAt"] = Timestamp.GetCurrentTimestamp();

            if (!snapshot.Exists)
            {
                Debug.Log("🆕 Creating new user document");

                userDoc.SetAsync(schema).ContinueWithOnMainThread(writeTask =>
                {
                    if (writeTask.IsFaulted)
                    {
                        Debug.LogError("❌ Failed to create user document: " + writeTask.Exception);
                    }
                    else
                    {
                        Debug.Log("✅ User document created");
                    }
                });

                return;
            }

            if (updates.Count > 0)
            {
                userDoc.UpdateAsync(updates).ContinueWithOnMainThread(updateTask =>
                {
                    if (updateTask.IsFaulted)
                    {
                        Debug.LogError("❌ Failed to update user document: " + updateTask.Exception);
                    }
                    else
                    {
                        Debug.Log($"🔁 User schema updated ({updates.Count} fields added)");
                    }
                });
            }
        });
    }
    /// <summary>
    /// Canonical user schema.
    /// This dictionary defines ALL expected user fields and their default values.
    /// Used for both creation and auto-migration.
    /// </summary>
    private Dictionary<string, object> GetUserSchema()
    {
        return new Dictionary<string, object>
    {
        // === Identity ===
        { "displayName", "New Player" },
        { "createdAt", Timestamp.GetCurrentTimestamp() },
        { "lastLoginAt", Timestamp.GetCurrentTimestamp() },

        // === Economy ===
        { "currency", 1000 },
        { "dungeonProgression", 0 },
        { "battlesWon", 0 },

        // === Progression ===
        { "starterDeckGiven", false }
    };
    }
    /// <summary>
    /// Merges the canonical user schema with an existing Firestore document.
    /// Missing fields are added, existing fields are preserved.
    /// </summary>
    private Dictionary<string, object> MergeSchemaWithDocument(
        Dictionary<string, object> schema,
        DocumentSnapshot snapshot)
    {
        Dictionary<string, object> result = new Dictionary<string, object>();

        foreach (var kvp in schema)
        {
            if (!snapshot.Exists || !snapshot.ContainsField(kvp.Key))
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }
}
