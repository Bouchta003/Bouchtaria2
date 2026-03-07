using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class AdventureRunData
{
    public List<int> completedStageOneFightIds = new List<int>();
    public bool secondStageUnlocked;
    public int secondStageStreak;
    public bool thirdStageUnlocked;

    public bool CanReachSecondStage => secondStageUnlocked || completedStageOneFightIds.Count >= 8;
    public bool CanReachThirdStage => thirdStageUnlocked || secondStageStreak >= 4;
}

public static class AdventureProgressionService
{
    public const int UltimateFightId = 13;

    private static readonly int[] StageOneFightIds = { 1, 2, 3, 4, 5, 6, 7, 8 };
    private static readonly int[] StageTwoFightIds = { 9, 10, 11, 12 };

    private const string AdventureCombatActiveField = "adventurecombatactive";
    private const string AdventureCompletedStageOneField = "adventurestageonecompletedfights";
    private const string AdventureSecondStageUnlockedField = "adventuresecondstageunlocked";
    private const string AdventureSecondStageStreakField = "adventuresecondstagestreak";
    private const string AdventureThirdStageUnlockedField = "adventurethirdstageunlocked";
    private const string AdventureCanReachSecondStageField = "adventurecanreachsecondstage";
    private const string AdventureCanReachThirdStageField = "adventurecanreachthirdstage";

    public static bool IsStageOneFight(int fightId) => StageOneFightIds.Contains(fightId);
    public static bool IsStageTwoFight(int fightId) => StageTwoFightIds.Contains(fightId);

    public static void SetAdventureCombatActive(bool isActive, Action onComplete = null)
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
            .UpdateAsync(AdventureCombatActiveField, isActive)
            .ContinueWithOnMainThread(_ => onComplete?.Invoke());
    }

    public static void RecordFightResult(int fightId, bool didWin)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            return;
        }

        DocumentReference userDoc = FirebaseFirestore.DefaultInstance.Collection("users").Document(user.UserId);
        userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || !task.Result.Exists)
            {
                Debug.LogError("Failed to fetch adventure progression data.");
                return;
            }

            AdventureRunData data = Parse(task.Result);

            if (didWin)
            {
                if (IsStageOneFight(fightId))
                {
                    if (!data.completedStageOneFightIds.Contains(fightId))
                        data.completedStageOneFightIds.Add(fightId);

                    data.secondStageUnlocked = data.completedStageOneFightIds.Count >= StageOneFightIds.Length;
                }
                else if (IsStageTwoFight(fightId))
                {
                    if (!data.CanReachSecondStage)
                    {
                        Persist(userDoc, data);
                        return;
                    }

                    int expectedFight = StageTwoFightIds[Mathf.Clamp(data.secondStageStreak, 0, StageTwoFightIds.Length - 1)];
                    data.secondStageStreak = fightId == expectedFight ? data.secondStageStreak + 1 : 1;
                    data.thirdStageUnlocked = data.secondStageStreak >= StageTwoFightIds.Length;
                }
                else if (fightId == UltimateFightId)
                {
                    if (!data.CanReachThirdStage)
                    {
                        Persist(userDoc, data);
                        return;
                    }
                }
            }
            else
            {
                if (IsStageTwoFight(fightId))
                    data.secondStageStreak = 0;
            }

            Persist(userDoc, data);
        });
    }

    public static bool CanStartFight(AdventureRunData data, int fightId)
    {
        if (IsStageOneFight(fightId))
            return true;

        if (IsStageTwoFight(fightId))
            return data.CanReachSecondStage;

        if (fightId == UltimateFightId)
            return data.CanReachThirdStage;

        return false;
    }

    private static void Persist(DocumentReference userDoc, AdventureRunData data)
    {
        data.completedStageOneFightIds = data.completedStageOneFightIds
            .Distinct()
            .Where(IsStageOneFight)
            .OrderBy(x => x)
            .ToList();

        var updates = new Dictionary<string, object>
        {
            { AdventureCompletedStageOneField, data.completedStageOneFightIds },
            { AdventureSecondStageUnlockedField, data.secondStageUnlocked || data.completedStageOneFightIds.Count >= StageOneFightIds.Length },
            { AdventureSecondStageStreakField, Mathf.Clamp(data.secondStageStreak, 0, StageTwoFightIds.Length) },
            { AdventureThirdStageUnlockedField, data.thirdStageUnlocked || data.secondStageStreak >= StageTwoFightIds.Length },
            { AdventureCanReachSecondStageField, data.CanReachSecondStage },
            { AdventureCanReachThirdStageField, data.CanReachThirdStage },
            { AdventureCombatActiveField, false }
        };

        userDoc.UpdateAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
                Debug.LogError("Failed to save adventure progression data.");
        });
    }

    private static AdventureRunData Parse(DocumentSnapshot snapshot)
    {
        var data = new AdventureRunData();

        if (snapshot.ContainsField(AdventureCompletedStageOneField))
        {
            object raw = snapshot.GetValue<object>(AdventureCompletedStageOneField);
            if (raw is IEnumerable<object> list)
                data.completedStageOneFightIds = list.Select(x => Convert.ToInt32(x)).ToList();
            else if (raw is IEnumerable<int> ints)
                data.completedStageOneFightIds = ints.ToList();
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
