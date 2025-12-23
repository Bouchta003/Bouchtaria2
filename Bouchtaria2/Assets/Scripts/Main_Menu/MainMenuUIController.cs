using UnityEngine;

public class MainMenuUIController : MonoBehaviour
{
    public void OnPlayCollectionClicked()
    {
        GameFlowController.Instance.GoToCollection();
    }

    public void OnBackToTitleClicked()
    {
        GameFlowController.Instance.GoToTitleScreen();
    }
}
