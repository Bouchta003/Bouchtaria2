using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class LoginUIController : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button createAccountButton;
    [SerializeField] private Button playButton;
    [SerializeField] private GameObject logregisterWindow;
    [SerializeField] private GameObject DisplayedNamePanel;
    [SerializeField] private TMP_InputField DisplayedNameInput;
    public static LoginUIController Instance;

    void Start()
    {
        SetDisplayedNamePanelState(false);
        EventSystem.current.SetSelectedGameObject(emailInput.gameObject);
        RefreshDisplayNamePanelState();
    }

    private void OnEnable()
    {
        if (AuthManager.Instance != null)
            AuthManager.Instance.AuthReady += OnAuthReady;
    }

    private void OnDisable()
    {
        if (AuthManager.Instance != null)
            AuthManager.Instance.AuthReady -= OnAuthReady;
    }

    private void OnAuthReady(string uid)
    {
        StartCoroutine(RefreshDisplayNamePanelWhenReady());
    }

    private IEnumerator RefreshDisplayNamePanelWhenReady()
    {
        while (FirestoreManager.Instance == null || !FirestoreManager.Instance.IsReady)
            yield return null;

        RefreshDisplayNamePanelState();
    }

    private void SetDisplayedNamePanelState(bool isActive)
    {
        DisplayedNamePanel.SetActive(isActive);

        if (playButton != null)
            playButton.interactable = !isActive;
    }

    public void RefreshDisplayNamePanelState()
    {
        if (AuthManager.Instance == null || AuthManager.Instance.CurrentUser == null)
        {
            SetDisplayedNamePanelState(false);
            return;
        }

        if (FirestoreManager.Instance == null || !FirestoreManager.Instance.IsReady)
            return;

        string uid = AuthManager.Instance.CurrentUser.UserId;
        FirebaseFirestore.DefaultInstance.Collection("users").Document(uid)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("❌ Failed to read displayName");
                    Debug.LogException(task.Exception);
                    return;
                }

                DocumentSnapshot snapshot = task.Result;

                bool shouldAskForName = !snapshot.Exists ||
                    !snapshot.ContainsField("displayName") ||
                    string.IsNullOrWhiteSpace(snapshot.GetValue<string>("displayName")) ||
                    snapshot.GetValue<string>("displayName") == "Anonymous";

                SetDisplayedNamePanelState(shouldAskForName);
            });
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
        if (AuthManager.Instance == null || AuthManager.Instance.CurrentUser == null)
            return;

        string enteredName = DisplayedNameInput.text?.Trim();
        if (!string.IsNullOrWhiteSpace(enteredName) && enteredName.Length > 1)
        {
            string uid = AuthManager.Instance.CurrentUser.UserId;
            var updates = new Dictionary<string, object>
            {
                { "displayName", enteredName }
            };

            FirebaseFirestore.DefaultInstance.Collection("users").Document(uid)
                .SetAsync(updates, SetOptions.MergeAll)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError("❌ Failed to update displayName");
                        Debug.LogException(task.Exception);
                        return;
                    }

                    SetDisplayedNamePanelState(false);
                });
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
