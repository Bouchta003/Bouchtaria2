using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LeaderboardManager : MonoBehaviour
{
    [SerializeField] GameObject InformationPanel;
    [SerializeField] TextMeshProUGUI InformationTextDeck;
    [SerializeField] TextMeshProUGUI InformationTextRelics;


    [SerializeField] TextMeshProUGUI FirstName;
    [SerializeField] TextMeshProUGUI SecondName;
    [SerializeField] TextMeshProUGUI ThirdName;

    [SerializeField] TextMeshProUGUI FirstFloor;
    [SerializeField] TextMeshProUGUI SecondFloor;
    [SerializeField] TextMeshProUGUI ThirdFloor;

    [SerializeField] Image FirstImageMainTrait;
    [SerializeField] Image FirstImageSecondaryTrait;
    [SerializeField] Image SecondImageMainTrait;
    [SerializeField] Image SecondImageSecondaryTrait;
    [SerializeField] Image ThirdImageMainTrait;
    [SerializeField] Image ThirdImageSecondaryTrait;

    void Start()
    {
        //Populate the leaderboard with the top 3 players and their information which are their Name/Floor/Main Trait Sprite/Secondary Trait Sprite

    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            InformationPanel.SetActive(false);
        }
    }
    public void ToggleFirstInfo()
    {
        //Populate the information panel with the first player's information which is their Deck and Relics. Then toggle the information panel on and off.
        InformationPanel.SetActive(!InformationPanel.activeSelf);
    }
    public void ToggleSecondInfo()
    {
        //Populate the information panel with the second player's information which is their Deck and Relics. Then toggle the information panel on and off.
        InformationPanel.SetActive(!InformationPanel.activeSelf);   
    }
    public void ToggleThirdInfo()
    {
        //Populate the information panel with the third player's information which is their Deck and Relics. Then toggle the information panel on and off.
        InformationPanel.SetActive(!InformationPanel.activeSelf);
    }
    public void BackToMenu()
    {
        SceneManager.LoadScene("PathofPower");
    }
}
