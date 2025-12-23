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
    [SerializeField] private TMP_Text uidText;
    private string lastUserId;
    [SerializeField] private GameObject successfullConnection;
    public static LoginUIController Instance;
    void Start()
    {
        EventSystem.current.SetSelectedGameObject(emailInput.gameObject);
    }
    public void RefreshAuthUI()
    {
        var user = AuthManager.Instance.CurrentUser;

        uidText.text = user != null
            ? $"UID: {user.UserId}"
            : "Not logged in";

        Debug.Log("🔄 Auth UI updated");
    }

    public void OnGuestClicked()
    {
        AuthManager.Instance.Initialize();
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
