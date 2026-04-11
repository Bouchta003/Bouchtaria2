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

    }

    private void OnDestroy()
    {
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

        playerDeckDropdown.value = 0;
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
        string selectedDeckName =
            playerDeckDropdown.options[playerDeckDropdown.value].text;

        List<int> selectedDeck = userDecks[selectedDeckName];

        DeckSelectionCache.SelectedPlayerDeck = selectedDeck;
        return selectedDeck;
    }
    public void StartBattle()
    {
        string selectedDeckName =
            playerDeckDropdown.options[playerDeckDropdown.value].text;

        List<int> selectedDeck = userDecks[selectedDeckName];

        DeckSelectionCache.SelectedPlayerDeck = selectedDeck;

        SceneManager.LoadScene(combatSceneName);
    }
    public void MainMenu()
    {
        SceneManager.LoadScene("Main_Menu");
    }
}
