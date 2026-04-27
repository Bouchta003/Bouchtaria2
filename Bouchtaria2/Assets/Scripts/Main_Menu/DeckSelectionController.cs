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
        333,333,289,289,291,291,290,290,292,292,293,293,335,335,337,337,343,343,342,342
        ,339,339,91,91,317,317,351,351,88,88
    };
    List<int> enemyDeck1 = new List<int>
    {
        58,58,59,59,162,162,269,269,60,60,88,88,111,111,170,170,109,109,158,158,65,65,
        146,146,241,241,142,142,89,89,
    };
    List<int> enemyDeck2 = new List<int>
    {
        316,316,321,321,313,313,325,325,141,141,97,97,308,308,
        168,168,175,175,183,183,170,170,227,227,363,363,238,238,174,174,
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
