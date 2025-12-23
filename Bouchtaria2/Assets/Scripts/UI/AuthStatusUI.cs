using UnityEngine;
using TMPro;
using Firebase.Auth;

public class AuthStatusUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject userPanel;
    [SerializeField] private TMP_Text userText;

    private FirebaseAuth auth;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += OnAuthStateChanged;

        // Initial refresh (important)
        RefreshUI();
    }

    void OnDestroy()
    {
        if (auth != null)
            auth.StateChanged -= OnAuthStateChanged;
    }

    private void OnAuthStateChanged(object sender, System.EventArgs e)
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        var user = auth.CurrentUser;

        bool isConnected = user != null;
        userPanel.SetActive(isConnected);

        if (!isConnected)
            return;

        // Priority: email > UID
        if (!string.IsNullOrEmpty(user.Email))
        {
            userText.text = $"👤 {user.Email}";
        }
        else
        {
            userText.text = $"👤 Guest ({user.UserId})";
        }
    }
}
