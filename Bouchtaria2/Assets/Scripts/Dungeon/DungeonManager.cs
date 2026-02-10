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
    //public int difficulty;
    public int coins;
    //public bool isBossFight;
    public List<DungeonShop.Augment> augments;

    public void Reset()
    {
        floor = 0;
        augments.Clear();
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

    public DungeonRunData CurrentRun;

    private const string StreakField = "streak";
    private const string CoinField = "coin";
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
    void Start()
    {
        GetUserCurrentStreak(streak =>
        {
            Debug.Log("User streak: " + streak);
            StreakText.text = streak.ToString();
            if (streak <= 1) StreakFire.gameObject.SetActive(false);
            else if(streak < 5) StreakFire.gameObject.SetActive(true);
            if (streak >= 5) StreakFire.transform.localScale = new Vector3(1.2f,1.2f,1.2f);

            if (streak <= 0) StartNewRun();
            else FetchRunData();
        });
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
                    augments = ParseAugments(snapshot.ContainsField(AugmentsField) ? task.Result.GetValue<string>(AugmentsField) : string.Empty)
                };

                if (CurrentRun.floor <= 0)
                    CurrentRun.floor = 1;
            });
    }
    public void StartNewRun()
    {
        CurrentRun = new DungeonRunData
        {
            floor = 1,
            coins = 0,
            augments = new List<DungeonShop.Augment>()
        };

        SaveRunData(resetStreak: true);
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
            { CoinField, CurrentRun.coins },
            { AugmentsField, SerializeAugments(CurrentRun.augments) }
        };

        if (resetStreak)
            updates[StreakField] = 0;

        db.Collection("users")
            .Document(user.UserId)
            .UpdateAsync(updates)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("Failed to save dungeon run data.");
            });
    }
    // Update is called once per frame
    void Update()
    {
        
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
                    StreakText.text = streak.ToString();
                    if (streak <= 1) StreakFire.gameObject.SetActive(false);
                    else if (streak < 5) StreakFire.gameObject.SetActive(true);
                    if (streak >= 5) StreakFire.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                    SaveRunData();
                });
            });

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
          .UpdateAsync(StreakField, 0)
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
                    StreakText.text = streak.ToString();
                    if (streak <= 1) StreakFire.gameObject.SetActive(false);
                    else if (streak < 5) StreakFire.gameObject.SetActive(true);
                    if (streak >= 5) StreakFire.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
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
        GameFlowController.Instance.GoToDungeonCombat(CurrentRun);
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
