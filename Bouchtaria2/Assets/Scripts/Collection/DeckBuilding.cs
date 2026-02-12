using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using System;
using TMPro;
using System.Linq;
using System.Collections;
using Firebase.Extensions;

public class DeckBuilding : MonoBehaviour
{
    [Header("Chest Animator")]
    [SerializeField] ChestAnimation chestAnimation;
    [SerializeField] Collider2D chestCOllider;

    [Header("UI")]
    [SerializeField] public GameObject CollectionLayout;
    [SerializeField] public GameObject DeckUI;
    [SerializeField] public GameObject IndexUI;
    [SerializeField] public GameObject ChangeDecksButton;
    [SerializeField] public GameObject DeleteDeckButton;
    [SerializeField] public TMP_InputField DeckNameInput;
    [SerializeField] public TextMeshProUGUI DustCounter;
    [SerializeField] public SpriteRenderer ChestSpriteTop;
    [SerializeField] public SpriteRenderer ChestSpriteBot;

    [Header("Cursor")]
    [SerializeField] Image craftCursor;
    [SerializeField] Image craftFilter;

    public static DeckBuilding Instance;

    public List<int> CurrentDeck;
    public CollectionScreen collection;
    public int UserDust;
    public bool isCrafting = false;
    //Local Deck Storage
    private Dictionary<string, List<int>> userDecks = new();
    private List<string> deckNames = new();
    private int currentDeckIndex = 0;
    public Dictionary<CardData.Trait, int> AllyTraitsUnlockable;
    [SerializeField] private TraitsDetection traitsDetection;
    //DeckCount Display
    private Coroutine counterRoutine;
    private Coroutine warningRoutine;
    [SerializeField] private TMP_Text counterPopup;
    [SerializeField] private GameObject warningPopup;
    [SerializeField] private float popupDuration = 0.6f;
    [SerializeField] private float popupScale = 1.2f;

    public event System.Action OnDecksLoaded;

    int maxDeckSize = 30;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        craftFilter.gameObject.SetActive(false);
        DeckUI.SetActive(false);
        collection = CollectionLayout.GetComponentInChildren<CollectionScreen>();

