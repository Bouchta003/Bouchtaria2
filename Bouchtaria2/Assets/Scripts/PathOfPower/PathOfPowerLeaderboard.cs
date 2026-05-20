using System;
using System.Collections.Generic;
using System.Linq;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

public class PathOfPowerLeaderboard : MonoBehaviour
{
    [Serializable]
    public class LeaderboardPlayerData
    {
        public int floor;
        public int step;
        public string displayName;
        public List<string> deck;
    }

    private readonly List<LeaderboardPlayerData> rankedPlayers = new List<LeaderboardPlayerData>();

    private void OnEnable()
    {
        LoadLeaderboard();
    }

    public void LoadLeaderboard(Action onLoaded = null)
    {
        FirebaseFirestore.DefaultInstance.Collection("users").GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                rankedPlayers.Clear();
                if (task.IsFaulted || task.Result == null)
                {
                    onLoaded?.Invoke();
                    return;
                }

                foreach (DocumentSnapshot doc in task.Result.Documents)
                {
                    int floor = doc.ContainsField(PathOfPowerSaveService.BestFloorField) ? doc.GetValue<int>(PathOfPowerSaveService.BestFloorField) : 0;
                    int step = doc.ContainsField(PathOfPowerSaveService.BestStepField) ? doc.GetValue<int>(PathOfPowerSaveService.BestStepField) : 0;
                    if (floor <= 0)
                        continue;

                    List<string> deckNames = BuildDeckCardNames(doc);
                    string displayName = doc.ContainsField("displayName") ? doc.GetValue<string>("displayName") : doc.Id;

                    rankedPlayers.Add(new LeaderboardPlayerData
                    {
                        floor = floor,
                        step = step,
                        displayName = displayName,
                        deck = deckNames
                    });
                }

                rankedPlayers.Sort((a, b) =>
                {
                    int floorCmp = b.floor.CompareTo(a.floor);
                    return floorCmp != 0 ? floorCmp : b.step.CompareTo(a.step);
                });

                onLoaded?.Invoke();
            });
    }

    public LeaderboardPlayerData FetchPlayerDataRank(int rank)
    {
        if (rank < 0 || rank >= rankedPlayers.Count)
            return null;

        return rankedPlayers[rank];
    }

    private static List<string> BuildDeckCardNames(DocumentSnapshot doc)
    {
        List<string> names = new List<string>();
        if (!doc.ContainsField(PathOfPowerSaveService.BestDeckField))
            return names;

        object rawDeck = doc.GetValue<object>(PathOfPowerSaveService.BestDeckField);
        IEnumerable<object> cardIds = rawDeck as IEnumerable<object>;
        if (cardIds == null)
            return names;

        foreach (object raw in cardIds)
        {
            int cardId = Convert.ToInt32(raw);
            if (CardDatabase.Instance != null && CardDatabase.Instance.Cards.TryGetValue(cardId, out CardData card))
                names.Add(card.name);
            else
                names.Add($"Card {cardId}");
        }

        return names;
    }
}
