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

        gameManager = FindFirstObjectByType<GameManager>();
        thisInstance = gameObject.GetComponent<CardInstance>();
    }
    void Start()
    {
        col = GetComponent<Collider2D>();
        enemyCardDropArea = FindFirstObjectByType<EnemyCardDropArea>();
        allyCardDropArea = FindFirstObjectByType<AllyCardDropArea>();

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
        if (!hoverEnabled) return;
        if (isDragging) return;
        if (gameManager != null &&
            gameManager.isDiscovering && !thisInstance.IsDisplay)
            return;

        isHovered = true; // ✅ THIS WAS MISSING
    }

    public void EnableHover()
    {
        originalScale = transform.localScale; // ✅ CORRECT scale
        isHovered = false;
        hoverEnabled = true;
    }


    public void OnHoverExit()
    {
        isHovered = false;
    }

    public void OnPointerDown()
    {
        if(SceneManager.GetActiveScene().name == "Combat")
        {
            //Discovery effect
            if (gameManager.isDiscovering && thisInstance.IsDisplay)
            {
                gameManager.isDiscovering = false; // lock FIRST

                if (gameManager.OwnerHasTrait(thisInstance.Owner, CardData.Trait.Faith, 2))
                    gameManager.AddCardToHand(thisInstance.Owner, thisInstance.Data.id, -1);
                else
                    gameManager.AddCardToHand(thisInstance.Owner, thisInstance.Data.id);

                //RefreshMana
                if (gameManager.OwnerHasTrait(thisInstance.Owner, CardData.Trait.Faith, 3))
                { gameManager.GainMana(1, thisInstance.Owner); }

                // Destroy all discover cards
                foreach (Transform child in gameManager.discoverDisplay.transform)
                Destroy(child.gameObject);

                gameManager.discoverDisplay.SetActive(false);
                return;
            }
            //Drag to play card
            if (thisInstance.CurrentZone == CardZone.Hand && GetComponent<CardInstance>().Owner==PlayerOwner.Player)
            {
                isDragging = true;isHovered = false;
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

            isDragging = true;isHovered = false;
            startDragPosition = transform.position;
            transform.position = GetMousePositionInWorldSpace();
        }
    }

    public void OnPointerDrag()
    {
        if (!isDragging)
            return;

        if (thisInstance.CurrentZone != CardZone.Hand && SceneManager.GetActiveScene().name == "Combat")
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
