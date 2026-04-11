using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AlphaRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
{
    public Image maskImage;
    public float alphaThreshold = 0.1f;

    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        if (maskImage == null || maskImage.sprite == null)
            return true;

        RectTransform rectTransform = maskImage.rectTransform;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, eventCamera, out localPoint))
            return false;

        Rect rect = rectTransform.rect;

        // Convert to normalized (0–1)
        float normX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float normY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);

        Sprite sprite = maskImage.sprite;
        Rect spriteRect = sprite.rect;

        // Convert to texture space (IMPORTANT FIX)
        float texX = (spriteRect.x + spriteRect.width * normX) / sprite.texture.width;
        float texY = (spriteRect.y + spriteRect.height * normY) / sprite.texture.height;

        try
        {
            Color pixel = sprite.texture.GetPixelBilinear(texX, texY);
            return pixel.a >= alphaThreshold;
        }
        catch
        {
            return false;
        }
    }
}