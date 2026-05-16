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
    public int currentDeckSize;

    public void Reset()
    {
        floor = 1;
        coins = 30;
        augments ??= new List<DungeonShop.Augment>();
        dungeonDeck ??= new List<int>();
        currentDeckSize = 15;
        augments.Clear();
        dungeonDeck.Clear();
    }
}
public static class GameRunContext
{
    public static DungeonRunData DungeonData;
    public static PathOfPowerRunData PathOfPowerData;
    public static bool IsDungeonRun;
    public static bool IsPathOfPowerRun;
    public static bool IsAdventureCombat;
    public static int AdventureFightId;
    public static bool IsAdventureHardMode;
}

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance;

    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI StreakText;
    [SerializeField] TextMeshProUGUI BestStreakText;
    [SerializeField] Image StreakFire;
    [SerializeField] Image NextEnemy;

    [Header("Current Augments")]
    [SerializeField] TextMeshProUGUI HPAugmentCount;
    [SerializeField] TextMeshProUGUI ManaAugmentCount;
    [SerializeField] TextMeshProUGUI DrawAugmentCount;
    [SerializeField] TextMeshProUGUI ExtraLifeAugmentCount;
    public DungeonRunData CurrentRun;

    private const string StreakField = "streak";
    private const string BestStreakField = "beststreak";
    private const string CoinField = "coin";
    private const string DeckField = "dungeondeck";
    private const string AugmentsField = "dungeonaugments";
    private const string DungeonCombatActiveField = "dungeoncombatactive";
    private const string DungeonEventFloorsField = "dungeoneventfloors";
    private const string DungeonPendingEventField = "dungeonpendingevent";
    private const string DungeonPendingEventFloorField = "dungeonpendingeventfloor";
    private int currentBestStreak;
    private readonly HashSet<int> usedEventFloors = new HashSet<int>();
    private int pendingEventFloor = -1;

    int CurrentEvent = -1;
    [SerializeField] GameObject EventWindow;
    [SerializeField] TextMeshProUGUI EventText;
    [SerializeField] Image EventImage;
    [SerializeField] List<Sprite> EventImageList;
    [SerializeField] GameObject EventChoiceWindow;
    [SerializeField] GameObject EventAcceptWindow;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            CurrentRun = Instance.CurrentRun;
            Destroy(Instance.gameObject);
        }

        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindSceneUIReferences();
        ApplyRunToUI();

        if (CurrentEvent > 0 || (CurrentRun != null && CurrentRun.floor >= 5))
            TriggerRandomEvent();
    }

    private void EnsureCurrentRunInitialized()
    {
        CurrentRun ??= new DungeonRunData();
        CurrentRun.floor = Mathf.Max(1, CurrentRun.floor);
        CurrentRun.augments ??= new List<DungeonShop.Augment>();
        CurrentRun.dungeonDeck ??= new List<int>(); 
        if (CurrentRun.currentDeckSize <= 0)
            CurrentRun.currentDeckSize = 15;
    }

    void Start()
    {
        RebindSceneUIReferences();
        EnsureCurrentRunInitialized();
        CalculateDeckSize();
        ApplyRunToUI();
        FetchRunData();
    }

    private void RebindSceneUIReferences()
    {
        if (StreakText == null)
            StreakText = GameObject.Find("StreakText")?.GetComponent<TextMeshProUGUI>();

        if (BestStreakText == null)
            BestStreakText = GameObject.Find("BestStreakText")?.GetComponent<TextMeshProUGUI>();

        if (StreakFire == null)
            StreakFire = GameObject.Find("StreakFire")?.GetComponent<Image>();

        if (NextEnemy == null)
            NextEnemy = GameObject.Find("NextEnemy")?.GetComponent<Image>();

        if (HPAugmentCount == null)
            HPAugmentCount = GameObject.Find("HPAugmentCount")?.GetComponent<TextMeshProUGUI>();

        if (ManaAugmentCount == null)
            ManaAugmentCount = GameObject.Find("ManaAugmentCount")?.GetComponent<TextMeshProUGUI>();

        if (DrawAugmentCount == null)
            DrawAugmentCount = GameObject.Find("DrawAugmentCount")?.GetComponent<TextMeshProUGUI>(); 
        
        if (ExtraLifeAugmentCount == null)
            ExtraLifeAugmentCount = GameObject.Find("ReviveAugmentCount")?.GetComponent<TextMeshProUGUI>();
    }
    public void ToggleEventWindow()
    {
        EventWindow.SetActive(!EventWindow.activeSelf);
    }
    /// <summary>
    /// When the user reaches a streak that is a mutliple of 5 he gets a random bonus effect amongst these. 
    /// Depending on the current event, the image and text of the event should change, image should be based on the eventimagelist.
    /// It displays the event window, the contents of that window will vary depending on the current event chosen at random
    /// Once the random event is triggered, and the window closes, it counts as used for that floor and cannot be reused.
    /// If the user leaves the game and comes back, if he has used the augment it should still count as used. and not reset on scene load.
    /// </summary>
    public void TriggerRandomEvent()
    {
        EnsureCurrentRunInitialized();

        if (CurrentEvent >= 1 && CurrentEvent <= 3)
        {
            if (pendingEventFloor <= 0)
                pendingEventFloor = CurrentRun.floor;

            bool resumed = ShowEventForCurrentState();
            LogEventTrigger(CurrentRun.floor, CurrentEvent, resumed, resumed
                ? "Resumed pending event after scene load/relogin."
                : "Pending event could not be displayed (UI references missing)."
            );
            SaveRunData();
            return;
        }

        if (CurrentRun.floor < 5)
        {
            LogEventTrigger(CurrentRun.floor, -1, false, "Floor is below 5.");
            return;
        }

        if (CurrentRun.floor % 5 != 0)
        {
            LogEventTrigger(CurrentRun.floor, -1, false, "Floor is not divisible by 5.");
            return;
        }

        if (usedEventFloors.Contains(CurrentRun.floor))
        {
            LogEventTrigger(CurrentRun.floor, -1, false, "Event already used on this floor.");
            return;
        }

        pendingEventFloor = CurrentRun.floor;
        if (CurrentEvent < 1 || CurrentEvent > 3)
            CurrentEvent = UnityEngine.Random.Range(1, 4);

        bool triggered = ShowEventForCurrentState();
        LogEventTrigger(CurrentRun.floor, CurrentEvent, triggered, triggered
            ? "New event rolled on floor multiple of 5."
            : "Event roll happened but UI could not be shown."
        );
        SaveRunData();
    }
    public void ClickEventAnswer(bool answer)
    {
        switch (CurrentEvent)
        {
            case 1:
                CurrentRun.coins += 50;
                Debug.Log("Dungeon event: +50 dungeon coins.");
                CompleteCurrentEvent();
                break;
            case 2:
                CurrentRun.augments.Add(DungeonShop.Augment.MaxHP);
                CurrentRun.augments.Add(DungeonShop.Augment.MaxHP);
                Debug.Log("Dungeon event: +10 HP equivalent granted.");
                CompleteCurrentEvent();
                break;
            case 3:
                if (!answer)
                {
                    Debug.Log("Dungeon event choice declined by player.");
                    CompleteCurrentEvent();
                    return;
                }

                GetUserGold(gold =>
                {
                    int transferAmount = Mathf.Clamp(gold, 0, 300);
                    if (transferAmount <= 0)
                    {
                        Debug.Log("Dungeon event: no gold available to convert.");
                        CompleteCurrentEvent();
                        return;
                    }

                    CurrentRun.coins += transferAmount/3;
                    ModifyUserGold(-transferAmount, () =>
                    {
                        Debug.Log($"Dungeon event: converted {transferAmount} gold into dungeon coins.");
                        CompleteCurrentEvent();
                    });
                });
                break;
            default:
                HideEventWindows();
                return;
        }
    }

    private bool ShowEventForCurrentState()
    {
        bool hasMainWindow = EventWindow != null;
        ConfigureCurrentEventUI();
        return hasMainWindow;
    }

    private void ConfigureCurrentEventUI()
    {
        if (EventWindow != null)
            EventWindow.SetActive(true);

        if (EventChoiceWindow != null)
            EventChoiceWindow.SetActive(CurrentEvent == 3);

        if (EventAcceptWindow != null)
            EventAcceptWindow.SetActive(CurrentEvent != 3);

        if (EventImage != null && EventImageList != null && EventImageList.Count >= CurrentEvent)
            EventImage.sprite = EventImageList[Mathf.Max(0, CurrentEvent - 1)];

        if (EventText == null)
            return;

        switch (CurrentEvent)
        {
            case 1:
                EventText.text = "I got too much money Alhamdulillah, here are 50 coins if you want.";
                break;
            case 2:
                EventText.text = "Doctor Bensalmia here, would you like some more health ? Belive me it's free !";
                break;
            case 3:
                EventText.text = "Okay so I negociated with the guy and he said you could trade of 300 golds for 100 coins, are you interested ? ";
                break;
            default:
                EventText.text = "";
                break;
        }
    }

    private void CompleteCurrentEvent()
    {
        if (pendingEventFloor > 0)
            usedEventFloors.Add(pendingEventFloor);

        pendingEventFloor = -1;
        CurrentEvent = -1;
        HideEventWindows();
        SaveRunData();
    }

    private void LogEventTrigger(int floor, int eventValue, bool didTrigger, string reason)
    {
        Debug.Log($"[DungeonEvent] floor={floor}, eventValue={eventValue}, triggered={didTrigger}, reason={reason}");
    }

    private void HideEventWindows()
    {
        if (EventWindow != null)
            EventWindow.SetActive(false);

        if (EventChoiceWindow != null)
            EventChoiceWindow.SetActive(false); 
        
        if (EventAcceptWindow != null)
            EventAcceptWindow.SetActive(false);
    }
    public void RefreshAugmentCount()
    {
        int decksize = 15;
        if (HPAugmentCount != null)
            HPAugmentCount.text = "0";

        if (ManaAugmentCount != null)
            ManaAugmentCount.text = "0";
        
        if (ExtraLifeAugmentCount != null)
            ExtraLifeAugmentCount.text = "0";

        if (DrawAugmentCount != null)
            DrawAugmentCount.text = "0";

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
            if (pair.Key == DungeonShop.Augment.ExtraLife && pair.Value > 0)
            {
                if (ExtraLifeAugmentCount != null)
                    ExtraLifeAugmentCount.text = pair.Value.ToString();

                if (ExtraLifeAugmentCount.text == "0") ExtraLifeAugmentCount.gameObject.SetActive(false);
                else ExtraLifeAugmentCount.gameObject.SetActive(true);
            }
            if (pair.Key == DungeonShop.Augment.StartDraw && pair.Value > 0)
            {
                if (DrawAugmentCount != null)
                    DrawAugmentCount.text = pair.Value.ToString();
            }
            if (pair.Key == DungeonShop.Augment.DeckSizeUp3 && pair.Value > 0)
            {
                decksize += 3 * pair.Value;
            }
            if (pair.Key == DungeonShop.Augment.DeckSizeDown3 && pair.Value > 0)
            {
                decksize -= 3 * pair.Value;
            }
        }

    }
    public int CalculateDeckSize()
    {
        EnsureCurrentRunInitialized();

        int decksize = 20;

        Dictionary<DungeonShop.Augment, int> augmentCounts =
            CurrentRun.augments
                .GroupBy(a => a)
                    .ToDictionary(g => g.Key, g => g.Count());
        foreach (var pair in augmentCounts)
        {
            if (pair.Key == DungeonShop.Augment.DeckSizeUp3 && pair.Value > 0)
            {
                decksize += 3 * pair.Value;
            }
            if (pair.Key == DungeonShop.Augment.DeckSizeDown3 && pair.Value > 0)
            {
                decksize -= 3 * pair.Value;
            }
        }
        CurrentRun.currentDeckSize = Mathf.Max(5, decksize);
        return CurrentRun.currentDeckSize;
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
                bool hadPendingCombat = snapshot.ContainsField(DungeonCombatActiveField)
                    && snapshot.GetValue<bool>(DungeonCombatActiveField);
                CurrentRun = new DungeonRunData
                {
                    floor = snapshot.ContainsField(StreakField) ? task.Result.GetValue<int>(StreakField) : 1,
                    coins = snapshot.ContainsField(CoinField) ? task.Result.GetValue<int>(CoinField) : 30,
                    augments = ParseAugments(snapshot, AugmentsField),
                    dungeonDeck = ParseDeck(snapshot, DeckField)
                }; CalculateDeckSize();
                usedEventFloors.Clear();
                foreach (var floor in ParseIntList(snapshot, DungeonEventFloorsField))
                    usedEventFloors.Add(Mathf.Max(1, floor));
                CurrentEvent = snapshot.ContainsField(DungeonPendingEventField)
                    ? task.Result.GetValue<int>(DungeonPendingEventField)
                    : -1;
                pendingEventFloor = snapshot.ContainsField(DungeonPendingEventFloorField)
                    ? Mathf.Max(1, task.Result.GetValue<int>(DungeonPendingEventFloorField))
                    : (CurrentEvent > 0 ? CurrentRun.floor : -1);
                currentBestStreak = snapshot.ContainsField(BestStreakField) ? task.Result.GetValue<int>(BestStreakField) : 0;


                bool hasNoRunData = CurrentRun.floor <= 0
                    && CurrentRun.dungeonDeck.Count <= 0
                    && CurrentRun.augments.Count <= 0;

                if (CurrentRun.floor <= 0)
                    CurrentRun.floor = 1;

                Debug.Log("User streak: " + CurrentRun.floor);
                ApplyRunToUI();
                TriggerRandomEvent();

                if (hasNoRunData)
                {
                    StartNewRun();
                    return;
                }

                if (hadPendingCombat)
                {
                    Debug.LogWarning("Detected unfinished dungeon combat from previous session. Resetting the run to prevent progress abuse.");
                    EndRunAndReset(user, goToDungeonMenu: false);
                }
            });
    }
    public void StartNewRun()
    {
        CurrentRun = new DungeonRunData
        {
            floor = 1,
            coins = 30,
            augments = new List<DungeonShop.Augment>(),
            dungeonDeck = new List<int>()
        }; 
        usedEventFloors.Clear();
        pendingEventFloor = -1;
        CurrentEvent = -1;
        CalculateDeckSize();
        currentBestStreak = 0;

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
            { DeckField,  CurrentRun.dungeonDeck},
            { DungeonCombatActiveField, false },
            { DungeonEventFloorsField, usedEventFloors.ToList() },
            { DungeonPendingEventField, CurrentEvent },
            { DungeonPendingEventFloorField, pendingEventFloor }
        };

        if (resetStreak)
        { 
            updates[StreakField] = 1;
            updates[DeckField] = new List<int>();
            updates[DungeonEventFloorsField] = new List<int>();
            updates[DungeonPendingEventField] = -1;
            updates[DungeonPendingEventFloorField] = -1;
        }

        db.Collection("users")
            .Document(user.UserId)
            .UpdateAsync(updates)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    ErrorPopup.Show("Failed to save dungeon run data.");
            });
        GameRunContext.DungeonData = CurrentRun;
        ApplyRunToUI();
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
                    TryUpdateBestStreak(CurrentRun.floor);
                    ApplyRunToUI();
                    TriggerRandomEvent();
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

        EndRunAndReset(user, goToDungeonMenu: false);
    }
    public void ConcedeRun()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            return;
        }

        EndRunAndReset(user, goToDungeonMenu: true);
    }

    private void EndRunAndReset(FirebaseUser user, bool goToDungeonMenu)
    {
        int runScore = Mathf.Max(1, CurrentRun?.floor ?? 1);
        TryUpdateBestStreak(runScore, () =>
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

            db.Collection("users")
              .Document(user.UserId)
              .UpdateAsync(StreakField, 1)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError("Failed to reset streak.");
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
            CurrentRun.coins = 30;
            usedEventFloors.Clear();
            pendingEventFloor = -1;
            CurrentEvent = -1;
            SaveRunData(resetStreak: true);

            if (GameRunContext.DungeonData != null)
            {
                GameRunContext.DungeonData.Reset();
                GameRunContext.DungeonData.coins = 30;
            }
            ApplyRunToUI();

            if (goToDungeonMenu)
                GoToDungeonMenu();
        });
    }

    private void TryUpdateBestStreak(int candidateStreak, Action onComplete = null)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            onComplete?.Invoke();
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference userDoc = db.Collection("users").Document(user.UserId);

        userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || !task.Result.Exists)
            {
                onComplete?.Invoke();
                return;
            }

            int bestStreak = task.Result.ContainsField(BestStreakField)
                ? task.Result.GetValue<int>(BestStreakField)
                : 0;

            int targetBest = Mathf.Max(bestStreak, candidateStreak);
            currentBestStreak = targetBest;

            if (targetBest > bestStreak)
            {
                userDoc.UpdateAsync(BestStreakField, targetBest).ContinueWithOnMainThread(_ =>
                {
                    ApplyRunToUI();
                    onComplete?.Invoke();
                });
                return;
            }

            ApplyRunToUI();
            onComplete?.Invoke();
        });
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

    private static List<int> ParseIntList(DocumentSnapshot snapshot, string field)
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

    private void GetUserGold(Action<int> onResult)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            onResult?.Invoke(0);
            return;
        }

        FirebaseFirestore.DefaultInstance
            .Collection("users")
            .Document(user.UserId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || !task.Result.Exists)
                {
                    Debug.LogError("Failed to fetch user gold.");
                    onResult?.Invoke(0);
                    return;
                }

                int gold = task.Result.ContainsField("gold")
                    ? task.Result.GetValue<int>("gold")
                    : 0;
                onResult?.Invoke(Mathf.Max(0, gold));
            });
    }

    private void ModifyUserGold(int delta, Action onComplete = null)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            onComplete?.Invoke();
            return;
        }

        FirebaseFirestore.DefaultInstance
            .Collection("users")
            .Document(user.UserId)
            .UpdateAsync("gold", FieldValue.Increment(delta))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("Failed to modify gold.");

                onComplete?.Invoke();
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
    public void GoToRankings()
    {
        SceneManager.LoadScene("DungeonRank");
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
            if (deck.Count != CurrentRun.currentDeckSize) GameFlowController.Instance.GoToDungeonDeck(CurrentRun);
            else 
            {
                //Start Combat :
                List<int> enemyDeck0 = EnemyDecks.GetFloorDeck(CurrentRun.floor);
                DeckSelectionCache.SelectedEnemyDeck = enemyDeck0;
                DeckSelectionCache.SelectedPlayerDeck = new List<int>(deck);
                CurrentRun.dungeonDeck = new List<int>(deck);
                SetDungeonCombatActive(true, () => GameFlowController.Instance.GoToDungeonCombat(CurrentRun));
            }
        });
    }

    public static void SetDungeonCombatActive(bool isActive, Action onComplete = null)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            onComplete?.Invoke();
            return;
        }

        FirebaseFirestore.DefaultInstance
            .Collection("users")
            .Document(user.UserId)
            .UpdateAsync(DungeonCombatActiveField, isActive)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError($"Failed to set dungeon combat flag ({isActive}).");

                onComplete?.Invoke();
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

            if (BestStreakText != null)
                BestStreakText.text = currentBestStreak.ToString();

            if (StreakFire != null)
            {
                if (CurrentRun.floor < 5) StreakFire.gameObject.SetActive(true);
                if (CurrentRun.floor >= 5) StreakFire.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            }

            if (NextEnemy != null) NextEnemy.sprite = CardDatabase.Instance.GetCardById(EnemyDecks.GetFloorDeck(CurrentRun.floor)[0]).artSpriteCompact;
        }

        RefreshAugmentCount();
    }
}
