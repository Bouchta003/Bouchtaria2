using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoginUIController : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button createAccountButton;

    public void OnGuestClicked()
    {
        AuthManager.Instance.Initialize();
    }

    public void OnCreateAccountClicked()
    {
        AuthManager.Instance.CreateEmailAccount(
               emailInput.text,
               passwordInput.text
           );
    }
    public void PlayButton()
    {
        if (AuthManager.Instance.CurrentUser != null)
        {
            Debug.Log("SHould move to next panel");
            GameFlowController gameFlow = FindFirstObjectByType<GameFlowController>();
            gameFlow.MoveToCollection();
        }
    }


    public void OnLoginClicked()
    {
        AuthManager.Instance.SignInWithEmail(
            emailInput.text,
            passwordInput.text
        );
    }
}
