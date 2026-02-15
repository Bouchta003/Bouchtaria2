using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections;

public class FirestoreManager : MonoBehaviour
{
    public static FirestoreManager Instance;

    private FirebaseFirestore db;
    private bool isReady = false;

    public bool IsReady => isReady;

    private void Awake()
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

        StartCoroutine(WaitForFirebase());
    }

    private IEnumerator WaitForFirebase()
    {
        // 🔐 Wait until Firebase is actually initialized
        while (FirebaseApp.DefaultInstance == null)
        {
            yield return null;
        }

        db = FirebaseFirestore.DefaultInstance;
        isReady = true;

        Debug.Log("🔥 Firestore initialized AFTER Firebase");
    }

    public void CreateOrLoadUser(string uid, System.Action onReady)
    {
        if (!isReady)
        {
            Debug.LogError("❌ Firestore not ready yet");
            return;
        }

        Debug.Log($"📄 CreateOrLoadUser for UID: {uid}");

        DocumentReference userDoc = db.Collection("users").Document(uid);

        userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("❌ Failed to read user document");
                Debug.LogException(task.Exception);
                return;
            }

            if (!task.Result.Exists)
            {
                Debug.Log("🆕 Creating new user document");

                userDoc.SetAsync(new System.Collections.Generic.Dictionary<string, object>
                {
                    { "createdAt", Timestamp.GetCurrentTimestamp() },
                    { "displayName", "Anonymous" },
                    { "dust",100 },
                    { "gold",1000 },
                    { "streak",1 },
                    { "beststreak",0 },
                    { "coin",0 },
                    { "dungeondeck", new System.Collections.Generic.List<int>() },
                    { "dungeonaugments","" },
                    { "dungeoncombatactive", false },
                })
                .ContinueWithOnMainThread(setTask =>
                {
                    if (setTask.IsFaulted)
                    {
                        Debug.LogError("❌ Failed to create user document");
                        Debug.LogException(setTask.Exception);
                        return;
                    }

                    Debug.Log("✅ User document created");
                    onReady?.Invoke();
                });
            }
            else
            {
                Debug.Log("✅ User already exists");

                var snapshot = task.Result;
                var updates = new System.Collections.Generic.Dictionary<string, object>();

                if (!snapshot.ContainsField("gold"))
                    updates["gold"] = 1000;

                if (!snapshot.ContainsField("dust"))
                    updates["dust"] = 100;

                if (!snapshot.ContainsField("streak"))
                    updates["streak"] = 1; 
                
                if (!snapshot.ContainsField("beststreak"))
                    updates["beststreak"] = 0;

                if (!snapshot.ContainsField("coin"))
                    updates["coin"] = 0;

                if (!snapshot.ContainsField("dungeondeck"))
                    updates["dungeondeck"] = new System.Collections.Generic.List<int>();

                if (!snapshot.ContainsField("dungeonaugments"))
                    updates["dungeonaugments"] = "";

                if (!snapshot.ContainsField("dungeoncombatactive"))
                    updates["dungeoncombatactive"] = false;

                if (updates.Count > 0)
                {
                    userDoc.UpdateAsync(updates).ContinueWithOnMainThread(updateTask =>
                    {
                        if (updateTask.IsFaulted)
                        {
                            Debug.LogError("❌ Failed to add missing fields");
                            Debug.LogException(updateTask.Exception);
                            return;
                        }

                        Debug.Log("🛠 Missing fields added to user document");
                        onReady?.Invoke();
                    });
                }
                else
                {
                    onReady?.Invoke();
                }
            }

        });
    }
}
