using UnityEngine;
using UnityEngine.SceneManagement;

public class Card : MonoBehaviour
{
    Collider2D col;
    Vector3 startDragPosition;
    EnemyCardDropArea enemyCardDropArea;
    AllyCardDropArea allyCardDropArea;
    HandManager handManager;

    bool isDragging; // 🔑 NEW

    void Awake()
    {
        if (SceneManager.GetActiveScene().name != "Combat")
            this.GetComponent<BoxCollider2D>().enabled = true;
        handManager = FindFirstObjectByType<HandManager>();
    }

    void Start()
    {
        col = GetComponent<Collider2D>();
        enemyCardDropArea = FindFirstObjectByType<EnemyCardDropArea>();
        allyCardDropArea = FindFirstObjectByType<AllyCardDropArea>();
    }

    void OnMouseEnter()
    {
        if (isDragging) return;
        if(handManager!=null)handManager.RaiseCard(gameObject, 50); // hover
    }

    private void OnMouseDown()
    {
        isDragging = true;

        startDragPosition = transform.position;
        transform.position = GetMousePositionInWorldSpace();

        if(handManager!=null)handManager.RaiseCard(gameObject, 500); // drag (topmost)
    }

    private void OnMouseDrag()
    {
        transform.position = GetMousePositionInWorldSpace();
    }

    private Vector3 GetMousePositionInWorldSpace()
    {
        Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0f;
        return p;
    }

    private void OnMouseUp()
    {
        isDragging = false;

        col.enabled = false;

        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPos);

        col.enabled = true;

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

        if (hitCollider.CompareTag("Ally"))
        {
            if (allyCardDropArea.allyPrefabCards.Count < allyCardDropArea.maxBoardSize)
            {
                cardDropArea.OnCardDrop(this);
                return;
            }

            Debug.Log("No more space for allies bro");
            ResetCard();
            return;
        }

        if (hitCollider.CompareTag("Enemy"))
        {
            if (enemyCardDropArea.enemyPrefabCards.Count < enemyCardDropArea.maxBoardSize)
            {
                cardDropArea.OnCardDrop(this);
                return;
            }

            Debug.Log("No more space for enemies bro");
            ResetCard();
            return;
        }

        ResetCard();
    }

    void OnMouseExit()
    {
        if (isDragging) return;
        if(handManager!=null)handManager.RestoreCardOrder();
    }

    void ResetCard()
    {
        transform.position = startDragPosition;
        if(handManager!=null)handManager.RestoreCardOrder();
    }
}
