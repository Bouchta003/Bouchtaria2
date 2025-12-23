using UnityEngine;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro;
using System;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    private FirebaseAuth auth;

    public FirebaseUser CurrentUser
    {
        get
        {
            if (auth == null) return null;
            return auth.CurrentUser;
        }
    }

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
    public void LinkAnonymousAccount(string email, string password)
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("❌ No user to link");
            return;
        }

        if (!auth.CurrentUser.IsAnonymous)
        {
            Debug.LogWarning("⚠️ User already has an account");
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
                    LogAuthError(task.Exception);
                    return;
                }

                Debug.Log("🔗 Anonymous account successfully linked");
                Debug.Log($"UID preserved: {task.Result.User.UserId}");
            });
    }
    private void LogAuthError(System.Exception e)
    {
        if (e is Firebase.FirebaseException firebaseEx)
        {
            Debug.LogError($"Firebase Auth Error Code: {firebaseEx.ErrorCode}");
        }
        else
        {
            Debug.LogError(e);
        }
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
    public void OnAuthReady()
    {
        Debug.Log("🔐 OnAuthReady");

        string uid = auth.CurrentUser.UserId;
        GameFlowController.Instance.InitializeForUser(uid);
    }
    public event Action<string> AuthReady;
    private void NotifyAuthReady()
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("❌ AuthReady called but CurrentUser is null");
            return;
        }

        string uid = auth.CurrentUser.UserId;
        Debug.Log($"🔐 Auth ready → UID: {uid}");

        AuthReady?.Invoke(uid);
    }


    private string lastUserId = null;

    private void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (auth.CurrentUser == null)
            return;

        string uid = auth.CurrentUser.UserId;

        if (uid == lastUserId)
            return;

        lastUserId = uid;

        Debug.Log($"🔐 Auth stabilized for user {uid}");

        OnAuthReady();
    }

    // 🔹 TEST 1 — Anonymous login
    public void SignInAnonymously()
    {
        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("❌ Anonymous sign-in failed");
                return;
            }

            var user = task.Result.User;
            Debug.Log($"🆕 Anonymous login: {user.UserId}");
            auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("❌ Anonymous login failed");
                    return;
                }

                Debug.Log("✅ Anonymous login success");
                NotifyAuthReady();
            });

        });

    }
    public void CreateOrLinkAccount(string email, string password)
    {
        if (auth.CurrentUser != null && auth.CurrentUser.IsAnonymous)
        {
            // 🔵 UPGRADE PATH (PRESERVE DATA)
            LinkAnonymousAccount(email, password);
            return;
        }

        // 🟢 FRESH ACCOUNT PATH
        CreateEmailAccount(email, password);
    }
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

            // ❌ DO NOT call OnAuthReady here
        });
    }

    public void SignInWithEmail(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password)
    .ContinueWithOnMainThread(task =>
    {
        if (task.IsFaulted)
        {
            Debug.LogError("❌ Email login failed");
            return;
        }

        Debug.Log("✅ Email login success");
        NotifyAuthReady();
    });

    }


    // 🔹 Utility (important for clean tests)
    public void SignOut()
    {
        auth.SignOut(); 
        
        GameFlowController.Instance.ResetForNewUser();
        UserCollectionManager.Instance.ResetForNewUser();

        Debug.Log("🚪 Signed out");
    }
}
