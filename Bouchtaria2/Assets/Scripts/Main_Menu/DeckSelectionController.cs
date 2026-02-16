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
        126,126,126,126,126,126,126,128,128,128,128,128,128,128,126,126,126,126,126,126,126 // Hoopa portal
    };
    List<int> enemyDeck1 = new List<int>
{
    53,54,53,54,53,

};
    List<int> enemyDeck2 = new List<int>
{
    // ===== CHAOS CORE (all available Chaos cards) =====
    126,126,      // Colonel Whatsapp (Chaos – draw engine)
    127,127,      // Giratina Origin (Chaos – UNPACKABLE finisher)

    // ===== FAITH DRAW & VALUE =====
    129,129,      // Duaa (Faith – discover Faith)
    130,130,      // Sadaqa (Faith – draw + heal)

    109,109,    // Seeker of Ilm (Faith – repeat draw)
    67,67,      // Awrah Man (Faith – tempo draw)

    // ===== FAITH BOARD CONTROL =====
    58,58,      // No More Music ! (Faith – silence all enemies)
    68,68,      // Dans l'din (Faith – burn + discover)

    // ===== FAITH DEFENSIVE CORE =====
    63,63,      // Hijabi (Faith – Blessed body)
    64,64,      // Hijab (Faith – Blessed gear)

    65,65,      // Armor Clad Faith (Faith – armor + summon)
    66,66,      // Potemslim (Faith – Protect + Blessed)

    // ===== FAITH LATE GAME =====
    108,108,    // Guardian of Niyyah (Faith – sustain engine)
    106,106,    // Bearer of Sabr (Faith – resilient absorber)

    110,110     // Voice Of Dhikr (Faith – scaling finisher)
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
