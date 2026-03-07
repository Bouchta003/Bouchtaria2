using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using UnityEngine;

public class AdventureModeManager : MonoBehaviour
{
    private const string AdventureCompletedStageOneField = "adventurestageonecompletedfights";
    private const string AdventureSecondStageUnlockedField = "adventuresecondstageunlocked";
    private const string AdventureSecondStageStreakField = "adventuresecondstagestreak";
    private const string AdventureThirdStageUnlockedField = "adventurethirdstageunlocked";

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

            List<int> enemyDeck = EnemyDecks.GetFloorDeck(fightId);
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