        GetUserDust(dust =>
        {
            Debug.Log("User dust: " + dust);
            UserDust = dust;
            DustCounter.text = dust.ToString();
        });

    }
    private void Start()
    {
        //In case of dungeon runs
        if (GameRunContext.IsDungeonRun)
        {
            maxDeckSize = 15;
            //Add augment logic to increase deck size based on augment.

            DeckNameInput.text = "DungeonDeck";
            DeckNameInput.DeactivateInputField();// To test

            ChangeDecksButton.SetActive(false);
            DeleteDeckButton.SetActive(false);
            
            ChestSpriteBot.color = new Color(1, 0.8f, 0.3f);
            ChestSpriteTop.color = new Color(1, 0.8f, 0.3f);
        }
    }
    private void Update()
    {
        craftFilter.gameObject.SetActive(isCrafting);    
    }
    public void ShowIndex()
    {
        IndexUI.SetActive(!IndexUI.activeSelf);
    }
    public void CloseIndex()
    {
        IndexUI.SetActive(false);
    }
    public Dictionary<string, List<int>> GetUserDecks()
    {
        return new Dictionary<string, List<int>>(userDecks);
    }

    #region CardDropInChest
    public void DropCardToChest(Card card)
    {
        int cardId = card.GetComponent<CardView>().CardData.id;

        if (!CanAddCard(cardId))
        {
            Debug.LogWarning($"Cannot add card {cardId} to deck.");
            return;
        }

        CurrentDeck.Add(cardId);
        ShowProgress(CurrentDeck.Count, maxDeckSize);
        DetectUnlockableTraits();
    }
    public void RemoveCardFromChest(Card card)
    {
        if (CurrentDeck.Contains(card.GetComponent<CardView>().CardData.id))
        {
            CurrentDeck.Remove(card.GetComponent<CardView>().CardData.id);
            collection.ShowPage(collection.currentPage);
            ShowProgress(CurrentDeck.Count, maxDeckSize);
        }
        else ErrorPopup.Show("Couldn't remove card of id " + card.GetComponent<CardView>().CardData.id);
        DetectUnlockableTraits();
    }
    private bool IsCardOwned(int cardId)
    {
        return UserCollectionManager.Instance.IsOwned(cardId);
    }
    private bool CanAddCard(int cardId)
    {
        // Ownership
        if (!IsCardOwned(cardId))
        {
            ShowWarning("You do not own this card, you may not add it to your deck.");
            return false;
        }

        // Copy limit
        int copies = CurrentDeck.Count(id => id == cardId);
        if (copies >= 2)
        {
            ShowWarning("A deck can only have 2 copies of each card.");
            return false;
        }
        if (CurrentDeck.Count >= maxDeckSize)
        {
            ShowWarning($"Your deck already has {maxDeckSize} cards.");
            return false;
        }

        return true;
    }
    #endregion
    #region Deck Creation
    public string DisplayDeckCardIDs(List<int> deck)
    {
        string result = "Current deck contains : ";
        foreach (int id in deck)
        {
            result += id.ToString() + ", ";
        }
        result += $"for a total of {deck.Count} cards.";

        return result;
    }
    public void DisplayDeck()
    {
        collection.isDeck = !collection.isDeck;
        DeckUI.SetActive(collection.isDeck);

        if (collection.isDeck)
            FetchDecks();

        collection.ShowPage(collection.currentPage);
        DetectUnlockableTraits();
    }
    public void RegisterDeck()
    {
        if (GameRunContext.IsDungeonRun)
        {
            RegisterDungeonDeck();
            return;
        }
        // 🔒 Validation
        if (string.IsNullOrWhiteSpace(DeckNameInput.text))
        {
            ShowWarning("Deck name is empty.");
            return;
        }

        if (CurrentDeck == null || CurrentDeck.Count != maxDeckSize)
        {
            ShowWarning($"Deck must contain exactly {maxDeckSize} cards (currently {CurrentDeck.Count}).");
            return;
        }
        // Final validation pass
        foreach (int id in CurrentDeck)
        {
            if (!IsCardOwned(id))
            {
                ShowWarning("Deck contains unowned cards. Aborting save.");
                return;
            }

            if (CurrentDeck.Count(x => x == id) > 2)
            {
                ShowWarning("Deck violates copy limit. Aborting save.");
                return;
            }
        }

        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            ShowWarning("No authenticated user.");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        string deckName = DeckNameInput.text.Trim();

        CollectionReference decksRef =
            db.Collection("users")
              .Document(user.UserId)
              .Collection("decks");

        // 🔍 Look for existing deck with same name
        decksRef.WhereEqualTo("name", deckName)
            .GetSnapshotAsync()
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to query decks: " + task.Exception);
                    return;
                }

                QuerySnapshot snapshot = task.Result;

                DocumentReference deckDoc;

                if (snapshot.Documents.Any())
                {
                    // ♻ Replace existing deck
                    deckDoc = snapshot.Documents.First().Reference;
                    Debug.Log($"Replacing existing deck '{deckName}'.");
                }
                else
                {
                    // ➕ Create new deck
                    deckDoc = decksRef.Document();
                    Debug.Log($"Creating new deck '{deckName}'.");
                }

                Dictionary<string, object> deckData = new Dictionary<string, object>
                {
                { "name", deckName },
                { "cardIds", new List<int>(CurrentDeck) },
                { "updatedAt", Timestamp.GetCurrentTimestamp() }
                };

                deckDoc.SetAsync(deckData).ContinueWith(saveTask =>
                {
                    if (saveTask.IsFaulted)
                    {
                        ShowWarning("Failed to save deck: " + saveTask.Exception);
                    }
                    else
                    {
                        ErrorPopup.Show("Deck successfully saved.");
                    }
                });
            });
    }
    private void RegisterDungeonDeck()
    {
        if (string.IsNullOrWhiteSpace(DeckNameInput.text))
        {
            ShowWarning("Deck name is empty.");
            return;
        }

        if (CurrentDeck == null || CurrentDeck.Count != maxDeckSize)
        {
            ShowWarning($"Deck must contain exactly {maxDeckSize} cards (currently {CurrentDeck.Count}).");
            return;
        }
        // Final validation pass
        foreach (int id in CurrentDeck)
        {
            if (!IsCardOwned(id))
            {
                ShowWarning("Deck contains unowned cards. Aborting save.");
                return;
            }

            if (CurrentDeck.Count(x => x == id) > 2)
            {
                ShowWarning("Deck violates copy limit. Aborting save.");
                return;
            }
        }

        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        db.Collection("users")
          .Document(user.UserId)
          .UpdateAsync("dungeondeck", CurrentDeck)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    ErrorPopup.Show("Failed to save dungeon deck.");
                    return;
                }

                DungeonManager.Instance.CurrentRun.dungeonDeck = CurrentDeck;
                DungeonManager.Instance.SaveRunData();
                ErrorPopup.Show("Deck successfully saved.");
            });

    }
    public void DeleteDeck()
    {
        string deckName = DeckNameInput.text;
        if (string.IsNullOrWhiteSpace(deckName))
        {
            ShowWarning("Invalid deck name.");
            return;
        }

        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            ShowWarning("No authenticated user.");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        CollectionReference decksRef =
            db.Collection("users")
              .Document(user.UserId)
              .Collection("decks");

        // Find deck by name
        decksRef.WhereEqualTo("name", deckName)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to query deck: " + task.Exception);
                    ShowWarning("Failed to delete deck.");
                    return;
                }

                QuerySnapshot snapshot = task.Result;

                if (!snapshot.Documents.Any())
                {
                    ShowWarning($"Deck '{deckName}' not found.");
                    return;
                }

            // Delete all matching docs (should normally be one)
            foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    doc.Reference.DeleteAsync().ContinueWithOnMainThread(deleteTask =>
                    {
                        if (deleteTask.IsFaulted)
                        {
                            Debug.LogError("Failed to delete deck: " + deleteTask.Exception);
                            ShowWarning("Failed to delete deck.");
                            return;
                        }

                    // Update local cache
                    userDecks.Remove(deckName);
                        deckNames.Remove(deckName);

                    // Adjust current deck index safely
                    if (deckNames.Count == 0)
                        {
                            CurrentDeck.Clear();
                            DeckNameInput.text = string.Empty;
                            currentDeckIndex = 0;
                        }
                        else
                        {
                            currentDeckIndex = Mathf.Clamp(currentDeckIndex, 0, deckNames.Count - 1);
                            LoadDeck(deckNames[currentDeckIndex]);
                        }

                        Debug.Log($"Deck '{deckName}' deleted successfully.");
                    });
                }
            });
    }
    public void FetchDecks()
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
  .Collection("decks")
  .GetSnapshotAsync()
  .ContinueWithOnMainThread(task =>
  {
      if (task.IsFaulted)
      {
          ErrorPopup.Show("Failed to fetch decks: " + task.Exception);
          return;
      }

      userDecks.Clear();
      deckNames.Clear();

      foreach (DocumentSnapshot doc in task.Result.Documents)
      {
          string name = doc.GetValue<string>("name");
          List<int> cardIds = doc.GetValue<List<int>>("cardIds");

          userDecks[name] = new List<int>(cardIds);
          deckNames.Add(name);
      }

      Debug.Log($"Fetched {deckNames.Count} decks");

      // ✅ NOW the data exists
      OnDecksLoaded?.Invoke();
  });

    }
    private void LoadDeck(string deckName)
    {
        if (!userDecks.ContainsKey(deckName))
            return;

        CurrentDeck = new List<int>(userDecks[deckName]);
        DeckNameInput.text = deckName;

        collection.ShowPage(collection.currentPage);
        DetectUnlockableTraits();
    }
    public void SwitchDeck()
    {
        if (deckNames.Count == 0)
            return;

        currentDeckIndex = (currentDeckIndex + 1) % deckNames.Count;
        LoadDeck(deckNames[currentDeckIndex]);
    }
    public void DetectUnlockableTraits()
    {
        //Destroy all traits
        AllyTraitsUnlockable =
            traitsDetection.RetrieveTraitTiersFromDeck(
                GetDeckQueue(),
                PlayerOwner.Player
            );
    }
    private Queue<CardData> GetDeckQueue()
    {
        Queue<CardData> deck = new Queue<CardData>();

        foreach (int cardID in CurrentDeck)
        {
            deck.Enqueue(CardDatabase.Instance.GetCardById(cardID));
        }

        return deck;
    }
    #endregion
    #region Routines
    public void ShowWarning(string message)
    {
        if (warningRoutine != null)
            StopCoroutine(warningRoutine);

        warningRoutine = StartCoroutine(WarningRoutine(message));
    }
    private IEnumerator WarningRoutine(string message)
    {
        TextMeshProUGUI warningText = warningPopup.GetComponentInChildren<TextMeshProUGUI>();
        warningPopup.gameObject.SetActive(true);
        warningText.text = $"{message}";

        RectTransform rt = warningText.rectTransform;
        CanvasGroup cg = warningPopup.GetComponent<CanvasGroup>();

        if (cg == null)
            cg = warningPopup.gameObject.AddComponent<CanvasGroup>();

        rt.localScale = Vector3.one * 0.9f;
        cg.alpha = 0f;

        // Fade + pop in
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float k = t / 0.15f;

            rt.localScale = Vector3.Lerp(Vector3.one * 0.9f, Vector3.one * popupScale, k);
            cg.alpha = k;
            yield return null;
        }

        rt.localScale = Vector3.one;

        // Hold
        yield return new WaitForSeconds(3f);

        // Fade out
        t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            cg.alpha = 1f - (t / 0.2f);
            yield return null;
        }

        warningPopup.gameObject.SetActive(false);
    }
    public void ShowProgress(int current, int cap)
    {
        if (counterRoutine != null)
            StopCoroutine(counterRoutine);

        counterRoutine = StartCoroutine(counterPopupRoutine(current, cap));
    }
    private IEnumerator counterPopupRoutine(int current, int cap)
    {
        counterPopup.gameObject.SetActive(true);
        counterPopup.text = $"{current} / {cap}";

        RectTransform rt = counterPopup.rectTransform;
        CanvasGroup cg = counterPopup.GetComponent<CanvasGroup>();

        if (cg == null)
            cg = counterPopup.gameObject.AddComponent<CanvasGroup>();

        rt.localScale = Vector3.one * 0.9f;
        cg.alpha = 0f;

        // Fade + pop in
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float k = t / 0.15f;

            rt.localScale = Vector3.Lerp(Vector3.one * 0.9f, Vector3.one * popupScale, k);
            cg.alpha = k;
            yield return null;
        }

        rt.localScale = Vector3.one;

        // Hold
        yield return new WaitForSeconds(popupDuration);

        // Fade out
        t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            cg.alpha = 1f - (t / 0.2f);
            yield return null;
        }

        counterPopup.gameObject.SetActive(false);
    }
    #endregion
    #region Crafting
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
    public void ToggleCraftMode()
    {
        isCrafting = !isCrafting;
        if (isCrafting)
        {
            Debug.Log("start crafting");
        }
    }
    public void UseUserDust(int amount)
    {
        ModifyUserDust(-Mathf.Abs(amount));
    }
    public void GainUserDust(int amount)
    {
        ModifyUserDust(Mathf.Abs(amount));
    }
    private void ModifyUserDust(int delta)
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
          .UpdateAsync("dust", FieldValue.Increment(delta))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to modify dust.");
                    return;
                }

                GetUserDust(dust =>
                {
                    UserDust = dust;
                    DustCounter.text = dust.ToString();
                    Debug.Log("User dust updated to: " + dust);
                });
            });

    }
    #endregion
}
