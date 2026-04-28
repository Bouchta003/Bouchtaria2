using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class AdventureModeManager : MonoBehaviour
{
    private const string AdventureCompletedStageOneField = "adventurestageonecompletedfights";
    private const string AdventureCompletedStageTwoField = "adventurestagetwocompletedfights";
    private const string AdventureSecondStageUnlockedField = "adventuresecondstageunlocked";
    private const string AdventureThirdStageUnlockedField = "adventurethirdstageunlocked";
    private const string AdventureHardCompletedStageOneField = "adventurehardstageonecompletedfights";
    private const string AdventureHardCompletedStageTwoField = "adventurehardstagetwocompletedfights";
    private const string AdventureHardSecondStageUnlockedField = "adventurehardsecondstageunlocked";
    private const string AdventureHardThirdStageUnlockedField = "adventurehardthirdstageunlocked";
    private const string AdventureHardModeUnlockedField = "hardmodeunlocked";
    private const string AdventureIsHardModeField = "adventureishardmode";

    [SerializeField] Button previousFloorButton;
    [SerializeField] Button nextFloorButton;

    [SerializeField] GameObject FirstFloorUI;
    [SerializeField] GameObject SecondFloorUI;
    [SerializeField] GameObject ThirdFloorUI;
    [SerializeField] GameObject DifficultyGO;
    [SerializeField] Image DifficultyImage;
    [SerializeField] TextMeshProUGUI DifficultyText;
    [SerializeField] List<GameObject> CheckBoxes;

    bool isFirstFloor => FirstFloorUI.activeSelf && !SecondFloorUI.activeSelf && !ThirdFloorUI.activeSelf;
    bool isSecondFloor => !FirstFloorUI.activeSelf && SecondFloorUI.activeSelf && !ThirdFloorUI.activeSelf;
    bool isThirdFloor => !FirstFloorUI.activeSelf && !SecondFloorUI.activeSelf && ThirdFloorUI.activeSelf;
    private bool isHardMode;
    private void Start()
    {
        SwitchToFloor(1);
        GetAdventureProgression(data =>
        {
            isHardMode = data.hardModeUnlocked && data.isHardMode;
            DifficultyGO.SetActive(data.hardModeUnlocked);
            RefreshDifficultyUI();
            RefreshCheckBoxes(data);
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
                    nextFloorButton.gameObject.SetActive(data.CanReachSecondStage(isHardMode));
                });
                break;
            case 2:
                GetAdventureProgression(data =>
                {
                    if (data.CanReachSecondStage(isHardMode))
                    {
                        FirstFloorUI.SetActive(false); SecondFloorUI.SetActive(true); ThirdFloorUI.SetActive(false);
                        previousFloorButton.gameObject.SetActive(true);
                    }

                    nextFloorButton.gameObject.SetActive(data.CanReachThirdStage(isHardMode));
                });
                break;
            case 3:
                GetAdventureProgression(data =>
                {
                    if (data.CanReachThirdStage(isHardMode))
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
        Debug.Log($"EnemyClicked : {id}");
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
            if (!AdventureProgressionService.CanStartFight(data, fightId, isHardMode))
            {
                ErrorPopup.Show("This adventure fight is still locked.");
                return;
            }

            List<int> enemyDeck = EnemyDecks.GetAdventureDeck(fightId, isHardMode);
            if (enemyDeck == null || enemyDeck.Count == 0)
            {
                ErrorPopup.Show("Enemy deck is not configured for this adventure fight.");
                return;
            }

            GameFlowController.Instance.GoToAdventureCombat(fightId, playerDeck, enemyDeck, isHardMode);
        });
    }
    public void ClickDifficulty()
    {
        Debug.Log("Toggle mode");
        ToggleHardMode();
    }
    public void ToggleHardMode()
    {
        GetAdventureProgression(data =>
        {
            if (!data.hardModeUnlocked)
            {
                isHardMode = false;
                DifficultyGO.SetActive(false);
                RefreshDifficultyUI();
                return;
            }

            isHardMode = !isHardMode;
            RefreshDifficultyUI();
            RefreshCheckBoxes(data);
            SwitchToFloor(isFirstFloor ? 1 : isSecondFloor ? 2 : 3);
            PersistDifficultySelection();
        });
    }

    private void PersistDifficultySelection()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
            return;

        FirebaseFirestore.DefaultInstance
            .Collection("users")
            .Document(user.UserId)
            .UpdateAsync(AdventureIsHardModeField, isHardMode);
    }

    private void RefreshDifficultyUI()
    {
        if (DifficultyImage != null)
            DifficultyImage.color = isHardMode ? Color.red : Color.green;

        if (DifficultyText != null)
            DifficultyText.text = isHardMode ? "Hard mode" : "Easy Mode";
    }

    private void RefreshCheckBoxes(AdventureRunData data)
    {
        foreach (GameObject checkbox in CheckBoxes)
            checkbox.SetActive(false);

        List<int> stageOne = isHardMode ? data.hardCompletedStageOneFightIds : data.completedStageOneFightIds;
        List<int> stageTwo = isHardMode ? data.hardCompletedStageTwoFightIds : data.completedStageTwoFightIds;

        foreach (int id in stageOne)
        {
            if (id - 1 >= 0 && id - 1 < CheckBoxes.Count)
                CheckBoxes[id - 1].SetActive(true);
        }
        foreach (int id in stageTwo)
        {
            if (id - 1 >= 0 && id - 1 < CheckBoxes.Count)
                CheckBoxes[id - 1].SetActive(true);
        }
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
        if (snapshot.ContainsField(AdventureCompletedStageTwoField))
        {
            object raw = snapshot.GetValue<object>(AdventureCompletedStageTwoField);
            if (raw is IEnumerable<object> list)
            {
                foreach (object entry in list)
                {
                    if (entry != null && int.TryParse(entry.ToString(), out int value))
                        data.completedStageTwoFightIds.Add(value);
                }
            }
        }
        if (snapshot.ContainsField(AdventureHardCompletedStageOneField))
        {
            object raw = snapshot.GetValue<object>(AdventureHardCompletedStageOneField);
            if (raw is IEnumerable<object> list)
            {
                foreach (object entry in list)
                {
                    if (entry != null && int.TryParse(entry.ToString(), out int value))
                        data.hardCompletedStageOneFightIds.Add(value);
                }
            }
        }
        if (snapshot.ContainsField(AdventureHardCompletedStageTwoField))
        {
            object raw = snapshot.GetValue<object>(AdventureHardCompletedStageTwoField);
            if (raw is IEnumerable<object> list)
            {
                foreach (object entry in list)
                {
                    if (entry != null && int.TryParse(entry.ToString(), out int value))
                        data.hardCompletedStageTwoFightIds.Add(value);
                }
            }
        }

        data.secondStageUnlocked = snapshot.ContainsField(AdventureSecondStageUnlockedField)
            && snapshot.GetValue<bool>(AdventureSecondStageUnlockedField);

        data.thirdStageUnlocked = snapshot.ContainsField(AdventureThirdStageUnlockedField)
            && snapshot.GetValue<bool>(AdventureThirdStageUnlockedField);

        data.hardSecondStageUnlocked = snapshot.ContainsField(AdventureHardSecondStageUnlockedField)
            && snapshot.GetValue<bool>(AdventureHardSecondStageUnlockedField);

        data.hardThirdStageUnlocked = snapshot.ContainsField(AdventureHardThirdStageUnlockedField)
            && snapshot.GetValue<bool>(AdventureHardThirdStageUnlockedField);

        data.hardModeUnlocked = snapshot.ContainsField(AdventureHardModeUnlockedField)
            && snapshot.GetValue<bool>(AdventureHardModeUnlockedField);

        data.isHardMode = snapshot.ContainsField(AdventureIsHardModeField)
            && snapshot.GetValue<bool>(AdventureIsHardModeField);

        return data;
    }
}
