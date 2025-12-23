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
                    { "displayName", "Anonymous" }
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
                onReady?.Invoke();
            }
        });
    }
}
