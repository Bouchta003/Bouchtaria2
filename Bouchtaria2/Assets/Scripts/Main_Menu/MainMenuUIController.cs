using UnityEngine;

public class MainMenuUIController : MonoBehaviour
{
    private void Start()
    {
        GameRunContext.IsDungeonRun = false;
        GameRunContext.IsPathOfPowerRun = false;
        GameRunContext.IsAdventureCombat = false;
        GameRunContext.AdventureFightId = 0;
        GameRunContext.IsAdventureHardMode = false;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) OnDungeonlicked();
    }
    public void OnPlayCollectionClicked()
    {
        GameFlowController.Instance.GoToCollection();
    }
    public void OnDungeonlicked()
    {
        GameFlowController.Instance.GoToDungeon();
    }
    public void OnPathOfPowerClicked()
    {
        GameFlowController.Instance.GoToPathOfPower();
    }
    public void OnAdventureClicked()
    {
        GameFlowController.Instance.GoAdventureStage1();
    }
    public void OnDuelClicked()
    {
        GameFlowController.Instance.GoToCombat();
    }
    public void OnShopClicked()
    {
        GameFlowController.Instance.GoToShop();
    }
    public void OnBackToTitleClicked()
    {
        GameFlowController.Instance.GoToTitleScreen();
    }
}
