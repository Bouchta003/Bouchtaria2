using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.Collections;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [SerializeField] TextMeshProUGUI GoldCounter;
    [SerializeField] TextMeshProUGUI DustCounter;
    [SerializeField] TextMeshProUGUI ProgressionCounter;

    //Card Show : 
    [SerializeField] private Transform packSpawnRoot;
    [SerializeField] private Vector3 cardSpawnScale = Vector3.one;
    [SerializeField] private float cardSpacing = 1.5f;


    int UserGold;
    int UserDust;

    string wish;
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
        packSpawnRoot.gameObject.SetActive(false);
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
        List<int> availablePool = new(pack.possibleCardIds);
        int count = pack.cardCount;

        int[] finalCards = new int[count];

        // 1️⃣ Pick guaranteed card for index 0
        int guaranteed = GetGuaranteedWishCard(availablePool);
        finalCards[0] = guaranteed;
        availablePool.Remove(guaranteed);

        ResolveCard(guaranteed);

        // 2️⃣ Pick remaining cards normally
        for (int i = 1; i < count; i++)
        {
            if (availablePool.Count == 0)
                break;

            int cardId = GetRandomCardWeighted(availablePool);
            finalCards[i] = cardId;
            availablePool.Remove(cardId);

            ResolveCard(cardId);
        }

        // 3️⃣ Animate reveal (last → first, index 0 last)
        StartCoroutine(RevealPackCoroutine(finalCards));

        UpdateGoldAndDust();
    }
    private IEnumerator RevealPackCoroutine(int[] cards)
    {
        packSpawnRoot.gameObject.SetActive(true);

        float baseDelay = 0.25f;
        float finalDelay = 0.7f;

        // Reveal from last index to 1
        for (int i = cards.Length - 1; i > 0; i--)
        {
            SpawnCardInScene(cards[i], i);
            yield return new WaitForSeconds(baseDelay);
        }

        // Extra dramatic delay for index 0
        yield return new WaitForSeconds(finalDelay);

        SpawnCardInScene(cards[0], 0);
    }

    private void SpawnCardInScene(int cardId, int index)
    {
        CardData data = CardDatabase.Instance.GetCardById(cardId);
        if (data == null)
            return;
        Vector3 position = Vector3.zero;

        CardFactory.Instance.CreateCardInPosition(
            data,
            PlayerOwner.Player,   // or Neutral / Shop / None
            position,
            cardSpawnScale,
            packSpawnRoot.GetChild(index+2)
        ).GetComponent<SortingGroup>().sortingOrder = 20;
    }
    private int GetGuaranteedWishCard(List<int> pool)
    {
        if (string.IsNullOrEmpty(wish))
            return GetRandomCardWeighted(pool);

        List<int> wishedCards = new();

        foreach (int cardId in pool)
        {
            if (CardDatabase.Instance.Cards[cardId].traits.Contains(wish))
            {
                wishedCards.Add(cardId);
            }
        }

        // Fallback if no card matches the wish
        if (wishedCards.Count == 0)
            return GetRandomCardWeighted(pool);

        return wishedCards[UnityEngine.Random.Range(0, wishedCards.Count)];
    }

    public void ValidatePack()
    {
        Debug.Log("Click");
        packSpawnRoot.gameObject.SetActive(false);
        for (int i = 2; i < 7; i++)
        {
            foreach (Transform child in packSpawnRoot.GetChild(i))
            {
                child.gameObject.SetActive(false);
            }
        }
    }
    private int GetRandomCardWeighted(List<int> pool)
    {
        float totalWeight = 0f;

        // Precompute weights
        Dictionary<int, float> weights = new();

        foreach (int cardId in pool)
        {
            var card = CardDatabase.Instance.Cards[cardId];

            float weight = 1f;

            // Apply wish bonus
            if (!string.IsNullOrEmpty(wish) &&
                card.traits.Contains(wish))
            {
                weight *= 2f;
            }

            weights[cardId] = weight;
            totalWeight += weight;
        }

        // Roll
        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var pair in weights)
        {
            cumulative += pair.Value;
            if (roll <= cumulative)
            {
                return pair.Key;
            }
        }

        // Fallback (should never happen)
        return pool[0];
    }
    private void ResolveCard(int cardId)
    {
        string cardName = CardDatabase.Instance.GetCardById(cardId).name;

        if (UserCollectionManager.Instance.IsOwned(cardId))
        {
            DeckBuilding.Instance.GainUserDust(20);
            Debug.Log("Gain 20 dust from owned card: " + cardName);
        }
        else
        {
            UserCollectionManager.Instance.UnlockCard(cardId);
            Debug.Log("Unlocked new card: " + cardName);
        }
    }

    public void BuyRandomPack()
    {
        if (packSpawnRoot.gameObject.activeSelf) return;
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
            packSpawnRoot.gameObject.SetActive(true);
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
    public void SelectWish(string selectedTrait)
    {
        if (packSpawnRoot.gameObject.activeSelf) return;
        Debug.Log("Click wish " + selectedTrait);
        wish = selectedTrait;
    }
    #endregion
}
