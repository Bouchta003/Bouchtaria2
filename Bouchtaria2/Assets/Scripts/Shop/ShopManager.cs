using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [SerializeField] TextMeshProUGUI GoldCounter;
    [SerializeField] TextMeshProUGUI DustCounter;
    [SerializeField] TextMeshProUGUI ProgressionCounter;
    int UserGold;
    int UserDust;

    CardData.Trait wish;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        //Start instance
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        UpdateGoldAndDust();
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("Main_Menu");
    }
    public void UpdateGoldAndDust()
    {
        //Update user gold
        GetUserGold(gold =>
        {
            Debug.Log("User gold: " + gold);
            UserGold = gold;
            GoldCounter.text = gold.ToString();
        });
        DeckBuilding.Instance.GetUserDust(dust =>
        {
            Debug.Log("User dust: " + dust);
            UserDust = dust;
            DustCounter.text = dust.ToString();
        });
    }
    #region Pack Management
    private void OpenPack(CardPack pack)
    {
        List<int> openedCards = new();

        for (int i = 0; i < pack.cardCount; i++)
        {
            int cardId = GetRandomCard(pack.possibleCardIds);
            openedCards.Add(cardId);
            ResolveCard(cardId);
        }

        Debug.Log($"Pack opened: {string.Join(",", openedCards)}, wishing for {wish}");

        // Optional: trigger pack-opening UI animation here
    }
    private int GetRandomCard(List<int> pool)
    {
        int index = UnityEngine.Random.Range(0, pool.Count);
        return pool[index];
    }

    private void ResolveCard(int cardId)
    {
        if (UserCollectionManager.Instance.IsOwned(cardId))
        {
            DeckBuilding.Instance.GainUserDust(20);
            Debug.Log("Gain 20 dust");
        }
        else
        {
            UserCollectionManager.Instance.UnlockCard(cardId);
            Debug.Log("Unlocked new card : "+cardId);
        }
        UpdateGoldAndDust();
    }
    public void BuyRandomPack()
    {
        const int PACK_COST = 100; // example

        GetUserGold(gold =>
        {
            UserGold = gold;
            GoldCounter.text = gold.ToString();

            if (UserGold < PACK_COST)
            {
                Debug.Log("Not enough gold");
                return;
            }

            UseGold(PACK_COST);

            CardPack generatedPack = GenerateRandomPack();
            OpenPack(generatedPack);
        });
    }
    private CardPack GenerateRandomPack()
    {
        List<int> packableCards = new();

        foreach (var card in CardDatabase.Instance.Cards.Values)
        {
            if (card.packable)
            {
                packableCards.Add(card.id);
            }
        }

        if (packableCards.Count == 0)
        {
            Debug.LogError("No packable cards found!");
            return null;
        }

        return new CardPack
        {
            packId = "RandomPack",
            cost = 100,
            cardCount = 5,
            possibleCardIds = packableCards
        };
    }
    #endregion
    #region Gold Management
    public void UseGold(int amount)
    {
        ModifyUserGold(-Mathf.Abs(amount));
    }
    public void GetUserGold(Action<int> onResult)
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
                  Debug.LogError("Failed to fetch user gold.");
                  onResult?.Invoke(0);
                  return;
              }

              int gold = task.Result.ContainsField("gold")
                  ? task.Result.GetValue<int>("gold")
                  : 500;

              onResult?.Invoke(gold);
          });
    }
    private void ModifyUserGold(int delta)
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
          .UpdateAsync("gold", FieldValue.Increment(delta))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to modify gold.");
                    return;
                }

                GetUserGold(gold =>
                {
                    //UserDust = gold;
                    GoldCounter.text = gold.ToString();
                    Debug.Log("User gold updated to: " + gold);
                });
            });

    }
    #endregion
    #region Wish Management
    public void SelectWish(CardData.Trait selectedTrait)
    {
        wish = selectedTrait;
    }
    #endregion
}
