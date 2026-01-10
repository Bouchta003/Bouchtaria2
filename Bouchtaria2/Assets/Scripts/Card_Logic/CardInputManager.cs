using UnityEngine;
using UnityEngine.Rendering;

public class CardInputManager : MonoBehaviour
{
    private Card hoveredCard;
    private Card draggedCard;

    void Update()
    {
        if (Input.GetMouseButtonDown(1)) // Right click
        {
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
                gm.CancelCurrentTargeting();
        }

        HandleHover();
        HandleClickAndDrag();
    }
    void HandleHover()
    {
        Card newHoveredCard = GetTopmostCardUnderMouse();

        if (newHoveredCard != hoveredCard)
        {
            if (hoveredCard != null)
                hoveredCard.OnHoverExit();

            if (newHoveredCard != null)
                newHoveredCard.OnHoverEnter();

            hoveredCard = newHoveredCard;
        }
    }

    void HandleClickAndDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (hoveredCard == null)
                return;

            draggedCard = hoveredCard;
            draggedCard.OnPointerDown();
        }

        if (Input.GetMouseButton(0))
        {
            if (draggedCard != null)
                draggedCard.OnPointerDrag();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (draggedCard != null)
            {
                draggedCard.OnPointerUp();
                draggedCard = null;
            }
        }
    }
    Card GetTopmostCardUnderMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray);

        Card bestCard = null;
        int bestOrder = int.MinValue;

        foreach (var hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (!hit.collider.TryGetComponent(out Card card))
                continue;

            int order = 0;

            SortingGroup group = card.GetComponent<SortingGroup>();
            if (group != null)
            {
                order = group.sortingOrder;
            }
            else
            {
                SpriteRenderer sr = card.GetActiveSpriteRenderer();
                if (sr == null) continue;
                order = sr.sortingOrder;
            }

            if (order > bestOrder)
            {
                bestOrder = order;
                bestCard = card;
            }
        }

        return bestCard;
    }

}
