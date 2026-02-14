using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class DungeonShop : MonoBehaviour
{
    public static DungeonShop Instance;

    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI CoinText;
    [SerializeField] TextMeshProUGUI ColonelText;
    [SerializeField] GameObject SpeechBubbleGO;
    [SerializeField] Image CoinImage;

    private Coroutine speechBubbleCoroutine;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float displayDuration = 4f;
    [SerializeField] private float typeSpeed = 0.02f;

    private CanvasGroup canvasGroup;
    private Coroutine speechRoutine;
    private bool ShouldPay;

    public enum Augment
    {
        MaxHP, StartMana, StartDraw, DeckSizeUp3, DeckSizeDown3
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
        canvasGroup = SpeechBubbleGO.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = SpeechBubbleGO.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        SpeechBubbleGO.SetActive(false);

        GetUserCurrentCoin(coin =>
        {
            CoinText.text = coin.ToString();
            if (coin >= 100)
                CoinImage.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        });

        ShouldPay = true;
    }

    public void DisplayItemDescription(int itemIndex)
    {
        string message = "";

        switch (itemIndex)
        {
            case -1:
                message = "Wanna gamble for a random item ? Don't worry the ones you can get are 50 coins or higher. Feeling lucky ?";
                break;
            case -2:
                message = "Ya deck too tiny ? With this you'll get three more slots. On the colonel !";
                break;
            case -3:
                message = "Too much cards in your deck? Buy this and I'll make three of them disappear !\n*poof*";
                break;
            case 0:
                message = "Get some more health before your fight, I sell 5 HP per buy, what do you say ?";
                break;
            case 1:
                message = "With this mana potion, you'll start your fights with a lil more mana than the enemy, pretty neat right ?";
                break;
            case 2:
                message = "Ya buy this one, ya draw one more card at each fight !";
                break;
            default:
                message = "Hummmm... I am not sure about this one, maybe in another patch I'll have some more info to share for this.";
                break;
        }
        ResetSpeechBubble();

        speechRoutine = StartCoroutine(SpeechBubbleSequence(message));
    }
    private IEnumerator SpeechBubbleSequence(string message)
    {
        SpeechBubbleGO.SetActive(true);

        yield return StartCoroutine(Fade(0, 1));

        yield return StartCoroutine(TypeText(message));

        yield return new WaitForSeconds(displayDuration);

        yield return StartCoroutine(Fade(1, 0));

        SpeechBubbleGO.SetActive(false);
    }
    private IEnumerator TypeText(string message)
    {
        ColonelText.text = message;
        ColonelText.maxVisibleCharacters = 0;

        while (ColonelText.maxVisibleCharacters < message.Length)
        {
            ColonelText.maxVisibleCharacters++;
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    private IEnumerator Fade(float start, float end)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, end, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = end;
    }
    private void ResetSpeechBubble()
    {
        if (speechRoutine != null)
        {
            StopCoroutine(speechRoutine);
            speechRoutine = null;
        }

        StopAllCoroutines(); // extra safety

        ColonelText.text = "";
        ColonelText.maxVisibleCharacters = 0;

        canvasGroup.alpha = 0f;
        SpeechBubbleGO.SetActive(false);
    }

    private IEnumerator HideSpeechBubbleAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        SpeechBubbleGO.SetActive(false);
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

                ModifyUserCoin(-75);
                DungeonManager.Instance.CurrentRun.coins -= 75;
                int rand = UnityEngine.Random.Range(0, 3);
                ShouldPay = false;
                ClickAugment(rand);
                break;
            case -2:
                if (ShouldPay && DungeonManager.Instance.CurrentRun.coins < 30)
                {
                    ErrorPopup.Show("Not enough dungeon coins.");
                    ShouldPay = true; break;
                }

                if(ShouldPay)ModifyUserCoin(-30);
                if(ShouldPay)DungeonManager.Instance.CurrentRun.coins -= 30;
                DungeonManager.Instance.CurrentRun.augments.Add(Augment.DeckSizeUp3);
                DungeonManager.Instance.SaveRunData();
                ShouldPay = true; break;
            case -3:
                if (ShouldPay && DungeonManager.Instance.CurrentRun.coins < 30 ||DungeonManager.Instance.CurrentRun.currentDeckSize<=5)
                {
                    ErrorPopup.Show("Not enough dungeon coins or deck too small already.");
                    ShouldPay = true; break;
                }

                if(ShouldPay)ModifyUserCoin(-30);
                if(ShouldPay)DungeonManager.Instance.CurrentRun.coins -= 30;
                DungeonManager.Instance.CurrentRun.augments.Add(Augment.DeckSizeDown3);
                DungeonManager.Instance.SaveRunData();
                ShouldPay = true; break;
            case 0:
                if (ShouldPay && DungeonManager.Instance.CurrentRun.coins < 50)
                {
                    ErrorPopup.Show("Not enough dungeon coins.");
                    ShouldPay = true; break;
                }

                if(ShouldPay)ModifyUserCoin(-50);
                if(ShouldPay)DungeonManager.Instance.CurrentRun.coins -= 50;
                DungeonManager.Instance.CurrentRun.augments.Add(Augment.MaxHP);
                DungeonManager.Instance.SaveRunData();
                ShouldPay = true; break;
            case 1:
                if (ShouldPay && DungeonManager.Instance.CurrentRun.coins < 120)
                {
                    ErrorPopup.Show("Not enough dungeon coins.");
                    ShouldPay = true; break;
                }

                if(ShouldPay)ModifyUserCoin(-120);
                if(ShouldPay)DungeonManager.Instance.CurrentRun.coins -= 120;
                DungeonManager.Instance.CurrentRun.augments.Add(Augment.StartMana);
                DungeonManager.Instance.SaveRunData();
                ShouldPay = true; break;
            case 2:
                if (ShouldPay && DungeonManager.Instance.CurrentRun.coins < 100)
                {
                    ErrorPopup.Show("Not enough dungeon coins.");
                    ShouldPay = true; break;
                }

                if(ShouldPay)ModifyUserCoin(-100);
                if(ShouldPay)DungeonManager.Instance.CurrentRun.coins -= 100;
                DungeonManager.Instance.CurrentRun.augments.Add(Augment.StartDraw);
                DungeonManager.Instance.SaveRunData();
                ShouldPay = true; break;
            default:
                ErrorPopup.Show("Unkown augment ID " + aug);ShouldPay = true; break;
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
