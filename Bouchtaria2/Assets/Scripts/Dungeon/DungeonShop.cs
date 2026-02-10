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
        MaxHP, StartMana, RandomAugment
    }
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

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

    // Update is called once per frame
    void Update()
    {
        
    }
    private void ModifyUserCoin(int delta)
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
        Debug.Log(aug+ "clicked") ;
        switch (aug)
        {
            case 0:
                ModifyUserCoin(-100);
                int rand = UnityEngine.Random.Range(1, 3);
                ClickAugment(rand);
                break;
            case 1:
                ModifyUserCoin(-10);
                DungeonManager.Instance.CurrentRun.augments.Add(Augment.MaxHP);
                break;
            case 2:
                ModifyUserCoin(-50);
                DungeonManager.Instance.CurrentRun.augments.Add(Augment.StartMana);
                break;
            default:
                ErrorPopup.Show("Unkown augment ID " + aug);break;
        }
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
