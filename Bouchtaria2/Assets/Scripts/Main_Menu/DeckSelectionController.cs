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
    0,0,        // Starter Choice
    16,16,      // Mudkip
    19,19,      // Chimchar
    22,22,      // Snivy

    25,25,      // Easy Encounter
    26,26,      // Metapod
    40,40,      // Beldum

    54,54,      // Dormis
    55,55,      // Darkrai
    56,56,      // Wigglytuff
    57,57,      // Snorlax

    87,87,      // Ditto
    88,88,      // Rainbow Card
    89,89,      // Frog
    42,42       // Pizza Franprix
};
    List<int> enemyDeck1 = new List<int>
{
    3,3,        // Palico
    4,4,        // Gemma
    5,5,        // Alma

    6,6,        // By My Own Order

    34,34,      // Balahara
    9,9,        // Odogaron

    30,30,      // Rathalos
    32,32,      // Rathian

    36,36,      // Greatsword Hunter
    37,37,      // Insectglaive Hunter
    38,38,      // Sword'n'Shield Hunter

    103,103,    // Stygian Zinogre
    104,104,    // Rey Dau

    88,88,      // Rainbow Card
    89,89       // Frog
};
    List<int> enemyDeck2 = new List<int>
{
    60,60,      // Duaa
    61,61,      // Sadaqa

    63,63,      // Hijabi
    64,64,      // Hijab

    65,65,      // Armor Clad Faith
    66,66,      // Potemslim

    67,67,      // Awrah Man
    68,68,      // Dans l'din

    106,106,    // Bearer of Sabr
    107,107,    // Protector of the Ummah
    108,108,    // Guardian of Niyyah

    109,109,    // Seeker of Ilm
    110,110,    // Voice Of Dhikr
    111,111,    // Tawakkul

    88,88       // Rainbow Card
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
