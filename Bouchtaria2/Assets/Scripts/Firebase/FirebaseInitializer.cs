using UnityEngine;
using Firebase;
using Firebase.Extensions;

public class FirebaseInitializer : MonoBehaviour
{
    public static FirebaseInitializer Instance;
    public bool IsReady { get; private set; }

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
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                IsReady = true;
                Debug.Log("✅ Firebase initialized successfully");

                AuthManager.Instance.Initialize();
            }
            else
            {
                Debug.LogError("❌ Firebase dependency error: " + task.Result);
            }
        });
    }
}
