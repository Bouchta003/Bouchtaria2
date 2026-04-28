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
    public List<int> completedStageTwoFightIds = new List<int>();
    public List<int> hardCompletedStageOneFightIds = new List<int>();
    public List<int> hardCompletedStageTwoFightIds = new List<int>();
    public bool secondStageUnlocked;
    public bool thirdStageUnlocked;
    public bool hardSecondStageUnlocked;
    public bool hardThirdStageUnlocked;
    public bool hardModeUnlocked;
    public bool isHardMode;

    public bool CanReachSecondStage(bool hardMode)
    {
        if (hardMode)
            return hardSecondStageUnlocked || hardCompletedStageOneFightIds.Count >= 8;

        return secondStageUnlocked || completedStageOneFightIds.Count >= 8;
    }

    public bool CanReachThirdStage(bool hardMode)
    {
        if (hardMode)
            return hardThirdStageUnlocked || hardCompletedStageTwoFightIds.Count >= 4;

        return thirdStageUnlocked || completedStageTwoFightIds.Count >= 4;
    }
}

public static class AdventureProgressionService
{
    public const int UltimateFightId = 13;

    private static readonly int[] StageOneFightIds = { 1, 2, 3, 4, 5, 6, 7, 8 };
    private static readonly int[] StageTwoFightIds = { 9, 10, 11, 12 };

    private const string AdventureCombatActiveField = "adventurecombatactive";
    private const string AdventureCompletedStageOneField = "adventurestageonecompletedfights";
    private const string AdventureCompletedStageTwoField = "adventurestagetwocompletedfights";
    private const string AdventureSecondStageUnlockedField = "adventuresecondstageunlocked";
    private const string AdventureSecondStageStreakField = "adventuresecondstagestreak";
    private const string AdventureThirdStageUnlockedField = "adventurethirdstageunlocked";
    private const string AdventureCanReachSecondStageField = "adventurecanreachsecondstage";
    private const string AdventureCanReachThirdStageField = "adventurecanreachthirdstage";
    private const string AdventureHardCompletedStageOneField = "adventurehardstageonecompletedfights";
    private const string AdventureHardCompletedStageTwoField = "adventurehardstagetwocompletedfights";
    private const string AdventureHardSecondStageUnlockedField = "adventurehardsecondstageunlocked";
    private const string AdventureHardThirdStageUnlockedField = "adventurehardthirdstageunlocked";
    private const string AdventureHardCanReachSecondStageField = "adventurehardcanreachsecondstage";
    private const string AdventureHardCanReachThirdStageField = "adventurehardcanreachthirdstage";
    private const string AdventureHardModeUnlockedField = "hardmodeunlocked";
    private const string AdventureIsHardModeField = "adventureishardmode";

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

    public static void RecordFightResult(int fightId, bool didWin, bool isHardMode = false)
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
                    List<int> stageOneFights = isHardMode ? data.hardCompletedStageOneFightIds : data.completedStageOneFightIds;
                    if (!stageOneFights.Contains(fightId))
                        stageOneFights.Add(fightId);

