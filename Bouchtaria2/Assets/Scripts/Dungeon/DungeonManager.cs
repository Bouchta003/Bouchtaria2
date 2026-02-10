using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class DungeonManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI StreakText;
    [SerializeField] Image StreakFire;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetUserCurrentStreak(streak =>
        {
            Debug.Log("User streak: " + streak);
            StreakText.text = streak.ToString();
            if (streak <= 1) StreakFire.gameObject.SetActive(false);
            else if(streak < 5) StreakFire.gameObject.SetActive(true);
            if (streak >= 5) StreakFire.transform.localScale = new Vector3(1.2f,1.2f,1.2f);

        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void GetUserCurrentStreak(Action<int> onResult)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            onResult?.Invoke(0);
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        db.Collection("users")
          .Document(user.UserId)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted || !task.Result.Exists)
              {
                  ErrorPopup.Show("Failed to fetch user streak.");
                  onResult?.Invoke(0);
                  return;
              }

              int dust = task.Result.ContainsField("streak")
                  ? task.Result.GetValue<int>("streak")
                  : 0;

              onResult?.Invoke(dust);
          });
    }
    public void LeaveToMenu()
    {
        SceneManager.LoadScene("Main_Menu");
    }
}
