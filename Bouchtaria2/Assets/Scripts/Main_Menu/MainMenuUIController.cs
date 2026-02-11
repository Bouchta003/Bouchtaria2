using UnityEngine;

public class MainMenuUIController : MonoBehaviour
{
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
