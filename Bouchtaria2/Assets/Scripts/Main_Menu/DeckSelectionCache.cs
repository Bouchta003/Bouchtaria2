using System.Collections.Generic;
using System.Linq;

public static class DeckSelectionCache
{
    public static List<int> SelectedPlayerDeck;
    public static List<int> SelectedEnemyDeck;
    public static string LastSelectedDeckName;

    private const int StandardCombatDeckSize = 30;

    public static bool IsDeckValidForStandardCombat(List<int> deck)
    {
        if (deck == null || deck.Count != StandardCombatDeckSize)
            return false;

        return deck.GroupBy(id => id).All(group => group.Count() <= 2);
    }

    public static void RememberDeckSelection(string deckName, List<int> deck)
    {
        if (GameRunContext.IsDungeonRun)
            return;

        if (string.IsNullOrWhiteSpace(deckName))
            return;

        if (!IsDeckValidForStandardCombat(deck))
            return;

        LastSelectedDeckName = deckName;
    }
}
