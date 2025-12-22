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
            Debug.Log($"User ID: {auth.CurrentUser.UserId}");
            return;
        }

        SignInAnonymously();
    }
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

                Debug.Log("✅ Email login successful");
                Debug.Log($"User ID: {task.Result.User.UserId}");
            });
    }

    private void SignInAnonymously()
    {
        auth.SignInAnonymouslyAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("❌ Anonymous sign-in failed");
                return;
            }

            Debug.Log("✅ Signed in anonymously");
            Debug.Log($"User ID: {task.Result.User.UserId}");
        });
    }
    public void LinkAnonymousAccount(string email, string password)
    {
        if (auth.CurrentUser == null || !auth.CurrentUser.IsAnonymous)
        {
            Debug.LogWarning("⚠️ No anonymous user to link");
            return;
        }

        Credential credential =
            EmailAuthProvider.GetCredential(email, password);

        auth.CurrentUser.LinkWithCredentialAsync(credential)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("❌ Account linking failed");
                    Debug.LogError(task.Exception);
                    return;
                }

                Debug.Log("🔗 Anonymous account successfully linked!");
                Debug.Log($"User ID (same): {task.Result.User.UserId}");
            });
    }

}
