using UnityEngine;
using Firebase.Auth;
using Firebase.Extensions;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance;

    public FirebaseAuth Auth { get; private set; }
    public FirebaseUser User { get; private set; }

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

    public void Initialize()
    {
        Auth = FirebaseAuth.DefaultInstance;

        if (Auth.CurrentUser != null)
        {
            User = Auth.CurrentUser;
            Debug.Log("✅ Existing user detected");
            Debug.Log("User ID: " + User.UserId);

            FirestoreManager.Instance.Initialize(User.UserId);
        }
        else
        {
            SignInAnonymously();
        }
    }


    void SignInAnonymously()
    {
        Auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("❌ Anonymous sign-in failed: " + task.Exception);
                return;
            }

            FirebaseUser newUser = task.Result.User;
            User = newUser;

            Debug.Log("✅ Signed in anonymously");
            Debug.Log("User ID: " + User.UserId);
            FirestoreManager.Instance.CreateOrLoadUser(User.UserId);
            FirestoreManager.Instance.Initialize(User.UserId);

        });
    }
}
