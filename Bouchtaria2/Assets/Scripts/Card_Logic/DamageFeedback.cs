using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DamageFeedback : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Image img;
    [SerializeField] private float flashDuration = 0.06f;
    [SerializeField, Range(0f, 1f)] private float flashStrength = 0.35f;

    private Color originalColor;
    private Coroutine flashRoutine;

    private void Awake()
    {
        if (sprite != null)
            originalColor = sprite.color;
        else if (img != null)
            originalColor = img.color;
    }

    public void Play()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(Flash());
    }

    private IEnumerator Flash()
    {
        Color flashColor = Color.Lerp(originalColor, Color.white, flashStrength);

        if (sprite != null)
        {
            sprite.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            sprite.color = originalColor;
        }
        else if (img != null)
        {
            img.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            img.color = originalColor;
        }
    }
}
