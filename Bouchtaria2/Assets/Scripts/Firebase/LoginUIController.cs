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
    [SerializeField] private GameObject logregisterWindow;
    [SerializeField] private GameObject DisplayedNamePanel;
    [SerializeField] private TMP_InputField DisplayedNameInput;
    public static LoginUIController Instance;
    void Start()
    {
        DisplayedNamePanel.SetActive(false);
        EventSystem.current.SetSelectedGameObject(emailInput.gameObject);
    }
    public void OnGuestClicked()
    {
        AuthManager.Instance.SignInAnonymously();
    }
    public void ToggleAccountLoginCreation()
    {
        logregisterWindow.SetActive(!logregisterWindow.activeSelf);
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
    public void ConfirmDisplayName()
    {
        if(DisplayedNameInput.text!=null && DisplayedNameInput.text.Length > 1)
        {
            //Update display name in firestore
            DisplayedNamePanel.SetActive(false);
        }
    }
    public static void CloseGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OnLoginClicked()
    {
        AuthManager.Instance.SignInWithEmail(
            emailInput.text,
            passwordInput.text
        );
    }
}
