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
        floor = 0;
        coins = 0;
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

    [Header("Current Augments")]
    [SerializeField] GameObject HPAugment;
    [SerializeField] GameObject ManaAugment;
    [SerializeField] TextMeshProUGUI HPAugmentCount;
    [SerializeField] TextMeshProUGUI ManaAugmentCount;
    public DungeonRunData CurrentRun;

    private const string StreakField = "streak";
    private const string CoinField = "coin";
    private const string DeckField = "dungeondeck";
    private const string AugmentsField = "dungeonaugments";
    private const int WinCoinReward = 20;
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
    void Start()
    {
        CurrentRun ??= new DungeonRunData
        {
            floor = 1,
            coins = 0,
            augments = new List<DungeonShop.Augment>(),
            dungeonDeck = new List<int>()
        };

        FetchRunData();
    }
    public void RefreshAugmentCount()
    {
        HPAugment.SetActive(false);
        ManaAugment.SetActive(false);
        HPAugmentCount.text = "0";
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
                HPAugment.SetActive(true);HPAugmentCount.text = pair.Value.ToString();
            }

            if (pair.Key == DungeonShop.Augment.StartMana && pair.Value > 0)
            {
                ManaAugment.SetActive(true); ManaAugmentCount.text = pair.Value.ToString();
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
                    augments = ParseAugments(snapshot.ContainsField(AugmentsField) ? task.Result.GetValue<string>(AugmentsField) : string.Empty),
                    dungeonDeck = ParseDeck(snapshot, DeckField)
                };

                bool hasNoRunData = CurrentRun.floor <= 0
                    && CurrentRun.dungeonDeck.Count <= 0
                    && CurrentRun.augments.Count <= 0;

                if (CurrentRun.floor <= 0)
                    CurrentRun.floor = 1;

                Debug.Log("User streak: " + CurrentRun.floor);
                UpdateStreakUI(CurrentRun.floor);

                if (hasNoRunData)
                {
                    StartNewRun();
                    return;
                }

                GameRunContext.DungeonData = CurrentRun;
                RefreshAugmentCount();
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
        GameRunContext.DungeonData = CurrentRun;
        SaveRunData(resetStreak: true);
        RefreshAugmentCount();
    }

    public void SaveRunData(bool resetStreak = false)
    {
        if (CurrentRun == null)
            return;

        SaveRunData(CurrentRun, resetStreak);
        RefreshAugmentCount();
    }
    public void IncrementStreak()
    {
        ApplyCombatResult(playerWon: true);
    }

    public void ResetStreak()
    {
        ApplyCombatResult(playerWon: false);
    }

    public static void ApplyCombatResult(bool playerWon)
    {
        DungeonRunData runData = GameRunContext.DungeonData
            ?? Instance?.CurrentRun
            ?? new DungeonRunData
            {
                floor = 1,
                coins = 0,
                augments = new List<DungeonShop.Augment>(),
                dungeonDeck = new List<int>()
            };

        runData.augments ??= new List<DungeonShop.Augment>();
        runData.dungeonDeck ??= new List<int>();

        if (playerWon)
        {
            runData.floor = Mathf.Max(1, runData.floor + 1);
            runData.coins += WinCoinReward;
        }
        else
        {
            runData.floor = 1;
            runData.coins = 0;
            runData.augments.Clear();
            runData.dungeonDeck.Clear();
        }

        GameRunContext.DungeonData = runData;

        if (Instance != null)
        {
            Instance.CurrentRun = runData;
            Instance.UpdateStreakUI(runData.floor);
            Instance.RefreshAugmentCount();
        }

        SaveRunData(runData, resetStreak: !playerWon);
    }
    private void UpdateStreakUI(int streak)
    {
        if (StreakText != null)
            StreakText.text = streak.ToString();

        if (StreakFire == null)
            return;

        if (streak <= 1) StreakFire.gameObject.SetActive(false);
        else if (streak < 5) StreakFire.gameObject.SetActive(true);
        if (streak >= 5) StreakFire.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
    }

    private static void SaveRunData(DungeonRunData runData, bool resetStreak)
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
            { StreakField, runData.floor},
            { CoinField, runData.coins },
            { AugmentsField, SerializeAugments(runData.augments) },
            { DeckField,  runData.dungeonDeck }
        };

        if (resetStreak)
        {
            updates[StreakField] = 0;
            updates[CoinField] = 0;
            updates[AugmentsField] = string.Empty;
            updates[DeckField] = new List<int>();
        }

        db.Collection("users")
            .Document(user.UserId)
            .UpdateAsync(updates)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    ErrorPopup.Show("Failed to save dungeon run data.");
            });
    }

    public void GetUserCurrentStreak(Action<int> onResult)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            onResult?.Invoke(0);
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
                  onResult?.Invoke(0);
                  return;
              }

              int streak = task.Result.ContainsField(StreakField)
                  ? task.Result.GetValue<int>(StreakField)
                  : 0;

              onResult?.Invoke(streak);
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
    public void FloorCombat()
    {
        GetUserCurrentDeck(deck =>
        {
            if (deck.Count <= 0) GameFlowController.Instance.GoToDungeonDeck(CurrentRun);
            else 
            {
                //Start Combat :
                List<int> enemyDeck0 = new List<int>
                    {0,0,        // Starter Choice
                    46,46,      // Faust flower
                    19,19,      // Chimchar
                    58,58,      // NoMusic

                    49,49,      // IO
                    120,120,    // Metronome
                    40,40,      // Beldum

                    54,54,      // Dormis
                    55,55,      // Darkrai
                    56,56,      // Wigglytuff
                    57,57,      // Snorlax

                    116,118,    // Reshiram et Zekrom
                    88,88,      // Rainbow Card
                    89,89,      // Frog
                    133,133     // Hoopa portal
                    };
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

    private static List<DungeonShop.Augment> ParseAugments(string raw)
    {
        var list = new List<DungeonShop.Augment>();

        if (string.IsNullOrWhiteSpace(raw))
            return list;

        string[] chunks = raw.Split(',');
        foreach (var chunk in chunks)
        {
            if (int.TryParse(chunk, out int value) && Enum.IsDefined(typeof(DungeonShop.Augment), value))
                list.Add((DungeonShop.Augment)value);
        }

        return list;
    }
}
