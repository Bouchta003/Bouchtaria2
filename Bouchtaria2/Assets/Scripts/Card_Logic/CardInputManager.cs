using UnityEngine;

public class CardInputManager : MonoBehaviour
{
    private Card hoveredCard;
    private Card draggedCard;

    void Update()
    {
        HandleHover();
        HandleClickAndDrag();
    }

    void HandleHover()
    {
        hoveredCard = GetTopmostCardUnderMouse();
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

            if (hit.collider.TryGetComponent<Card>(out Card card))
            {
                SpriteRenderer sr = card.GetActiveSpriteRenderer();
                if (sr == null) continue;

                int order = sr.sortingOrder;

                if (order > bestOrder)
                {
                    bestOrder = order;
                    bestCard = card;
                }
            }
        }

        return bestCard;
    }
}
