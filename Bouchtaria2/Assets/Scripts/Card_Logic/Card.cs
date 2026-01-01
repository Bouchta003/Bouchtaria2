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


    [Header("Visuals")]
    [SerializeField] private SpriteRenderer handVisual;
    [SerializeField] private SpriteRenderer boardVisual;
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
    }
    #region Pointer-based input (called by CardInputManager)

    public void OnPointerDown()
    {
        if(SceneManager.GetActiveScene().name == "Combat")
        {
            if (gameManager.isDiscovering)
            {
                if (thisInstance.IsDisplay)
                {
                    Debug.Log($"Discovered {thisInstance.Data.name}");
                    gameManager.AddCardToHand(thisInstance.Owner, thisInstance.Data.id);
                    gameManager.isDiscovering = false;
                    gameManager.discoverDisplay.SetActive(false);
                }
                return;
            }
            if (thisInstance.CurrentZone == CardZone.Hand)
            {
                isDragging = true;
                startDragPosition = transform.position;
                transform.position = GetMousePositionInWorldSpace();

                if (handManager != null)
                    handManager.RaiseCard(gameObject, 500);
                return;
            }

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
                    DeckBuilding.Instance.UnlockCard(GetComponent<CardView>().CardData.id);
                }
                return;
            }

            isDragging = true;
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
