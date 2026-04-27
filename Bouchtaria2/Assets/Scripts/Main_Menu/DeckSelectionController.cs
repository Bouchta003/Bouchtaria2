using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeckSelectionController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown playerDeckDropdown;
    [SerializeField] private string combatSceneName = "Combat";

    private Dictionary<string, List<int>> userDecks;

    List<int> enemyDeck0 = new List<int>
    {
        58, 58, 60, 60, 61, 61, 63, 63, 64, 64,
        65, 65, 66, 66, 67, 67, 68, 68, 106, 106,
        107, 107, 108, 108, 109, 109, 110, 110, 111, 111
    };
    List<int> enemyDeck1 = new List<int>
    {
        16, 16, 19, 19, 22, 22, 25, 25, 26, 26,
        52, 52, 55, 55, 56, 56, 57, 57, 87, 87,
        144, 144, 146, 146, 147, 147, 148, 148, 150, 150
    };
    List<int> enemyDeck2 = new List<int>
    {
        80, 80, 81, 81, 82, 82, 83, 83, 84, 84,
        134, 134, 135, 135, 136, 136, 137, 137, 138, 138,
        139, 139, 145, 145, 155, 155, 168, 168, 176, 176
    };

    private void Start()
    {
        DeckBuilding.Instance.OnDecksLoaded += PopulateDropdown;
        DeckBuilding.Instance.FetchDecks();
        playerDeckDropdown.onValueChanged.AddListener(OnDropdownSelectionChanged);

    }

    private void OnDestroy()
    {
        if (playerDeckDropdown != null)
            playerDeckDropdown.onValueChanged.RemoveListener(OnDropdownSelectionChanged);

        if (DeckBuilding.Instance != null)
            DeckBuilding.Instance.OnDecksLoaded -= PopulateDropdown;
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("Main_Menu");
    }
    private void PopulateDropdown()
    {
        userDecks = DeckBuilding.Instance.GetUserDecks();

        playerDeckDropdown.ClearOptions();

        List<string> options = new List<string>(userDecks.Keys);
        playerDeckDropdown.AddOptions(options);

        int selectedIndex = 0;
        if (!string.IsNullOrWhiteSpace(DeckSelectionCache.LastSelectedDeckName))
        {
            int preferredIndex = options.IndexOf(DeckSelectionCache.LastSelectedDeckName);
            if (preferredIndex >= 0 &&
                userDecks.TryGetValue(DeckSelectionCache.LastSelectedDeckName, out List<int> preferredDeck) &&
                DeckSelectionCache.IsDeckValidForStandardCombat(preferredDeck))
            {
                selectedIndex = preferredIndex;
            }
        }

        playerDeckDropdown.SetValueWithoutNotify(selectedIndex);
        playerDeckDropdown.RefreshShownValue();

        Debug.Log($"[DeckSelection] Loaded {options.Count} decks");
        foreach (var name in userDecks.Keys)
            Debug.Log($"Deck found: {name}");

    }
    public void ChooseEnemy(int index)
    {
        Debug.Log("clicked : " + index);
        switch (index)
        {
            case 0:
                DeckSelectionCache.SelectedEnemyDeck = enemyDeck0;
                break;
            case 1:
                DeckSelectionCache.SelectedEnemyDeck = enemyDeck1;
                break;
            case 2:
                DeckSelectionCache.SelectedEnemyDeck = enemyDeck2;
                break;
        }
        StartBattle();
    }
    public List<int> GetSelectedUserDeck()
    {
        if (playerDeckDropdown.options.Count == 0)
            return new List<int>();

        string selectedDeckName =
            playerDeckDropdown.options[playerDeckDropdown.value].text;

        List<int> selectedDeck = userDecks[selectedDeckName];

        DeckSelectionCache.RememberDeckSelection(selectedDeckName, selectedDeck);
        DeckSelectionCache.SelectedPlayerDeck = selectedDeck;
        return selectedDeck;
    }
    public void StartBattle()
    {
        if (playerDeckDropdown.options.Count == 0)
            return;

        string selectedDeckName =
            playerDeckDropdown.options[playerDeckDropdown.value].text;

        List<int> selectedDeck = userDecks[selectedDeckName];

        DeckSelectionCache.RememberDeckSelection(selectedDeckName, selectedDeck);
        DeckSelectionCache.SelectedPlayerDeck = selectedDeck;

        SceneManager.LoadScene(combatSceneName);
    }
    private void OnDropdownSelectionChanged(int index)
    {
        if (userDecks == null || index < 0 || index >= playerDeckDropdown.options.Count)
            return;

        string selectedDeckName = playerDeckDropdown.options[index].text;
        if (userDecks.TryGetValue(selectedDeckName, out List<int> selectedDeck))
            DeckSelectionCache.RememberDeckSelection(selectedDeckName, selectedDeck);
    }
    public void MainMenu()
    {
        SceneManager.LoadScene("Main_Menu");
    }
}
