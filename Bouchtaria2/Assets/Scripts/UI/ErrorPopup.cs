using UnityEngine;
using TMPro;
using System.Collections;

public class ErrorPopup : MonoBehaviour
{
    public static ErrorPopup Instance;

    [SerializeField] private GameObject popupRoot; // child object
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timings")]
    [SerializeField] private float fadeIn = 0.25f;
    [SerializeField] private float hold = 1.5f;
    [SerializeField] private float fadeOut = 0.25f;

    private Coroutine routine;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        popupRoot.SetActive(false);
        canvasGroup.alpha = 0f;
    }

    public static void Show(string message)
    {
        if (Instance == null)
        {
            Debug.LogError("ErrorPopup not present in scene.");
            return;
        }

        Instance.ShowInternal(message);
    }

    private void ShowInternal(string message)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(Animate(message));
    }

    private IEnumerator Animate(string message)
    {
        messageText.text = message;
        popupRoot.SetActive(true);
        canvasGroup.alpha = 0f;

        // Fade in
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = t / fadeIn;
            yield return null;
        }

        yield return new WaitForSeconds(hold);

        // Fade out
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - (t / fadeOut);
            yield return null;
        }

        popupRoot.SetActive(false);
        routine = null;
    }
}
