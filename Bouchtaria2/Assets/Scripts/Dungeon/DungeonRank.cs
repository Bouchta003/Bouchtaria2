using System.Collections.Generic;
using System.Linq;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;

public class DungeonRank : MonoBehaviour
{
    [Header("Streak Texts")]
    [SerializeField] TextMeshProUGUI FirstRankText;
    [SerializeField] TextMeshProUGUI SecondRankText;
    [SerializeField] TextMeshProUGUI ThirdRankText;
    [SerializeField] TextMeshProUGUI CurrentStreakText;

    private Dictionary<string, int> orderedBestStreaks = new Dictionary<string, int>();

    private void Start()
    {
        ClearRankTexts();
        FetchAndDisplayRanks();
        FetchAndDisplayCurrentUserBestStreak();
    }

    private void ClearRankTexts()
    {
        if (FirstRankText != null)
            FirstRankText.text = "-";

        if (SecondRankText != null)
            SecondRankText.text = "-";

        if (ThirdRankText != null)
            ThirdRankText.text = "-";

        if (CurrentStreakText != null)
            CurrentStreakText.text = "-";
    }

    private void FetchAndDisplayRanks()
    {
        FirebaseFirestore.DefaultInstance
            .Collection("users")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("❌ Failed to fetch dungeon ranks.");
                    return;
                }

                QuerySnapshot snapshot = task.Result;
                List<KeyValuePair<string, int>> playerBestStreaks = new List<KeyValuePair<string, int>>();

                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    int bestStreak = GetIntField(document, "beststreak");
                    if (bestStreak <= 1)
                        continue;

                    string displayName = GetDisplayName(document);
                    playerBestStreaks.Add(new KeyValuePair<string, int>(displayName, bestStreak));
                }

                orderedBestStreaks = playerBestStreaks
                    .GroupBy(entry => entry.Key)
                    .Select(group => new KeyValuePair<string, int>(group.Key, group.Max(entry => entry.Value)))
                    .OrderByDescending(entry => entry.Value)
                    .ThenBy(entry => entry.Key)
                    .ToDictionary(entry => entry.Key, entry => entry.Value);

                List<KeyValuePair<string, int>> topRanks = orderedBestStreaks.Take(3).ToList();

                if (FirstRankText != null)
                    FirstRankText.text = topRanks.Count > 0 ? FormatRank(topRanks[0]) : "-";

                if (SecondRankText != null)
                    SecondRankText.text = topRanks.Count > 1 ? FormatRank(topRanks[1]) : "-";

                if (ThirdRankText != null)
                    ThirdRankText.text = topRanks.Count > 2 ? FormatRank(topRanks[2]) : "-";
            });
    }

    private void FetchAndDisplayCurrentUserBestStreak()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            if (CurrentStreakText != null)
                CurrentStreakText.text = "-";
            return;
        }

        FirebaseFirestore.DefaultInstance
            .Collection("users")
            .Document(user.UserId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled || !task.Result.Exists)
                {
                    Debug.LogError("❌ Failed to fetch current user best streak.");
                    if (CurrentStreakText != null)
                        CurrentStreakText.text = "-";
                    return;
                }

                int bestStreak = GetIntField(task.Result, "beststreak");
                if (CurrentStreakText != null)
                    CurrentStreakText.text = bestStreak.ToString();
            });
    }

    private static int GetIntField(DocumentSnapshot document, string fieldName)
    {
        if (!document.ContainsField(fieldName))
            return 0;

        object rawValue = document.GetValue<object>(fieldName);
        if (rawValue is long longValue)
            return (int)longValue;

        if (rawValue is int intValue)
            return intValue;

        return 0;
    }

    private static string GetDisplayName(DocumentSnapshot document)
    {
        if (!document.ContainsField("displayName"))
            return "Anonymous";

        string displayName = document.GetValue<string>("displayName");
        return string.IsNullOrWhiteSpace(displayName) ? "Anonymous" : displayName;
    }

    private static string FormatRank(KeyValuePair<string, int> rankEntry)
    {
        return $"{rankEntry.Key} - {rankEntry.Value}";
    }
}
