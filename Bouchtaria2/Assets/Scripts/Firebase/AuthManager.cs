using UnityEngine;
using Firebase;
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
            ErrorPopup.Show("No user to link");
            return;
        }

        if (!auth.CurrentUser.IsAnonymous)
        {
            ErrorPopup.Show("User already has an account");
            return;
        }

        Credential credential =
            EmailAuthProvider.GetCredential(email, password);

        auth.CurrentUser.LinkWithCredentialAsync(credential)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    ShowAuthError(task.Exception);
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
            ErrorPopup.Show($"Firebase Auth Error Code: {firebaseEx.ErrorCode}");
        }
        else
        {
            ErrorPopup.Show(e.ToString());
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
            ErrorPopup.Show("❌ AuthReady called but CurrentUser is null");
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
    private void ShowAuthError(System.AggregateException ex, string fallback = "Authentication failed")
    {
        Debug.LogError(ex);

        string message = GetFirebaseAuthErrorMessage(ex);
        if (string.IsNullOrEmpty(message))
            message = fallback;

        ErrorPopup.Show(message);
    }

    private string GetFirebaseAuthErrorMessage(System.AggregateException aggEx)
    {
        if (aggEx == null)
            return "Authentication failed";

        var flat = aggEx.Flatten();

        // 1) Try Firebase error codes
        foreach (var inner in flat.InnerExceptions)
        {
            if (inner is FirebaseException firebaseEx)
            {
                try
                {
                    var authError = (AuthError)firebaseEx.ErrorCode;
                    string mapped = MapAuthError(authError);

                    // Only return mapped value if it is meaningful
                    if (!string.IsNullOrEmpty(mapped) &&
                        mapped != "Authentication failed")
                    {
                        return mapped;
                    }
                }
                catch
                {
                    // Do NOT return here – fall back to message
                }
            }
        }

        // 2) Fallback: use the clean inner exception message
        foreach (var inner in flat.InnerExceptions)
        {
            if (!string.IsNullOrEmpty(inner.Message))
                return CleanFirebaseMessage(inner.Message);
        }

        return "Authentication failed";
    }
    private string CleanFirebaseMessage(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "Authentication failed";

        string msg = raw.Trim();

        // Remove parentheses
        if (msg.StartsWith("(") && msg.EndsWith(")"))
            msg = msg.Substring(1, msg.Length - 2);

        // Normalize known Firebase messages
        if (msg.Contains("email address is badly formatted"))
            return "Invalid email address";

        if (msg.Contains("password is invalid"))
            return "Incorrect password";

        if (msg.Contains("no user record"))
            return "Account not found";

        return msg;
    }

    private string MapAuthError(AuthError error)
    {
        switch (error)
        {
            case AuthError.EmailAlreadyInUse:
                return "Email already in use";
            case AuthError.InvalidEmail:
                return "Invalid email address";
            case AuthError.WeakPassword:
                return "Password is too weak";
            case AuthError.WrongPassword:
                return "Incorrect password";
            case AuthError.UserNotFound:
                return "Account not found";
            case AuthError.UserDisabled:
                return "Account disabled";
            case AuthError.OperationNotAllowed:
                return "Operation not allowed";
            case AuthError.RequiresRecentLogin:
                return "Please log in again";
            case AuthError.CredentialAlreadyInUse:
                return "Account already linked";
            case AuthError.NetworkRequestFailed:
                return "Network error";
            default:
                return "Authentication failed";
        }
    }
    // 🔹 TEST 1 — Anonymous login
    public void SignInAnonymously()
    {
        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                ShowAuthError(task.Exception);
                return;
            }

            var user = task.Result.User;
            Debug.Log($"🆕 Anonymous login: {user.UserId}");
            auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    ShowAuthError(task.Exception);
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
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    ShowAuthError(task.Exception);
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
            ShowAuthError(task.Exception);
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
