using UnityEngine;
using TMPro;
using System.Collections;

public class ErrorMessage : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeIn = 0.25f;
    [SerializeField] private float hold = 1.5f;
    [SerializeField] private float fadeOut = 0.25f;

    public void Play(string message)
    {
        messageText.text = message;
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // fade in
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeIn);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(hold);

        // fade out
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeOut);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        Destroy(gameObject);
    }
}
