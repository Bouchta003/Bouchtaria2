using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PathOfPowerSaveService
{
    public const string CurrentFloorField = "pathofpower_currentfloor";
    public const string CurrentStepField = "pathofpower_currentstep";
    public const string CurrentDeckField = "pathofpower_currentdeck";
    public const string CurrentRelicsField = "pathofpower_currentrelics";
    public const string CurrentFloorSeedField = "pathofpower_currentfloorseed";
    public const string CurrentPathTypeField = "pathofpower_currentpathtype";
    public const string CurrentStreakField = "pathofpower_currentstreak";
    public const string PhaseField = "pathofpower_phase";
    public const string FloorStepsField = "pathofpower_floorsteps";
    public const string StarterRelicChoicesField = "pathofpower_starterrelicchoices";
    public const string WardenRelicRewardsField = "pathofpower_wardenrelicrewards";
    public const string PendingCardChoicesField = "pathofpower_pendingcardchoices";
    public const string StarterTraitChoicesField = "pathofpower_startertraitchoices";
    public const string StarterDeckTraitField = "pathofpower_starterdecktrait";
    public const string CombatActiveField = "pathofpower_combatactive";
    public const string CurrentDeckSizeField = "pathofpower_currentdecksize";
    public const string BestFloorField = "pathofpower_bestfloor";
    public const string BestStepField = "pathofpower_beststep";
    public const string BestFloorStepField = "pathofpower_bestfloorstep";
    public const string BestDeckField = "pathofpower_bestdeck";

    public static void Save(PathOfPowerRunData runData, Action onComplete = null)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user for Path Of Power save.");
            onComplete?.Invoke();
            return;
        }

        EnsureLists(runData);
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { CurrentFloorField, runData.currentFloor },
            { CurrentStepField, runData.currentStep },
            { CurrentDeckField, runData.currentDeck },
            { CurrentRelicsField, runData.currentRelics },
            { CurrentFloorSeedField, runData.currentFloorSeed },
            { CurrentPathTypeField, runData.currentPathType.ToString() },
            { CurrentStreakField, runData.currentStreak },
            { PhaseField, runData.phase.ToString() },
            { FloorStepsField, SerializeSteps(runData.currentFloorSteps) },
            { StarterRelicChoicesField, runData.pendingStarterRelicChoices },
            { WardenRelicRewardsField, runData.pendingWardenRelicRewards },
            { PendingCardChoicesField, runData.pendingCardChoices },
            { StarterTraitChoicesField, runData.pendingStarterTraitChoices },
            { StarterDeckTraitField, runData.starterDeckTrait.ToString() },
            { CombatActiveField, runData.combatActive },
            { CurrentDeckSizeField, runData.currentDeckSize }
        };

        FirebaseFirestore.DefaultInstance
            .Collection("users")
            .Document(user.UserId)
            .UpdateAsync(updates)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("Failed to save Path Of Power run data: " + task.Exception);

                GameRunContext.PathOfPowerData = runData;
                onComplete?.Invoke();
            });
    }

    public static void Load(Action<PathOfPowerRunData> onLoaded)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user for Path Of Power load.");
            onLoaded?.Invoke(new PathOfPowerRunData());
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
                    Debug.LogError("Failed to load Path Of Power run data.");
                    onLoaded?.Invoke(new PathOfPowerRunData());
                    return;
                }

                PathOfPowerRunData runData = ParseRun(task.Result);
                GameRunContext.PathOfPowerData = runData;
                onLoaded?.Invoke(runData);
            });
    }

    public static void SetCombatActive(bool isActive, Action onComplete = null)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user for Path Of Power combat flag.");
            onComplete?.Invoke();
            return;
        }

        FirebaseFirestore.DefaultInstance
            .Collection("users")
            .Document(user.UserId)
            .UpdateAsync(CombatActiveField, isActive)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError($"Failed to set Path Of Power combat flag ({isActive}).");

                onComplete?.Invoke();
            });
    }

    private static PathOfPowerRunData ParseRun(DocumentSnapshot snapshot)
    {
        PathOfPowerRunData runData = new PathOfPowerRunData
        {
            currentFloor = ReadInt(snapshot, CurrentFloorField, 0),
            currentStep = ReadInt(snapshot, CurrentStepField, 1),
            currentDeck = ParseIntList(snapshot, CurrentDeckField),
            currentRelics = ParseStringList(snapshot, CurrentRelicsField),
            currentFloorSeed = ReadInt(snapshot, CurrentFloorSeedField, 0),
            currentStreak = ReadInt(snapshot, CurrentStreakField, 0),
            currentFloorSteps = ParseSteps(snapshot),
            pendingStarterRelicChoices = ParseStringList(snapshot, StarterRelicChoicesField),
            pendingWardenRelicRewards = ParseStringList(snapshot, WardenRelicRewardsField),
            pendingCardChoices = ParseIntList(snapshot, PendingCardChoicesField),
            pendingStarterTraitChoices = ParseStringList(snapshot, StarterTraitChoicesField),
            starterDeckTrait = ParseEnum(ReadString(snapshot, StarterDeckTraitField, CardData.Trait.Neutral.ToString()), CardData.Trait.Neutral),
            combatActive = ReadBool(snapshot, CombatActiveField, false),
            currentDeckSize = Mathf.Max(5, ReadInt(snapshot, CurrentDeckSizeField, 20))
        };

        runData.currentPathType = ParseEnum(ReadString(snapshot, CurrentPathTypeField, PathOfPowerPathType.Simple.ToString()), PathOfPowerPathType.Simple);
        runData.phase = ParseEnum(ReadString(snapshot, PhaseField, PathOfPowerRunPhase.None.ToString()), PathOfPowerRunPhase.None);
        EnsureLists(runData);
        return runData;
    }

    private static int ReadInt(DocumentSnapshot snapshot, string field, int fallback)
    {
        return snapshot.ContainsField(field) ? snapshot.GetValue<int>(field) : fallback;
    }

    private static bool ReadBool(DocumentSnapshot snapshot, string field, bool fallback)
    {
        return snapshot.ContainsField(field) ? snapshot.GetValue<bool>(field) : fallback;
    }

    private static string ReadString(DocumentSnapshot snapshot, string field, string fallback)
    {
        return snapshot.ContainsField(field) ? snapshot.GetValue<string>(field) : fallback;
    }

    private static T ParseEnum<T>(string raw, T fallback) where T : struct
    {
        return Enum.TryParse(raw, out T parsed) ? parsed : fallback;
    }

    private static List<int> ParseIntList(DocumentSnapshot snapshot, string field)
    {
        if (!snapshot.ContainsField(field))
            return new List<int>();

        object raw = snapshot.GetValue<object>(field);
        if (raw is IEnumerable<object> objects)
            return objects.Select(Convert.ToInt32).ToList();

        if (raw is IEnumerable<int> ints)
            return ints.ToList();

        return new List<int>();
    }

    private static List<string> ParseStringList(DocumentSnapshot snapshot, string field)
    {
        if (!snapshot.ContainsField(field))
            return new List<string>();

        object raw = snapshot.GetValue<object>(field);
        if (raw is IEnumerable<object> objects)
            return objects.Select(value => value?.ToString() ?? string.Empty).Where(value => !string.IsNullOrWhiteSpace(value)).ToList();

        if (raw is IEnumerable<string> strings)
            return strings.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();

        return new List<string>();
    }

    private static List<Dictionary<string, object>> SerializeSteps(List<PathOfPowerStepData> steps)
    {
        if (steps == null)
            return new List<Dictionary<string, object>>();

        return steps.Select(step => new Dictionary<string, object>
        {
            { "stepIndex", step.stepIndex },
            { "stepType", step.stepType.ToString() },
            { "eventId", step.eventId ?? string.Empty },
            { "encounterId", step.encounterId ?? string.Empty },
            { "completed", step.completed }
        }).ToList();
    }

    private static List<PathOfPowerStepData> ParseSteps(DocumentSnapshot snapshot)
    {
        if (!snapshot.ContainsField(FloorStepsField))
            return new List<PathOfPowerStepData>();

        object raw = snapshot.GetValue<object>(FloorStepsField);
        IEnumerable<object> objects = raw as IEnumerable<object>;
        if (objects == null)
            return new List<PathOfPowerStepData>();

        List<PathOfPowerStepData> steps = new List<PathOfPowerStepData>();
        foreach (object item in objects)
        {
            Dictionary<string, object> map = item as Dictionary<string, object>;
            if (map == null)
                continue;

            steps.Add(new PathOfPowerStepData
            {
                stepIndex = map.TryGetValue("stepIndex", out object stepIndex) ? Convert.ToInt32(stepIndex) : 1,
                stepType = map.TryGetValue("stepType", out object stepType) ? ParseEnum(stepType.ToString(), PathOfPowerStepType.Fight) : PathOfPowerStepType.Fight,
                eventId = map.TryGetValue("eventId", out object eventId) ? eventId?.ToString() ?? string.Empty : string.Empty,
                encounterId = map.TryGetValue("encounterId", out object encounterId) ? encounterId?.ToString() ?? string.Empty : string.Empty,
                completed = map.TryGetValue("completed", out object completed) && Convert.ToBoolean(completed)
            });
        }

        return steps;
    }

    private static void EnsureLists(PathOfPowerRunData runData)
    {
        if (runData == null)
            return;

        runData.currentDeck ??= new List<int>();
        runData.currentRelics ??= new List<string>();
        runData.currentFloorSteps ??= new List<PathOfPowerStepData>();
        runData.pendingStarterRelicChoices ??= new List<string>();
        runData.pendingWardenRelicRewards ??= new List<string>();
        runData.pendingCardChoices ??= new List<int>();
        runData.activeEnemyRelics ??= new List<int>();
        runData.activeEnemyRelicTexts ??= new List<string>();
    }
}
