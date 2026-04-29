using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeckSelectionController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown playerDeckDropdown;
    [SerializeField] private string combatSceneName = "Combat";

    private Dictionary<string, List<int>> userDecks = new();
    private readonly List<string> dropdownDeckNames = new();

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
        if (playerDeckDropdown != null)
            playerDeckDropdown.onValueChanged.AddListener(OnDropdownSelectionChanged);

        if (DeckBuilding.Instance == null)
        {
            Debug.LogError("[DeckSelection] DeckBuilding instance not found.");
            ShowEmptyDeckOption();
            return;
        }

        DeckBuilding.Instance.OnDecksLoaded += PopulateDropdown;
        DeckBuilding.Instance.FetchDecks();
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
        if (playerDeckDropdown == null || DeckBuilding.Instance == null)
            return;

        userDecks = DeckBuilding.Instance.GetUserDecks();
        dropdownDeckNames.Clear();

        if (userDecks.Count == 0)
        {
            ShowEmptyDeckOption();
            return;
        }

        dropdownDeckNames.AddRange(userDecks.Keys);
        dropdownDeckNames.Sort(System.StringComparer.OrdinalIgnoreCase);

        playerDeckDropdown.ClearOptions();
        playerDeckDropdown.AddOptions(new List<string>(dropdownDeckNames));

        int selectedIndex = 0;
        if (!string.IsNullOrWhiteSpace(DeckSelectionCache.LastSelectedDeckName))
        {
            int preferredIndex = dropdownDeckNames.IndexOf(DeckSelectionCache.LastSelectedDeckName);
            if (preferredIndex >= 0 &&
                userDecks.TryGetValue(DeckSelectionCache.LastSelectedDeckName, out List<int> preferredDeck) &&
                DeckSelectionCache.IsDeckValidForStandardCombat(preferredDeck))
            {
                selectedIndex = preferredIndex;
            }
        }

        playerDeckDropdown.SetValueWithoutNotify(selectedIndex);
        playerDeckDropdown.RefreshShownValue();
        OnDropdownSelectionChanged(selectedIndex);
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
        if (!TryGetSelectedDeck(out string selectedDeckName, out List<int> selectedDeck))
            return new List<int>();

        DeckSelectionCache.RememberDeckSelection(selectedDeckName, selectedDeck);
        DeckSelectionCache.SelectedPlayerDeck = selectedDeck;
        return new List<int>(selectedDeck);
    }
    public void StartBattle()
    {
        if (!TryGetSelectedDeck(out string selectedDeckName, out List<int> selectedDeck))
            return;

        DeckSelectionCache.RememberDeckSelection(selectedDeckName, selectedDeck);
        DeckSelectionCache.SelectedPlayerDeck = new List<int>(selectedDeck);

        SceneManager.LoadScene(combatSceneName);
    }
    private void OnDropdownSelectionChanged(int index)
    {
        if (userDecks == null || index < 0 || index >= dropdownDeckNames.Count)
            return;

        string selectedDeckName = dropdownDeckNames[index];
        if (userDecks.TryGetValue(selectedDeckName, out List<int> selectedDeck))
            DeckSelectionCache.RememberDeckSelection(selectedDeckName, selectedDeck);
    }

    private bool TryGetSelectedDeck(out string selectedDeckName, out List<int> selectedDeck)
    {
        selectedDeckName = string.Empty;
        selectedDeck = null;

        if (playerDeckDropdown == null || dropdownDeckNames.Count == 0)
            return false;

        int selectedIndex = playerDeckDropdown.value;
        if (selectedIndex < 0 || selectedIndex >= dropdownDeckNames.Count)
            return false;

        selectedDeckName = dropdownDeckNames[selectedIndex];
        return userDecks.TryGetValue(selectedDeckName, out selectedDeck);
    }

    private void ShowEmptyDeckOption()
    {
        if (playerDeckDropdown == null)
            return;

        dropdownDeckNames.Clear();
        playerDeckDropdown.ClearOptions();
        playerDeckDropdown.AddOptions(new List<string> { "No decks found" });
        playerDeckDropdown.SetValueWithoutNotify(0);
        playerDeckDropdown.RefreshShownValue();
    }
    public void MainMenu()
    {
        SceneManager.LoadScene("Main_Menu");
    }
}
