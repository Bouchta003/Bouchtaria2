using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance;

    private bool isInitializing;
    private bool isGameReady;

    private bool cardsReady;
    private bool collectionReady;

    private string currentUid;

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
        }
    }

    public bool IsGameReady => isGameReady;

    public void InitializeForUser(string uid)
    {
        Debug.Log($"🚀 InitializeForUser: {uid}");

        ResetForNewUser();

        currentUid = uid;

        // 🔗 Subscribe to readiness events
        CardDatabase.Instance.OnCardsLoaded += OnCardsReady;
        UserCollectionManager.Instance.OnCollectionReady += OnCollectionReady;

        // 1️⃣ Load cards if needed
        if (CardDatabase.Instance.IsReady)
        {
            OnCardsReady();
        }
        else
        {
            CardDatabase.Instance.Initialize();
        }
    }
    private void OnEnable()
    {
        AuthManager.Instance.AuthReady += InitializeForUser;
    }

    private void OnDisable()
    {
        AuthManager.Instance.AuthReady -= InitializeForUser;
    }


    public void ResetForNewUser()
    {
        Debug.Log("🧹 Resetting game data state");

        cardsReady = false;
        collectionReady = false;
        currentUid = null;

        // ❌ Unsubscribe old events
        if (CardDatabase.Instance != null)
            CardDatabase.Instance.OnCardsLoaded -= OnCardsReady;

        if (UserCollectionManager.Instance != null)
            UserCollectionManager.Instance.OnCollectionReady -= OnCollectionReady;

        UserCollectionManager.Instance?.ResetForNewUser();
    }
    private void OnCardsReady()
    {
        CardDatabase.Instance.OnCardsLoaded -= OnCardsReady;
        cardsReady = true;

        Debug.Log("✅ Cards ready");

        FirestoreManager.Instance.CreateOrLoadUser(currentUid, () =>
        {
            Debug.Log("✅ Firestore user ready");
            UserCollectionManager.Instance.InitializeForUser(currentUid);
        });
    }
    private void OnCollectionReady()
    {
        Debug.Log("📦 Collection ready (GameFlow)");
        collectionReady = true;
        TryFinishInit();
    }
    private void TryFinishInit()
    {
        Debug.Log($"⏳ Check → Cards:{cardsReady} Collection:{collectionReady}");

        if (!cardsReady || !collectionReady)
            return;

        isGameReady = true;
        Debug.Log("🎮 GAME DATA READY");
    }


    public void GoToCollection()
    {
        if (!isGameReady)
        {
            Debug.LogWarning("⚠️ Game data not ready yet");
            return;
        }

        UserCollectionManager.Instance.RefreshCollection();
        SceneManager.LoadScene("Collection");
    }
    public void GoToMainMenu()
    {
        SceneManager.LoadScene("Main_Menu");
    }
    public void GoToTitleScreen()
    {
        SceneManager.LoadScene("Firebase");
    }
}
