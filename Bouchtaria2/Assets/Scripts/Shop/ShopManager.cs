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
    private int[] currentPackCards;
    private HashSet<int> ownedBeforePack;

    public string wish;

    [Header("Glow for Wish")]
    [SerializeField] Image monsterHunterGlow;
    [SerializeField] Image pokemonGlow;
    [SerializeField] Image faithGlow;
    [SerializeField] Image avatarGlow;
    [SerializeField] Image inazumaGlow;
    [SerializeField] Image fighterGlow;
    [SerializeField] Image speedsterGlow;
    [SerializeField] Image healerGlow;
    [SerializeField] Image neutralGlow;
    [SerializeField] Image chaosGlow;
    [SerializeField] Image comboGlow;
    [SerializeField] Image gunnerGlow;
    [SerializeField] Image soulGlow;
    [SerializeField] Image cozyGlow;
    [SerializeField] Image swordsmanGlow;

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
        UpdateProgressionCounter();

        packCanvasGroup = packSpawnRoot.GetComponent<CanvasGroup>();
        if (packCanvasGroup == null)
            packCanvasGroup = packSpawnRoot.gameObject.AddComponent<CanvasGroup>();

        packCanvasGroup.alpha = 0f;
    }
    private void Start()
    {
        UpdateProgressionCounter();
    }
    private void OnEnable()
    {
        if (UserCollectionManager.Instance != null)
            UserCollectionManager.Instance.OnCollectionUpdated += UpdateProgressionCounter;
    }

    private void OnDisable()
    {
        if (UserCollectionManager.Instance != null)
            UserCollectionManager.Instance.OnCollectionUpdated -= UpdateProgressionCounter;
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
            UserGold = gold;
            GoldCounter.text = "Gold: " + gold.ToString();
        });
        GetUserDust(dust =>
        {
            UserDust = dust;
            DustCounter.text = "Dust: " + dust.ToString();
        }); 
        // Update progression counter as well (pulls authoritative counts)
        UpdateProgressionCounter();
    }
    // -------------------- Progression counter helpers --------------------
    // Call: UpdateProgressionCounter() to update the ProgressionCounter UI text.
    // Tries Firestore first; falls back to client-side CardDatabase and UserCollectionManager.

    public void GetUserDust(Action<int> onResult)
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
                  Debug.LogError("Failed to fetch user dust.");
                  onResult?.Invoke(0);
                  return;
              }

              int dust = task.Result.ContainsField("dust")
                  ? task.Result.GetValue<int>("dust")
                  : 0;

              onResult?.Invoke(dust);
          });
    }
    public void UpdateProgressionCounter()
    {
        int ownedPackable = 0;
        int totalPackable = 0;

        var cardDb = CardDatabase.Instance;
        var collection = UserCollectionManager.Instance;

        if (cardDb == null || collection == null)
        {
            ProgressionCounter.text = "Progression 0/0";
            return;
        }

        foreach (var card in cardDb.Cards.Values)
        {
            if (!card.packable)
                continue;

            totalPackable++;

            // IMPORTANT: this MUST match CollectionScreen logic
            if (collection.IsOwned(card.id))
                ownedPackable++;
        }

        ProgressionCounter.text = $"Progression {ownedPackable}/{totalPackable}";
    }


    public void GetOwnedPackableCount(Action<int> onResult)
    {
        // Try to get authoritative owned list from Firestore user doc or subcollection.
        try
        {
            FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null)
            {
                onResult?.Invoke(CountOwnedPackableFromLocal());
                return;
            }

            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

            // First try a user doc field "ownedCards" (array)
            db.Collection("users")
              .Document(user.UserId)
              .GetSnapshotAsync()
              .ContinueWithOnMainThread(userTask =>
              {
                  try
                  {
                      if (userTask.IsFaulted || !userTask.Result.Exists)
                      {
                      // fallback to local
                      onResult?.Invoke(CountOwnedPackableFromLocal());
                          return;
                      }

                      var snap = userTask.Result;
                      if (snap.ContainsField("ownedCards"))
                      {
                      // ownedCards is expected as array of ints or strings
                      var raw = snap.GetValue<List<object>>("ownedCards");
                          int count = 0;
                          if (raw != null)
                          {
                              foreach (var o in raw)
                              {
                                  if (o == null) continue;
                              // Parse id
                              if (int.TryParse(o.ToString(), out int id))
                                  {
                                      var cd = CardDatabase.Instance.GetCardById(id);
                                      if (cd != null && cd.packable) count++;
                                  }
                              }
                          }
                          onResult?.Invoke(count);
                          return;
                      }

                  // If no ownedCards array, try subcollection users/{userId}/cards
                  db.Collection("users")
                        .Document(user.UserId)
                        .Collection("cards")
                        .GetSnapshotAsync()
                        .ContinueWithOnMainThread(collTask =>
                        {
                            try
                            {
                                if (collTask.IsFaulted)
                                {
                                    onResult?.Invoke(CountOwnedPackableFromLocal());
                                    return;
                                }

                                int subCount = 0;
                                foreach (var doc in collTask.Result.Documents)
                                {
                                    int cardId = -1;
                                    if (doc.ContainsField("cardId"))
                                    {
                                    // Firestore integer fields often come back as long - be safe
                                    try
                                        {
                                            var v = doc.GetValue<object>("cardId");
                                            if (v != null && int.TryParse(v.ToString(), out int tmp)) cardId = tmp;
                                        }
                                        catch { }
                                    }
                                    else
                                    {
                                    // Attempt to parse doc id as int
                                    int.TryParse(doc.Id, out cardId);
                                    }

                                    if (cardId <= 0) continue;
                                    var cd = CardDatabase.Instance.GetCardById(cardId);
                                    if (cd != null && cd.packable) subCount++;
                                }
                                onResult?.Invoke(subCount);
                            }
                            catch
                            {
                                onResult?.Invoke(CountOwnedPackableFromLocal());
                            }
                        });

                  }
                  catch
                  {
                  // any unexpected error => fallback local
                  onResult?.Invoke(CountOwnedPackableFromLocal());
                  }
              });
        }
        catch
        {
            onResult?.Invoke(CountOwnedPackableFromLocal());
        }
    }

    public void GetTotalPackableCount(Action<int> onResult)
    {
        // Try to ask Firestore for authoritative count of packable cards, otherwise count locally.
        try
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

            db.Collection("cards")
              .WhereEqualTo("packable", true)
              .GetSnapshotAsync()
              .ContinueWithOnMainThread(task =>
              {
                  if (task.IsFaulted)
                  {
                      onResult?.Invoke(CountTotalPackableFromLocal());
                      return;
                  }
              // If Firestore supports, snapshot.Count is authoritative
              onResult?.Invoke(task.Result.Count);
              });
        }
        catch
        {
            onResult?.Invoke(CountTotalPackableFromLocal());
        }
    }

    // Local fallback: count owned packable by checking UserCollectionManager.IsOwned + CardDatabase.packable
    private int CountOwnedPackableFromLocal()
    {
        int count = 0;
        if (CardDatabase.Instance == null) return 0;

        if (UserCollectionManager.Instance != null)
        {
            // fast path: iterate all cards and check IsOwned + packable
            foreach (var cd in CardDatabase.Instance.Cards.Values)
            {
                try
                {
                    if (cd.packable && UserCollectionManager.Instance.IsOwned(cd.id))
                        count++;
                }
                catch { /* ignore instance errors */ }
            }
            return count;
        }

        return 0;
    }

    // Local fallback: count total packable from CardDatabase
    private int CountTotalPackableFromLocal()
    {
        int count = 0;
        if (CardDatabase.Instance == null) return 0;
        foreach (var cd in CardDatabase.Instance.Cards.Values)
        {
            if (cd.packable) count++;
        }
        return count;
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

        // ✅ RESTORE UI INTERACTION
        packCanvasGroup.blocksRaycasts = true;
        packCanvasGroup.interactable = true;

        Sequence seq = DOTween.Sequence();
        seq.Join(packCanvasGroup.DOFade(1f, 0.35f));
        seq.Join(packSpawnRoot.DOScale(1f, 0.35f).SetEase(Ease.OutCubic));
    }
    private void OpenPack(CardPack pack)
    {
        List<int> availablePool = new(pack.possibleCardIds);
        int count = pack.cardCount;

        int[] finalCards = new int[count];

        // ✅ SNAPSHOT ownership BEFORE resolving
        ownedBeforePack = new HashSet<int>();
        foreach (int cardId in pack.possibleCardIds)
        {
            if (UserCollectionManager.Instance.IsOwned(cardId))
                ownedBeforePack.Add(cardId);
        }

        int guaranteed = GetGuaranteedWishCard(availablePool);
        finalCards[0] = guaranteed;
        availablePool.Remove(guaranteed);

        for (int i = 1; i < count; i++)
        {
            if (availablePool.Count == 0)
                break;

            int cardId = GetRandomCardWeighted(availablePool);
            finalCards[i] = cardId;
            availablePool.Remove(cardId);
        }

        // ✅ STORE pack result
        currentPackCards = finalCards;

        // ✅ RESOLVE IMMEDIATELY (security)
        foreach (int cardId in finalCards)
        {
            ResolveCard(cardId);
        }

        UpdateGoldAndDust();

        // ✅ VISUAL REVEAL (purely visual)
        StartCoroutine(RevealPackCoroutine(finalCards));
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
        if (ownedBeforePack != null && ownedBeforePack.Contains(cardId))
        {
            card.cardView.dustIndicator.SetActive(true);
        }
        if (ownedBeforePack != null && !ownedBeforePack.Contains(cardId))
        {
            card.cardView.newCardIndicator.SetActive(true);
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

        UpdateGoldAndDust();
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
        if (packSpawnRoot.gameObject.activeSelf)
            return;

        // Toggle off if same wish
        if (wish == selectedTrait)
        {
            wish = "";
            DisableAllGlows();
            return;
        }

        wish = selectedTrait;
        DisableAllGlows();

        Debug.Log("Click wish " + selectedTrait);

        // TEST TRAITS
        if (selectedTrait == "MonsterHunter")
        {
            EnableRainbowGlow(monsterHunterGlow);
        }
        else if (selectedTrait == "Pokemon")
        {
            EnableRainbowGlow(pokemonGlow);
        }
        else if (selectedTrait == "Fighter")
        {
            EnableRainbowGlow(fighterGlow);
        }
        else if (selectedTrait == "Chaos")
        {
            EnableRainbowGlow(chaosGlow);
        }
        else if (selectedTrait == "Combo")
        {
            EnableRainbowGlow(comboGlow);
        }
        else if (selectedTrait == "Gunner")
        {
            EnableRainbowGlow(gunnerGlow);
        }
        else if (selectedTrait == "Neutral")
        {
            EnableRainbowGlow(neutralGlow);
        }
        else if (selectedTrait == "Healer")
        {
            EnableRainbowGlow(healerGlow);
        }
        else if (selectedTrait == "Faith")
        {
            EnableRainbowGlow(faithGlow);
        }
        else if (selectedTrait == "Avatar")
        {
            EnableRainbowGlow(avatarGlow);
        }
        else if (selectedTrait == "Inazuma")
        {
            EnableRainbowGlow(inazumaGlow);
        }
        else if (selectedTrait == "Cozy")
        {
            EnableRainbowGlow(cozyGlow);
        }
        else if (selectedTrait == "SoulForce")
        {
            EnableRainbowGlow(soulGlow);
        }
        else if (selectedTrait == "Swordsman")
        {
            EnableRainbowGlow(swordsmanGlow);
        }
        else if (selectedTrait == "Speedster")
        {
            EnableRainbowGlow(speedsterGlow);
        }
    }

    private void DisableAllGlows()
    {
        DisableGlow(monsterHunterGlow);
        DisableGlow(pokemonGlow);
        DisableGlow(gunnerGlow);
        DisableGlow(chaosGlow);
        DisableGlow(comboGlow);
        DisableGlow(healerGlow);
        DisableGlow(faithGlow);
        DisableGlow(avatarGlow);
        DisableGlow(inazumaGlow);
        DisableGlow(fighterGlow);
        DisableGlow(cozyGlow);
        DisableGlow(soulGlow);
        DisableGlow(swordsmanGlow);
        DisableGlow(neutralGlow);
        DisableGlow(speedsterGlow);
    }

    private void EnableRainbowGlow(Image img)
    {
        if (img == null) return;

        img.gameObject.SetActive(true);
        img.DOKill();

        // Start visible
        Color baseColor = Color.red;
        baseColor.a = 0.7f;
        img.color = baseColor;

        // Alpha pulse
        img.DOFade(1f, 0.6f)
           .SetLoops(-1, LoopType.Yoyo)
           .SetEase(Ease.InOutSine);

        // Hue cycling (rainbow)
        DOTween.To(
            () => 0f,
            h =>
            {
                Color c = Color.HSVToRGB(h, 0.6f, 0.9f);

                c.a = img.color.a; // preserve alpha
            img.color = c;
            },
            1f,
            2.5f // speed of rainbow
        ).SetLoops(-1).SetEase(Ease.InOutSine);

    }
    private void DisableGlow(Image img)
    {
        if (img == null) return;

        img.DOKill();
        img.DOFade(0f, 0.2f).OnComplete(() =>
        {
            img.gameObject.SetActive(false);
        });
    }

    #endregion
}
