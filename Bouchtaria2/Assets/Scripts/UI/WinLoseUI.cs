using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class WinLoseUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI subText;
    [SerializeField] private Image backgroundPanel;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void ShowWin()
    {
        Setup("VICTORY", Color.green, "Enemy Core Destroyed");
    }

    public void ShowLose()
    {
        Setup("DEFEAT", Color.red, "Your Core Was Destroyed");
    }

    private void Setup(string title, Color color, string subtitle)
    {
        gameObject.SetActive(true);

        resultText.text = title;
        resultText.color = color;

        if (subText != null)
            subText.text = subtitle;

        StopAllCoroutines();
        StartCoroutine(AnimateIn());
    }

    private IEnumerator AnimateIn()
    {
        float t = 0f;
        float duration = 0.25f;

        Color bgColor = backgroundPanel.color;
        bgColor.a = 0f;
        backgroundPanel.color = bgColor;

        resultText.transform.localScale = Vector3.one * 0.8f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / duration);

            // Fade background
            bgColor.a = Mathf.Lerp(0f, 0.6f, lerp);
            backgroundPanel.color = bgColor;

            // Scale text
            resultText.transform.localScale =
                Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, lerp);

            yield return null;
        }

        // Ensure final values
        bgColor.a = 0.6f;
        backgroundPanel.color = bgColor;
        resultText.transform.localScale = Vector3.one;
    }

    public void RestartMatch()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
