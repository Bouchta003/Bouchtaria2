using UnityEngine;

public class MainMenuUIController : MonoBehaviour
{
    public void OnPlayCollectionClicked()
    {
        GameFlowController.Instance.GoToCollection();
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
