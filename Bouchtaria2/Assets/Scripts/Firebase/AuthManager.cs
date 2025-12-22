using UnityEngine;
using Firebase.Auth;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    private FirebaseAuth auth;

    public FirebaseUser CurrentUser => auth.CurrentUser;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize()
    {
        auth = FirebaseAuth.DefaultInstance;

        if (auth.CurrentUser != null)
        {
            Debug.Log("✅ Existing user detected");
            Debug.Log($"UID: {auth.CurrentUser.UserId}");
            OnAuthReady();
            return;
        }

        SignInAnonymously();
    }
    private void OnAuthReady()
    {
        string uid = auth.CurrentUser.UserId;

        FirestoreManager.Instance.Initialize(uid);
    }


    // 🔹 TEST 1 — Anonymous login
    public void SignInAnonymously()
    {
        auth.SignInAnonymouslyAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("❌ Anonymous login failed");
                Debug.LogError(task.Exception);
                return;
            }

            Debug.Log("✅ Anonymous login success");
            Debug.Log($"UID: {task.Result.User.UserId}");

            OnAuthReady();
        });
    }


    // 🔹 TEST 2 — Email account creation
    public void CreateEmailAccount(string email, string password)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("❌ Account creation failed");
                    Debug.LogError(task.Exception);
                    return;
                }

                Debug.Log("✅ Email account created");
                Debug.Log($"UID: {task.Result.User.UserId}"); 
                OnAuthReady();
            });
    }

    // 🔹 TEST 3 — Email login
    public void SignInWithEmail(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("❌ Email login failed");
                    Debug.LogError(task.Exception);
                    return;
                }

                Debug.Log("✅ Email login success");
                Debug.Log($"UID: {task.Result.User.UserId}");

                OnAuthReady(); // 🔥 REQUIRED
        });
    }


    // 🔹 Utility (important for clean tests)
    public void SignOut()
    {
        auth.SignOut();
        Debug.Log("🚪 Signed out");
    }
}
