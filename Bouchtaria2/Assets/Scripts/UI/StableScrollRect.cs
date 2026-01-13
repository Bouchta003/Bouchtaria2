using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class StableScrollRect : ScrollRect
{
    [Header("Stability")]
    [SerializeField] private float snapSpeed = 15f;

    private bool isDragging;

    public override void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        base.OnBeginDrag(eventData);
        isDragging = true;
    }

    public override void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
        isDragging = false;
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (content == null || viewport == null)
            return;

        ClampToBounds();
    }

    private void ClampToBounds()
    {
        Bounds contentBounds = GetContentBounds();
        Bounds viewBounds = GetViewBounds();

        Vector2 offset = Vector2.zero;

        if (contentBounds.min.y > viewBounds.min.y)
            offset.y = viewBounds.min.y - contentBounds.min.y;
        else if (contentBounds.max.y < viewBounds.max.y)
            offset.y = viewBounds.max.y - contentBounds.max.y;

        if (offset != Vector2.zero)
        {
            // Stop inertia from fighting the clamp
            velocity = Vector2.zero;

            if (!isDragging)
            {
                content.anchoredPosition += Vector2.Lerp(
                    Vector2.zero,
                    offset,
                    Time.unscaledDeltaTime * snapSpeed
                );
            }
        }
    }

    private Bounds GetViewBounds()
    {
        return new Bounds(
            viewport.rect.center,
            viewport.rect.size
        );
    }

    private Bounds GetContentBounds()
    {
        Vector3[] corners = new Vector3[4];
        content.GetWorldCorners(corners);

        var bounds = new Bounds(corners[0], Vector3.zero);
        for (int i = 1; i < 4; i++)
            bounds.Encapsulate(corners[i]);

        // Convert world bounds into viewport space
        Vector3 center = viewport.InverseTransformPoint(bounds.center);
        bounds.center = center;

        return bounds;
    }
}
