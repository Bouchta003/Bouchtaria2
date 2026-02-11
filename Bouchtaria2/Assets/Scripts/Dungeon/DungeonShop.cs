using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
public class DungeonShop : MonoBehaviour
{
    public static DungeonShop Instance;

    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI CoinText;
    [SerializeField] Image CoinImage;
    public enum Augment
    {
        MaxHP, StartMana
    }
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetUserCurrentCoin(coin =>
        {
            Debug.Log("User coin: " + coin); CoinText.text = coin.ToString();
            if (coin >= 100) CoinImage.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        });
    }

    public void ModifyUserCoin(int delta)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        db.Collection("users")
          .Document(user.UserId)
          .UpdateAsync("coin", FieldValue.Increment(delta))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to modify coin.");
                    return;
                }

                GetUserCurrentCoin(coin =>
                {
                    Debug.Log("User coin: " + coin); CoinText.text = coin.ToString();
                    if (coin >= 100) CoinImage.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                });
            });

    }
    public void ClickAugment(int aug)
    {
        Debug.Log(aug + "clicked");

        if (DungeonManager.Instance == null || DungeonManager.Instance.CurrentRun == null)
        {
            ErrorPopup.Show("No active dungeon run found.");
            return;
        }

        switch (aug)
        {
            case -1:
                if (DungeonManager.Instance.CurrentRun.coins < 75)
                {
                    ErrorPopup.Show("Not enough dungeon coins.");
                    break;
                }

                ModifyUserCoin(-100);
                DungeonManager.Instance.CurrentRun.coins -= 75;
                int rand = UnityEngine.Random.Range(0, 2);
                ClickAugment(rand);
                break;
            case 0:
                if (DungeonManager.Instance.CurrentRun.coins < 50)
                {
                    ErrorPopup.Show("Not enough dungeon coins.");
                    break;
                }

                ModifyUserCoin(-10);
                DungeonManager.Instance.CurrentRun.coins -= 50;
                DungeonManager.Instance.CurrentRun.augments.Add(Augment.MaxHP);
                DungeonManager.Instance.SaveRunData();
                break;
            case 1:
                if (DungeonManager.Instance.CurrentRun.coins < 100)
                {
                    ErrorPopup.Show("Not enough dungeon coins.");
                    break;
                }

                ModifyUserCoin(-50);
                DungeonManager.Instance.CurrentRun.coins -= 100;
                DungeonManager.Instance.CurrentRun.augments.Add(Augment.StartMana);
                DungeonManager.Instance.SaveRunData();
                break;
            default:
                ErrorPopup.Show("Unkown augment ID " + aug);break;
        }
        DungeonManager.Instance.RefreshAugmentCount();
    }
    public void LeaveToDungeonMenu()
    {
        SceneManager.LoadScene("DungeonMenu");
    }
    public void GetUserCurrentCoin(Action<int> onResult)
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
                  ErrorPopup.Show("Failed to fetch user coin.");
                  onResult?.Invoke(0);
                  return;
              }

              int dust = task.Result.ContainsField("coin")
                  ? task.Result.GetValue<int>("coin")
                  : 0;

              onResult?.Invoke(dust);
          });
    }
}
