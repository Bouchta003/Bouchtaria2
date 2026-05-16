using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    Collider2D col;
    Vector3 startDragPosition;
    EnemyCardDropArea enemyCardDropArea;
    AllyCardDropArea allyCardDropArea;
    HandManager handManager;
    GameManager gameManager;
    CardInstance thisInstance;
    public bool isDragging; // 🔑 NEW
    [SerializeField] Rigidbody2D rb;
    //Hover
    [Header("Hover Effect")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float hoverSpeed = 8f;

    private Vector3 originalScale;
    private bool isHovered;
    private bool hoverEnabled = false;


    [Header("Visuals")]
    [SerializeField] private SpriteRenderer handVisual;
    [SerializeField] private SpriteRenderer boardVisual;
    [Header("Hover Control")]
    [SerializeField] public bool delayedHover = false;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.enabled = true; // ALWAYS enable
        if (SceneManager.GetActiveScene().name != "PathofPower")
        {
            gameManager = FindFirstObjectByType<GameManager>();
            thisInstance = gameObject.GetComponent<CardInstance>();
        }
    }
    void Start()
    {
        col = GetComponent<Collider2D>();
        if (SceneManager.GetActiveScene().name != "PathofPower")
        {
            enemyCardDropArea = FindFirstObjectByType<EnemyCardDropArea>();
            allyCardDropArea = FindFirstObjectByType<AllyCardDropArea>();
        }

        if (!delayedHover)
            EnableHover();
    }

    void Update()
    {
        if (!hoverEnabled)
            return;

        Vector3 targetScale = isHovered ? originalScale * hoverScale : originalScale;
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * hoverSpeed
        );
    }


    #region Pointer-based input (called by CardInputManager)
    public void OnHoverEnter()
    {
        if (SceneManager.GetActiveScene().name == "PathofPower") { isHovered = true; return; }
            if (CombatDialogue.Instance != null)
        {
            if (CombatDialogue.Instance.UIDialogue.activeSelf && CombatDialogue.Instance.UIDialogue != null) return;
        }
        if (!hoverEnabled) return;
        if (isDragging) return;
        if (gameManager != null &&
            gameManager.isDiscovering && !thisInstance.IsDisplay)
            return;

        isHovered = true; 
    }

    public void EnableHover()
    {
        originalScale = transform.localScale; 
        isHovered = false;
        hoverEnabled = true;
    }


    public void OnHoverExit()
    {
        isHovered = false;
    }

    public void OnPointerDown()
    {
        if (CombatDialogue.Instance != null)
        {
            if (CombatDialogue.Instance.UIDialogue.activeSelf) return;
        }
        if (SceneManager.GetActiveScene().name == "Combat")
        {
            //Discovery effect
            if (gameManager.isDiscovering && thisInstance.IsDisplay)
            {
                gameManager.isDiscovering = false; // lock FIRST

                if (thisInstance.Data.cardType == "curse")
                {
                    // Destroy all discover cards first
                    foreach (Transform child in gameManager.discoverDisplay.transform)
                        Destroy(child.gameObject);

                    gameManager.discoverDisplay.SetActive(false);
                    gameManager.discoverLabel.text = "Choose a card !";

                    // Execute the curse effect on the owner who picked it
                    // (IsDisplay cards are always created as Player, so Owner is always Player here)
                    thisInstance.OnPlaySpell();

                    return;
                }
                if (gameManager.OwnerHasTrait(thisInstance.Owner, CardData.Trait.Faith, 2))
                    gameManager.AddCardToHand(thisInstance.Owner, thisInstance.Data.id, -(1 + gameManager.DiscoverDiscount));
                else
                    gameManager.AddCardToHand(thisInstance.Owner, thisInstance.Data.id, -gameManager.DiscoverDiscount);

                // Add extra copies if this was a discovertrait(trait,n) discover
                if (gameManager.DiscoverTraitCopiesCount > 0)
                {
                    for (int i = 0; i < gameManager.DiscoverTraitCopiesCount; i++)
                        gameManager.AddCardToHand(thisInstance.Owner, thisInstance.Data.id);
                    gameManager.DiscoverTraitCopiesCount = 0;
                }

                //RefreshMana
                if (gameManager.OwnerHasTrait(thisInstance.Owner, CardData.Trait.Faith, 3))
                { gameManager.GainMana(1, thisInstance.Owner); }

                // Destroy all discover cards
                foreach (Transform child in gameManager.discoverDisplay.transform)
                    Destroy(child.gameObject);

                gameManager.discoverDisplay.SetActive(false);
                gameManager.DiscoverDiscount = 0;
                return;
            }
            //Drag to play card
            if (thisInstance.CurrentZone == CardZone.Hand && GetComponent<CardInstance>().Owner == PlayerOwner.Player
            && !gameManager.isDiscovering && gameManager.CurrentGameState == GameState.Playing)
            {
                isDragging = true; isHovered = false;
                startDragPosition = transform.position;
                transform.position = GetMousePositionInWorldSpace();

                if (handManager != null)
                    handManager.RaiseCard(gameObject, 500);
                return;
            }
            //Click for card attack or effect target
            if (thisInstance.CurrentZone == CardZone.Board)
            {
                gameManager.HandleBoardCardClick(this);
                gameManager.HandleTargetClick(GetComponent<CardInstance>());
            }
        }
        else if (SceneManager.GetActiveScene().name == "PathofPower")
        {
            //Discovery Click validated : Add card to current run deck. 
            //Verify deck existance, then add card to deck and update on db too.
            //Replace this placeholder debug by the actual implementation.
            Debug.Log($"Added {this.GetComponent<CardView>().CardData.name} to the current run deck.");

            // End discovery : destroy and hide :
            foreach (Transform child in PathOfPowerManager.Instance.DiscoverDisplay.transform)
                Destroy(child.gameObject);

            PathOfPowerManager.Instance.DiscoverDisplay.SetActive(false);
            return;
        } 

        else
        {
            if (DeckBuilding.Instance.isCrafting && !UserCollectionManager.Instance.IsOwned(GetComponent<CardView>().CardData.id))
            {
                Debug.Log($"Should unlock card {GetComponent<CardView>().CardData.id}");
                if (DeckBuilding.Instance.UserDust >= 100)
                {
                    DeckBuilding.Instance.UseUserDust(100);
                    UserCollectionManager.Instance.UnlockCard(GetComponent<CardView>().CardData.id);
                    //DeckBuilding.Instance.collection.ShowPage(DeckBuilding.Instance.collection.currentPage);replace by lock drop animation
                    GetComponentInChildren<LockOverlayAnimation>()?.PlayUnlockAnimation();
                }
                return;
            }

            isDragging = true; isHovered = false;
            startDragPosition = transform.position;
            transform.position = GetMousePositionInWorldSpace();
        }
    }

    public void OnPointerDrag()
    {
        if (!isDragging)
            return;

        if (thisInstance.CurrentZone != CardZone.Hand &&
        SceneManager.GetActiveScene().name == "Combat" || SceneManager.GetActiveScene().name == "PathofPower")
            return;

        transform.position = GetMousePositionInWorldSpace();
    }

    public void OnPointerUp()
    {
        if (!isDragging)
            return;

        isDragging = false;

        //Collection drops
        if (SceneManager.GetActiveScene().name == "Collection")
        {
            ChestAnimation chest = FindFirstObjectByType<ChestAnimation>();

            if (chest != null && chest.IsTriggered() && chest.GetHoveringCard() == this)
            {
                if (DeckBuilding.Instance.collection.isDeck)
                    DeckBuilding.Instance.RemoveCardFromChest(this);
                else
                    DeckBuilding.Instance.DropCardToChest(this);
                ResetCard();
                return;
            }

            ResetCard();
            return;
        }

        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Collider2D hitCollider = Physics2D.OverlapPoint(
            mouseWorldPos,
            LayerMask.GetMask("DropArea")
        );

        if (hitCollider == null)
        {
            ResetCard();
            return;
        }

        if (!hitCollider.TryGetComponent(out ICardDropArea cardDropArea))
        {
            ResetCard();
            return;
        }

        cardDropArea.OnCardDrop(this);
        LockOnBoard();
    }

    public void OnRightClick()
    {
        if (SceneManager.GetActiveScene().name != "Collection")
            return;

        if (DeckBuilding.Instance == null || DeckBuilding.Instance.collection == null)
            return;

        if (DeckBuilding.Instance.collection.isDeck) { DeckBuilding.Instance.RemoveCardFromChest(this); return; }

        DeckBuilding.Instance.DropCardToChest(this);
    }

    public SpriteRenderer GetActiveSpriteRenderer()
    {
        if (thisInstance.CurrentZone == CardZone.Hand && handVisual.gameObject.activeInHierarchy)
            return handVisual;

        if (thisInstance.CurrentZone == CardZone.Board && boardVisual.gameObject.activeInHierarchy)
            return boardVisual;

        // Fallback (should not happen, but safe)
        return boardVisual != null ? boardVisual : handVisual;
    }

    private Vector3 GetMousePositionInWorldSpace()
    {
        Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0f;
        return p;
    }
    public void ResetCard()
    {
        //Add combat restriction for drag
        transform.position = startDragPosition;
        if (handManager != null) handManager.RestoreCardOrder();
    }
    public void LockOnBoard()
    {
        isDragging = false;
        col.enabled = true;
        this.enabled = true; // keep clicks
    }

    #endregion
}
