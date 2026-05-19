using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PathOfPowerEnemyDeckBuilder
{
    private const int MaxCopiesPerCard = 2;

    public static List<int> BuildEnemyDeck(PathOfPowerRunData runData, EnemyEncounterDefinition encounter = null)
    {
        int floor = Mathf.Max(1, runData?.currentFloor ?? 1);
        int targetSize = Mathf.Max(1, runData?.currentDeck?.Count ?? 30);
        bool allowUnpackableAndTier3 = floor >= 3;

        IReadOnlyList<int> parsedEncounterDeck = encounter?.GetParsedEnemyDeckTemplate();
        IEnumerable<int> template = parsedEncounterDeck != null && parsedEncounterDeck.Count > 0
            ? parsedEncounterDeck
            : EnemyDecks.GetFloorDeck(floor);

        List<int> result = new List<int>();
        Dictionary<int, int> counts = new Dictionary<int, int>();

        foreach (int cardId in template)
            TryAddCard(cardId, result, counts, targetSize, allowUnpackableAndTier3);

        int fallbackFloor = floor;
        while (result.Count < targetSize && fallbackFloor > 0)
        {
            foreach (int cardId in EnemyDecks.GetFloorDeck(fallbackFloor))
            {
                TryAddCard(cardId, result, counts, targetSize, allowUnpackableAndTier3);
                if (result.Count >= targetSize)
                    break;
            }

            fallbackFloor--;
        }

        // Last resort: keep combat launchable even when the database cannot validate enough cards in editor/test scenes.
        foreach (int cardId in template)
        {
            if (result.Count >= targetSize)
                break;

            if (!counts.ContainsKey(cardId))
                counts[cardId] = 0;

            if (counts[cardId] >= MaxCopiesPerCard)
                continue;

            result.Add(cardId);
            counts[cardId]++;
        }

        return result.Take(targetSize).ToList();
    }

    private static void TryAddCard(int cardId, List<int> result, Dictionary<int, int> counts, int targetSize, bool allowUnpackableAndTier3)
    {
        if (result.Count >= targetSize)
            return;

        if (!counts.ContainsKey(cardId))
            counts[cardId] = 0;

        if (counts[cardId] >= MaxCopiesPerCard)
            return;

        CardData card = null;
        if (CardDatabase.Instance != null && CardDatabase.Instance.Cards != null)
            CardDatabase.Instance.Cards.TryGetValue(cardId, out card);

        if (!allowUnpackableAndTier3 && card != null && !card.packable)
            return;

        result.Add(cardId);
        counts[cardId]++;
    }
}
