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
using DG.Tweening;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;
    private CanvasGroup packCanvasGroup;

    [SerializeField] TextMeshProUGUI GoldCounter;
    [SerializeField] TextMeshProUGUI DustCounter;
    [SerializeField] TextMeshProUGUI ProgressionCounter;

    //Card Show : 
    [SerializeField] private Transform packSpawnRoot;
    [SerializeField] private Vector3 cardSpawnScale = Vector3.one;
    [SerializeField] private float cardSpacing = 1.5f;

    //Animation of pack
    [SerializeField] private float packShowDuration = 0.35f;
    [SerializeField] private float packHideDuration = 0.55f;
    [SerializeField] private float packHideScale = 0.9f;

    int UserGold;
    int UserDust;

    public string wish;
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

        packCanvasGroup = packSpawnRoot.GetComponent<CanvasGroup>();
        if (packCanvasGroup == null)
            packCanvasGroup = packSpawnRoot.gameObject.AddComponent<CanvasGroup>();

        packCanvasGroup.alpha = 0f;
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
    private void ShowPackAnimated()
    {
        EnsurePackCanvasGroup();

        packSpawnRoot.gameObject.SetActive(true);

        packCanvasGroup.DOKill();
        packSpawnRoot.DOKill();

        packCanvasGroup.alpha = 0f;
        packSpawnRoot.localScale = Vector3.one * 0.95f;

        Sequence seq = DOTween.Sequence();
        seq.Join(packCanvasGroup.DOFade(1f, 0.35f));
        seq.Join(packSpawnRoot.DOScale(1f, 0.35f).SetEase(Ease.OutCubic));

        seq.OnComplete(() =>
        {
        });
    }

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

        Transform slot = packSpawnRoot.GetChild(index + 2);

        // Spawn
        CardInstance card = CardFactory.Instance.CreateCardInPosition(
            data,
            PlayerOwner.Player,
            Vector3.zero,
            cardSpawnScale,
            slot
        );
        if (UserCollectionManager.Instance.IsOwned(cardId))
        {
            card.cardView.dustIndicator.SetActive(true);
        }
        Card cardComp = card.GetComponent<Card>();
        if (cardComp != null)
        {
            cardComp.delayedHover = true;
        }

        SortingGroup sorting = card.GetComponent<SortingGroup>();
        if (sorting != null)
            sorting.sortingOrder = 20;

        AnimateCardSpawn(card.transform, index == 0);
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
        if (packCanvasGroup == null || packCanvasGroup.alpha == 0f)
            return;

        HidePackAnimated();
    }
    private void HidePackAnimated()
    {
        EnsurePackCanvasGroup();

        packCanvasGroup.DOKill();
        packSpawnRoot.DOKill();

        packCanvasGroup.blocksRaycasts = false;
        packCanvasGroup.interactable = false;

        Sequence seq = DOTween.Sequence();

        seq.Join(packCanvasGroup.DOFade(0f, packHideDuration));
        seq.Join(
            packSpawnRoot
                .DOScale(packHideScale, packHideDuration)
                .SetEase(Ease.InOutCubic)
        );

        seq.OnComplete(() =>
        {
            // Cleanup spawned cards
            for (int i = 2; i < packSpawnRoot.childCount; i++)
            {
                foreach (Transform child in packSpawnRoot.GetChild(i))
                    Destroy(child.gameObject);
            }

            packSpawnRoot.gameObject.SetActive(false);
        });
    }

    private void EnsurePackCanvasGroup()
    {
        if (packCanvasGroup != null)
            return;

        if (packSpawnRoot == null)
        {
            Debug.LogError("packSpawnRoot is null");
            return;
        }

        packCanvasGroup = packSpawnRoot.GetComponent<CanvasGroup>();
        if (packCanvasGroup == null)
            packCanvasGroup = packSpawnRoot.gameObject.AddComponent<CanvasGroup>();
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
            OpenPack(generatedPack); ShowPackAnimated();

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

    private void AnimateCardSpawn(Transform card, bool isFinalCard)
    {
        float duration = isFinalCard ? 0.6f : 0.4f;
        Ease ease = isFinalCard ? Ease.OutBack : Ease.OutCubic;

        Vector3 startPos = card.localPosition + Vector3.down * 0.5f;
        Vector3 endPos = card.localPosition;

        card.localPosition = startPos;
        card.localScale = Vector3.zero;

        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = card.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0f;

        Sequence seq = DOTween.Sequence();

        seq.Join(card.DOLocalMove(endPos, duration).SetEase(ease));
        seq.Join(card.DOScale(cardSpawnScale, duration).SetEase(ease));
        seq.Join(cg.DOFade(1f, duration * 0.8f));

        if (isFinalCard)
        {
            seq.Append(card.DOPunchScale(Vector3.one * 0.15f, 0.25f, 8, 0.8f));
        }
        seq.OnComplete(() =>
{
    Card cardComp = card.GetComponent<Card>();
    if (cardComp != null)
        cardComp.EnableHover();
});

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
        if (wish == selectedTrait)
        {
            wish = ""; return;
        }

        Debug.Log("Click wish " + selectedTrait);
        wish = selectedTrait;
    }
    #endregion
}
