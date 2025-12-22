using UnityEngine;
using TMPro;

public class LoginUIController : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;

    public void OnPlayClicked()
    {
        AuthManager.Instance.Initialize();
    }

    public void OnCreateAccountClicked()
    {
        AuthManager.Instance.LinkAnonymousAccount(
            emailInput.text,
            passwordInput.text
        );
    }

    public void OnLoginClicked()
    {
        AuthManager.Instance.SignInWithEmail(
            emailInput.text,
            passwordInput.text
        );
    }
}
