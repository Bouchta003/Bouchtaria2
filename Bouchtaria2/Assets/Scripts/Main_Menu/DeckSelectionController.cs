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
};
    List<int> enemyDeck1 = new List<int>
{
    3,3,        // Palico
    4,4,        // Gemma
    5,5,        // Alma

    6,6,        // By My Own Order

    9,9,        // Odogaron

    30,30,      // Rathalos
    32,32,      // Rathian
    34,34,      // Balahara

    36,36,      // Greatsword Hunter
    37,37,      // Insectglaive Hunter
    38,38       // Sword'n'Shield Hunter
};
    List<int> enemyDeck2 = new List<int>
{
    8,8,        // Choupitout
    42,42,      // Pizza Franprix
    44,44,      // Lasagna Lunch

    45,45,      // Faust
    46,46,      // Faust's Flower

    47,47,      // Bouchta BBQ
    48,48,      // Invisible Girl

    49,49,      // Io
    51,51,      // Doc
    52,52,      // Blissey

    53,53       // Blood Bending
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
