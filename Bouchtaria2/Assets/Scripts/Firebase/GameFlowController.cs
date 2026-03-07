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
    public void GoToCombat()
    {
        if (!isGameReady)
        {
            ErrorPopup.Show("⚠️ Game data not ready yet");
            return;
        }
        //IsThisLineNecessary ?
        UserCollectionManager.Instance.RefreshCollection();
        GameRunContext.IsDungeonRun = false;
        GameRunContext.IsAdventureCombat = false;
        GameRunContext.AdventureFightId = 0;
        SceneManager.LoadScene("QuickMatch");
    }
    public void GoToDungeonCombat(DungeonRunData data)
    {
        if (!isGameReady) return;
        GameRunContext.IsDungeonRun = true;
        GameRunContext.IsAdventureCombat = false;
        GameRunContext.AdventureFightId = 0;
        GameRunContext.DungeonData = DungeonManager.Instance.CurrentRun;
        SceneManager.LoadScene("Combat");
    }
    public void GoToDungeonAdventure()
    {
        if (!isGameReady) return;
        GameRunContext.IsDungeonRun = true;
        GameRunContext.IsAdventureCombat = false;
        GameRunContext.AdventureFightId = 0;
        GameRunContext.DungeonData = DungeonManager.Instance.CurrentRun;
        SceneManager.LoadScene("DungeonAdventure");
    }
    public void GoToDungeonDeck(DungeonRunData data)
    {
        GameRunContext.IsDungeonRun = true;
        GameRunContext.IsAdventureCombat = false;
        GameRunContext.AdventureFightId = 0;
        GameRunContext.DungeonData = data;

        if (!isGameReady)
        {
            ErrorPopup.Show("⚠️ Game data not ready yet");
            return;
        }

        UserCollectionManager.Instance.RefreshCollection();
        SceneManager.LoadScene("Collection");
    }

    public void GoToAdventureCombat(int fightId, System.Collections.Generic.List<int> playerDeck, System.Collections.Generic.List<int> enemyDeck)
    {
        if (!isGameReady)
        {
            ErrorPopup.Show("⚠️ Game data not ready yet");
            return;
        }

        DeckSelectionCache.SelectedPlayerDeck = new System.Collections.Generic.List<int>(playerDeck);
        DeckSelectionCache.SelectedEnemyDeck = new System.Collections.Generic.List<int>(enemyDeck);

        GameRunContext.IsDungeonRun = false;
        GameRunContext.IsAdventureCombat = true;
        GameRunContext.AdventureFightId = fightId;
        AdventureProgressionService.SetAdventureCombatActive(true, () => SceneManager.LoadScene("Combat"));
    }

    public void GoToShop()
    {
        if (!isGameReady)
        {
            ErrorPopup.Show("⚠️ Game data not ready yet");
            return;
        }
        //IsThisLineNecessary ?
        UserCollectionManager.Instance.RefreshCollection();
        SceneManager.LoadScene("Shop");
    }
    public void GoToCollection()
    {
        if (!isGameReady)
        {
            ErrorPopup.Show("⚠️ Game data not ready yet");
            return;
        }

        UserCollectionManager.Instance.RefreshCollection();
        GameRunContext.IsDungeonRun = false;
        GameRunContext.IsAdventureCombat = false;
        GameRunContext.AdventureFightId = 0;
        SceneManager.LoadScene("Collection");
    }
    public void GoToDungeon()
    {
        if (!isGameReady)
        {
            ErrorPopup.Show("⚠️ Game data not ready yet");
            return;
        }

        UserCollectionManager.Instance.RefreshCollection();
        SceneManager.LoadScene("DungeonMenu");
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
