using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdventureModeManager : MonoBehaviour
{
    private const string AdventureCompletedStageOneField = "adventurestageonecompletedfights";
    private const string AdventureSecondStageUnlockedField = "adventuresecondstageunlocked";
    private const string AdventureSecondStageStreakField = "adventuresecondstagestreak";
    private const string AdventureThirdStageUnlockedField = "adventurethirdstageunlocked";

    [SerializeField] Button previousFloorButton;
    [SerializeField] Button nextFloorButton;

    [SerializeField] GameObject FirstFloorUI;
    [SerializeField] GameObject SecondFloorUI;
    [SerializeField] GameObject ThirdFloorUI;
    [SerializeField] List<GameObject> CheckBoxes;

    bool isFirstFloor => FirstFloorUI.activeSelf && !SecondFloorUI.activeSelf && !ThirdFloorUI.activeSelf;
    bool isSecondFloor => !FirstFloorUI.activeSelf && SecondFloorUI.activeSelf && !ThirdFloorUI.activeSelf;
    bool isThirdFloor => !FirstFloorUI.activeSelf && !SecondFloorUI.activeSelf && ThirdFloorUI.activeSelf;
    private void Start()
    {
        SwitchToFloor(1);
        GetAdventureProgression(data =>
        {
            foreach (int id in data.completedStageOneFightIds) {
                Debug.LogWarning(id);
                CheckBoxes[id - 1].SetActive(true);
            }
        });

    }
    /// <summary>
    /// Depending on the floor and the player progression, display the related buttons for floor switching. Min floor is 1 and max floor is 3 meaning that on floor 1 we don't have a previous floor,
    /// and on floor 3 we don't have a next floor.
    /// next floor button is only active depending on progression if player data CAN ACTUALLY Reach the next floor.
    /// </summary>
    /// <param name="floor"></param>
    public void SwitchToFloor(int floor)
    {
        switch (floor)
        {
            case 1:
                FirstFloorUI.SetActive(true); SecondFloorUI.SetActive(false); ThirdFloorUI.SetActive(false);
                previousFloorButton.gameObject.SetActive(false);
                GetAdventureProgression(data =>
                {
                    nextFloorButton.gameObject.SetActive(data.CanReachSecondStage);
                });
                break;
            case 2:
                GetAdventureProgression(data =>
                {
                    if (data.CanReachSecondStage)
                    {
                        FirstFloorUI.SetActive(false); SecondFloorUI.SetActive(true); ThirdFloorUI.SetActive(false);
                        previousFloorButton.gameObject.SetActive(true);
                    }

                    nextFloorButton.gameObject.SetActive(data.CanReachThirdStage);
                });
                break;
            case 3:
                GetAdventureProgression(data =>
                {
                    if (data.CanReachThirdStage)
                    {
                        FirstFloorUI.SetActive(false); SecondFloorUI.SetActive(false); ThirdFloorUI.SetActive(true);
                        previousFloorButton.gameObject.SetActive(true);
                        nextFloorButton.gameObject.SetActive(false);
                    }
                });
                break;
        }
    }
    public void ClickPreviousFloor()
    {
        if (isSecondFloor) SwitchToFloor(1);
        else if (isThirdFloor) SwitchToFloor(2);
    }
    public void ClickNextFloor()
    {
        if (isFirstFloor) SwitchToFloor(2);
        else if (isSecondFloor) SwitchToFloor(3);
    }
    public void ClickEnemy(int id)
    {
        Debug.Log($"EnemyClicked");
        DeckSelectionController deckSelector = FindFirstObjectByType<DeckSelectionController>();
        StartAdventureFight(id, deckSelector.GetSelectedUserDeck());
    }
    public void GetAdventureProgression(Action<AdventureRunData> onResult)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            onResult?.Invoke(new AdventureRunData());
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
                    Debug.LogError("Failed to fetch adventure progression data.");
                    onResult?.Invoke(new AdventureRunData());
                    return;
                }

                onResult?.Invoke(ParseAdventureData(task.Result));
            });
    }

    public void StartAdventureFight(int fightId, List<int> playerDeck)
    {
        GetAdventureProgression(data =>
        {
            if (!AdventureProgressionService.CanStartFight(data, fightId))
            {
                ErrorPopup.Show("This adventure fight is still locked.");
                return;
            }

            List<int> enemyDeck = EnemyDecks.GetAdventureDeck(fightId);
            if (enemyDeck == null || enemyDeck.Count == 0)
            {
                ErrorPopup.Show("Enemy deck is not configured for this adventure fight.");
                return;
            }

            GameFlowController.Instance.GoToAdventureCombat(fightId, playerDeck, enemyDeck);
        });
    }

    private static AdventureRunData ParseAdventureData(DocumentSnapshot snapshot)
    {
        var data = new AdventureRunData();

        if (snapshot.ContainsField(AdventureCompletedStageOneField))
        {
            object raw = snapshot.GetValue<object>(AdventureCompletedStageOneField);
            if (raw is IEnumerable<object> list)
            {
                foreach (object entry in list)
                {
                    if (entry != null && int.TryParse(entry.ToString(), out int value))
                        data.completedStageOneFightIds.Add(value);
                }
            }
        }

        data.secondStageUnlocked = snapshot.ContainsField(AdventureSecondStageUnlockedField)
            && snapshot.GetValue<bool>(AdventureSecondStageUnlockedField);

        data.secondStageStreak = snapshot.ContainsField(AdventureSecondStageStreakField)
            ? snapshot.GetValue<int>(AdventureSecondStageStreakField)
            : 0;

        data.thirdStageUnlocked = snapshot.ContainsField(AdventureThirdStageUnlockedField)
            && snapshot.GetValue<bool>(AdventureThirdStageUnlockedField);

        return data;
    }
}