                    if (isHardMode)
                        data.hardSecondStageUnlocked = stageOneFights.Count >= StageOneFightIds.Length;
                    else
                        data.secondStageUnlocked = stageOneFights.Count >= StageOneFightIds.Length;
                }
                else if (IsStageTwoFight(fightId))
                {
                    if (!data.CanReachSecondStage(isHardMode))
                    {
                        Persist(userDoc, data);
                        return;
                    }

                    List<int> stageTwoFights = isHardMode ? data.hardCompletedStageTwoFightIds : data.completedStageTwoFightIds;
                    if (!stageTwoFights.Contains(fightId))
                        stageTwoFights.Add(fightId);

                    if (isHardMode)
                        data.hardThirdStageUnlocked = stageTwoFights.Count >= StageTwoFightIds.Length;
                    else
                        data.thirdStageUnlocked = stageTwoFights.Count >= StageTwoFightIds.Length;
                }
                else if (fightId == UltimateFightId)
                {
                    if (!data.CanReachThirdStage(isHardMode))
                    {
                        Persist(userDoc, data);
                        return;
                    }

                    if (!isHardMode)
                        data.hardModeUnlocked = true;
                }
            }
            else
            {
                if (IsStageTwoFight(fightId))
                {
                    List<int> stageTwoFights = isHardMode ? data.hardCompletedStageTwoFightIds : data.completedStageTwoFightIds;
                    if (!stageTwoFights.Contains(fightId))
                        stageTwoFights.Clear();
                }
            }

            Persist(userDoc, data);
        });
    }

    public static bool CanStartFight(AdventureRunData data, int fightId, bool isHardMode = false)
    {
        if (isHardMode && !data.hardModeUnlocked)
            return false;

        if (IsStageOneFight(fightId))
            return true;

        if (IsStageTwoFight(fightId))
            return data.CanReachSecondStage(isHardMode);

        if (fightId == UltimateFightId)
            return data.CanReachThirdStage(isHardMode);

        return false;
    }

    private static void Persist(DocumentReference userDoc, AdventureRunData data)
    {
        data.completedStageOneFightIds = data.completedStageOneFightIds
            .Distinct()
            .Where(IsStageOneFight)
            .OrderBy(x => x)
            .ToList();
        data.completedStageTwoFightIds = data.completedStageTwoFightIds
            .Distinct()
            .Where(IsStageTwoFight)
            .OrderBy(x => x)
            .ToList();
        data.hardCompletedStageOneFightIds = data.hardCompletedStageOneFightIds
            .Distinct()
            .Where(IsStageOneFight)
            .OrderBy(x => x)
            .ToList();
        data.hardCompletedStageTwoFightIds = data.hardCompletedStageTwoFightIds
            .Distinct()
            .Where(IsStageTwoFight)
            .OrderBy(x => x)
            .ToList();

        var updates = new Dictionary<string, object>
        {
            { AdventureCompletedStageOneField, data.completedStageOneFightIds },
            { AdventureCompletedStageTwoField, data.completedStageTwoFightIds },
            { AdventureSecondStageUnlockedField, data.secondStageUnlocked || data.completedStageOneFightIds.Count >= StageOneFightIds.Length },
            { AdventureSecondStageStreakField, FieldValue.Delete },
            { AdventureThirdStageUnlockedField, data.thirdStageUnlocked || data.completedStageTwoFightIds.Count >= StageTwoFightIds.Length },
            { AdventureCanReachSecondStageField, data.CanReachSecondStage(false) },
            { AdventureCanReachThirdStageField, data.CanReachThirdStage(false) },
            { AdventureHardCompletedStageOneField, data.hardCompletedStageOneFightIds },
            { AdventureHardCompletedStageTwoField, data.hardCompletedStageTwoFightIds },
            { AdventureHardSecondStageUnlockedField, data.hardSecondStageUnlocked || data.hardCompletedStageOneFightIds.Count >= StageOneFightIds.Length },
            { AdventureHardThirdStageUnlockedField, data.hardThirdStageUnlocked || data.hardCompletedStageTwoFightIds.Count >= StageTwoFightIds.Length },
            { AdventureHardCanReachSecondStageField, data.CanReachSecondStage(true) },
            { AdventureHardCanReachThirdStageField, data.CanReachThirdStage(true) },
            { AdventureHardModeUnlockedField, data.hardModeUnlocked },
            { AdventureIsHardModeField, data.isHardMode && data.hardModeUnlocked },
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
        if (snapshot.ContainsField(AdventureCompletedStageTwoField))
        {
            object raw = snapshot.GetValue<object>(AdventureCompletedStageTwoField);
            if (raw is IEnumerable<object> list)
                data.completedStageTwoFightIds = list.Select(x => Convert.ToInt32(x)).ToList();
            else if (raw is IEnumerable<int> ints)
                data.completedStageTwoFightIds = ints.ToList();
        }
        if (snapshot.ContainsField(AdventureHardCompletedStageOneField))
        {
            object raw = snapshot.GetValue<object>(AdventureHardCompletedStageOneField);
            if (raw is IEnumerable<object> list)
                data.hardCompletedStageOneFightIds = list.Select(x => Convert.ToInt32(x)).ToList();
            else if (raw is IEnumerable<int> ints)
                data.hardCompletedStageOneFightIds = ints.ToList();
        }
        if (snapshot.ContainsField(AdventureHardCompletedStageTwoField))
        {
            object raw = snapshot.GetValue<object>(AdventureHardCompletedStageTwoField);
            if (raw is IEnumerable<object> list)
                data.hardCompletedStageTwoFightIds = list.Select(x => Convert.ToInt32(x)).ToList();
            else if (raw is IEnumerable<int> ints)
                data.hardCompletedStageTwoFightIds = ints.ToList();
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
