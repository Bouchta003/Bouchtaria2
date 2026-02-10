using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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
        CurrentRun = new DungeonRunData
        {
            floor = 0,
            coins = 0,
            augments = new List<DungeonShop.Augment>()
        };

        DungeonShop.Instance.GetUserCurrentCoin(coin => CurrentRun.coins = coin);
    }
    public void StartNewRun()
    {
        CurrentRun = new DungeonRunData
        {
            floor = 1,
            coins = 0,
            augments = new List<DungeonShop.Augment>()
        };
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
          .UpdateAsync("streak", FieldValue.Increment(1))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to modify streak.");
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
          .UpdateAsync("streak", 0)
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

        GameRunContext.DungeonData.Reset();
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

              int streak = task.Result.ContainsField("streak")
                  ? task.Result.GetValue<int>("streak")
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
}
