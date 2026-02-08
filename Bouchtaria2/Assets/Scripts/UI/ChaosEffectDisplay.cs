using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // remove if using legacy Text

public class ChaosEffectDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image runeImage;
    [SerializeField] private TMP_Text effectText; // or Text
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timings (seconds)")]
    [SerializeField] private float fadeInTime = 0.4f;
    [SerializeField] private float holdTime = 1.6f;
    [SerializeField] private float fadeOutTime = 0.4f;

    Coroutine currentRoutine;
    void Awake()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Shows the chaos effect UI, then hides it.
    /// Total duration ≤ 3 seconds.
    /// </summary>
    public void ShowChaosEffect(Sprite rune, string description, Action onComplete = null)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        // Clamp total duration to 3 seconds
        float total = fadeInTime + holdTime + fadeOutTime;
        if (total > 3f && total > 0f)
        {
            float scale = 3f / total;
            fadeInTime *= scale;
            holdTime *= scale;
            fadeOutTime *= scale;
        }

        runeImage.sprite = rune;
        effectText.text = description;

        gameObject.SetActive(true);
        currentRoutine = StartCoroutine(Animate(onComplete));
    }

    IEnumerator Animate(Action onComplete)
    {
        gameObject.SetActive(true);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // Fade in
        yield return Fade(0f, 1f, fadeInTime);

        // Hold
        yield return new WaitForSeconds(holdTime);

        // Fade out
        yield return Fade(1f, 0f, fadeOutTime);

        gameObject.SetActive(false);
        currentRoutine = null;

        onComplete?.Invoke();
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        canvasGroup.alpha = from;

        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
