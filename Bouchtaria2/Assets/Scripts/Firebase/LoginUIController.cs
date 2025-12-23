using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase.Auth;
using UnityEngine.EventSystems;

public class LoginUIController : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button createAccountButton;
    public static LoginUIController Instance;
    void Start()
    {
        EventSystem.current.SetSelectedGameObject(emailInput.gameObject);
    }
    public void OnGuestClicked()
    {
        AuthManager.Instance.SignInAnonymously();
    }

    public void OnCreateAccountClicked()
    {
        AuthManager.Instance.CreateOrLinkAccount(
               emailInput.text,
               passwordInput.text
           );
    }
    public void PlayButton()
    {
        if (AuthManager.Instance.CurrentUser != null)
        {
            GameFlowController.Instance.GoToMainMenu();
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
