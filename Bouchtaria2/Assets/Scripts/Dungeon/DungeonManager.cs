using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class DungeonRunData
{
    public int floor;
    public int coins;
    public List<DungeonShop.Augment> augments;
    public List<int> dungeonDeck;

    public void Reset()
    {
        floor = 1;
        coins = 0;
        augments ??= new List<DungeonShop.Augment>();
        dungeonDeck ??= new List<int>();
        augments.Clear();
        dungeonDeck.Clear();
    }
}
public static class GameRunContext
{
    public static DungeonRunData DungeonData;
    public static bool IsDungeonRun;
}

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance;

    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI StreakText;
    [SerializeField] Image StreakFire;
    [SerializeField] Image NextEnemy;

    [Header("Current Augments")]
    [SerializeField] TextMeshProUGUI HPAugmentCount;
    [SerializeField] TextMeshProUGUI ManaAugmentCount;
    public DungeonRunData CurrentRun;

    private const string StreakField = "streak";
    private const string CoinField = "coin";
    private const string DeckField = "dungeondeck";
    private const string AugmentsField = "dungeonaugments";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void EnsureCurrentRunInitialized()
    {
        CurrentRun ??= new DungeonRunData();
        CurrentRun.floor = Mathf.Max(1, CurrentRun.floor);
        CurrentRun.augments ??= new List<DungeonShop.Augment>();
        CurrentRun.dungeonDeck ??= new List<int>();
    }

    void Start()
    {
        EnsureCurrentRunInitialized();

        ApplyRunToUI();
        FetchRunData();
    }
    public void RefreshAugmentCount()
    {
        if (HPAugmentCount != null)
            HPAugmentCount.text = "0";

        if (ManaAugmentCount != null)
            ManaAugmentCount.text = "0";

        if (CurrentRun == null || CurrentRun.augments == null)
            return;

        Dictionary<DungeonShop.Augment, int> augmentCounts =
            CurrentRun.augments
                .GroupBy(a => a)
                    .ToDictionary(g => g.Key, g => g.Count());
        foreach (var pair in augmentCounts)
        {
            if (pair.Key == DungeonShop.Augment.MaxHP && pair.Value>0)
            {
                if (HPAugmentCount != null)
                    HPAugmentCount.text = pair.Value.ToString();
            }

            if (pair.Key == DungeonShop.Augment.StartMana && pair.Value > 0)
            {
                if (ManaAugmentCount != null)
                    ManaAugmentCount.text = pair.Value.ToString();
            }
        }

    }
    public void FetchRunData()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            StartNewRun();
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        db.Collection("users")
            .Document(user.UserId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || !task.Result.Exists)
                {
                    ErrorPopup.Show("Failed to fetch dungeon run data.");
                    StartNewRun();
                    return;
                }

                var snapshot = task.Result;
                CurrentRun = new DungeonRunData
                {
                    floor = snapshot.ContainsField(StreakField) ? task.Result.GetValue<int>(StreakField) : 1,
                    coins = snapshot.ContainsField(CoinField) ? task.Result.GetValue<int>(CoinField) : 0,
                    augments = ParseAugments(snapshot, AugmentsField),
                    dungeonDeck = ParseDeck(snapshot, DeckField)
                };

                bool hasNoRunData = CurrentRun.floor <= 0
                    && CurrentRun.dungeonDeck.Count <= 0
                    && CurrentRun.augments.Count <= 0;

                if (CurrentRun.floor <= 0)
                    CurrentRun.floor = 1;

                Debug.Log("User streak: " + CurrentRun.floor);
                ApplyRunToUI();

                if (hasNoRunData)
                {
                    StartNewRun();
                    return;
                }
            });
    }
    public void StartNewRun()
    {
        CurrentRun = new DungeonRunData
        {
            floor = 1,
            coins = 0,
            augments = new List<DungeonShop.Augment>(),
            dungeonDeck = new List<int>()
        };
        SaveRunData(resetStreak: true);
        ApplyRunToUI();
    }

    public void SaveRunData(bool resetStreak = false)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        var updates = new Dictionary<string, object>
        {
            { StreakField, CurrentRun.floor},
            { CoinField, CurrentRun.coins },
            { AugmentsField, SerializeAugments(CurrentRun.augments) },
            { DeckField,  CurrentRun.dungeonDeck}
        };

        if (resetStreak)
        { 
            updates[StreakField] = 1;
            updates[DeckField] = new List<int>();
        }

        db.Collection("users")
            .Document(user.UserId)
            .UpdateAsync(updates)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    ErrorPopup.Show("Failed to save dungeon run data.");
            }); ApplyRunToUI();
    }
    public void IncrementStreak()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        db.Collection("users")
          .Document(user.UserId)
          .UpdateAsync(StreakField, FieldValue.Increment(1))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to modify streak.");
                    return;
                }
                GetUserCurrentStreak(streak =>
                {
                    CurrentRun.floor = Mathf.Max(1, streak);
                    Debug.Log("User streak: " + streak);
                    ApplyRunToUI();
                    SaveRunData();
                });
            });
        ApplyRunToUI();
    }
    public void ResetStreak()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        db.Collection("users")
          .Document(user.UserId)
          .UpdateAsync(StreakField, 1)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to modify dust.");
                    return;
                }

                GetUserCurrentStreak(streak =>
                {
                    Debug.Log("User streak: " + streak);
                    ApplyRunToUI();
                });
            });

        EnsureCurrentRunInitialized();
        CurrentRun.Reset();
        CurrentRun.coins = 0;
        SaveRunData(resetStreak: true);

        if (GameRunContext.DungeonData != null)
        {
            GameRunContext.DungeonData.Reset();
            GameRunContext.DungeonData.coins = 0;
        }
        ApplyRunToUI();
    }
    public void ConcedeRun()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        db.Collection("users")
          .Document(user.UserId)
          .UpdateAsync(StreakField, 1)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to modify dust.");
                    return;
                }

                GetUserCurrentStreak(streak =>
                {
                    Debug.Log("User streak: " + streak);
                    ApplyRunToUI();
                });
            });

        CurrentRun.Reset();
        CurrentRun.coins = 0;
        SaveRunData(resetStreak: true);

        if (GameRunContext.DungeonData != null)
        {
            GameRunContext.DungeonData.Reset();
            GameRunContext.DungeonData.coins = 0;
        }
        ApplyRunToUI();
        GoToDungeonMenu();
    }
    public void GetUserCurrentStreak(Action<int> onResult)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            onResult?.Invoke(1);
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        db.Collection("users")
          .Document(user.UserId)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted || !task.Result.Exists)
              {
                  ErrorPopup.Show("Failed to fetch user streak.");
                  onResult?.Invoke(1);
                  return;
              }

              int streak = task.Result.ContainsField(StreakField)
                  ? task.Result.GetValue<int>(StreakField)
                  : 1;

              onResult?.Invoke(Mathf.Max(1, streak));
          });
    }
    public void GetUserCurrentDeck(Action<List<int>> onResult)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            onResult?.Invoke(new List<int>());
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        db.Collection("users")
          .Document(user.UserId)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted || !task.Result.Exists)
              {
                  ErrorPopup.Show("Failed to fetch user deck.");
                  onResult?.Invoke(new List<int>());
                  return;
              }

              onResult?.Invoke(ParseDeck(task.Result, DeckField));
          });
    }

    private static List<int> ParseDeck(DocumentSnapshot snapshot, string field)
    {
        if (!snapshot.ContainsField(field))
            return new List<int>();

        object raw = snapshot.GetValue<object>(field);

        if (raw is IEnumerable<object> enumerable)
            return enumerable.Select(x => Convert.ToInt32(x)).ToList();

        if (raw is IEnumerable<int> ints)
            return ints.ToList();

        return new List<int>();
    }

    public void LeaveToMenu()
    {
        SceneManager.LoadScene("Main_Menu");
    }
    public void GoToShop()
    {
        SceneManager.LoadScene("DungeonShop");
    }
    public void GoToAdventure()
    {
        SceneManager.LoadScene("DungeonAdventure");
    }
    public void GoToDungeonMenu()
    {
        SceneManager.LoadScene("DungeonMenu");
    }
    public void GoToDeck()
    {
        GameFlowController.Instance.GoToDungeonDeck(CurrentRun);
    }
    public void FloorCombat()
    {
        GetUserCurrentDeck(deck =>
        {
            if (deck.Count <= 0) GameFlowController.Instance.GoToDungeonDeck(CurrentRun);
            else 
            {
                //Start Combat :
                List<int> enemyDeck0 = EnemyDecks.GetFloorDeck(CurrentRun.floor);
                DeckSelectionCache.SelectedEnemyDeck = enemyDeck0;
                DeckSelectionCache.SelectedPlayerDeck = new List<int>(deck);
                CurrentRun.dungeonDeck = new List<int>(deck);
                GameFlowController.Instance.GoToDungeonCombat(CurrentRun); 
            }
        });
    }
    private static string SerializeAugments(List<DungeonShop.Augment> augments)
    {
        if (augments == null || augments.Count == 0)
            return string.Empty;

        return string.Join(",", augments.Select(a => ((int)a).ToString()));
    }

    private static List<DungeonShop.Augment> ParseAugments(DocumentSnapshot snapshot, string field)
    {
        if (!snapshot.ContainsField(field))
            return new List<DungeonShop.Augment>();

        string raw = string.Empty;

        if (snapshot.TryGetValue(field, out string storedAsString))
            raw = storedAsString;
        else if (snapshot.TryGetValue(field, out object storedAsObject) && storedAsObject != null)
            raw = storedAsObject.ToString();

        return ParseAugmentsFromString(raw);
    }

    private static List<DungeonShop.Augment> ParseAugmentsFromString(string raw)
    {
        var list = new List<DungeonShop.Augment>();

        if (string.IsNullOrWhiteSpace(raw))
            return list;

        string[] chunks = raw.Split(',');
        foreach (string chunk in chunks)
        {
            if (int.TryParse(chunk.Trim(), out int value) && Enum.IsDefined(typeof(DungeonShop.Augment), value))
                list.Add((DungeonShop.Augment)value);
        }

        return list;
    }

    private void ApplyRunToUI()
    {
        EnsureCurrentRunInitialized();

        if (CurrentRun != null)
        {
            if (StreakText != null)
                StreakText.text = CurrentRun.floor.ToString();

            if (StreakFire != null)
            {
                if (CurrentRun.floor < 5) StreakFire.gameObject.SetActive(true);
                if (CurrentRun.floor >= 5) StreakFire.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            }

            if (SceneManager.GetActiveScene().name == "DungeonAdventure")
            {
                if (NextEnemy != null) NextEnemy.sprite = CardDatabase.Instance.GetCardById(EnemyDecks.GetFloorDeck(CurrentRun.floor)[0]).artSpriteCompact;
            }
        }

        RefreshAugmentCount();
    }
}
