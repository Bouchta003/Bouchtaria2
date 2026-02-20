using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;

public class WinLoseUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI subText;
    [SerializeField] private GameObject restartBtn;
    [SerializeField] private Image backgroundPanel;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void ShowWin()
    {
        if (GameRunContext.IsDungeonRun)
        {
            DungeonManager.Instance.IncrementStreak();

            restartBtn.SetActive(false);
            Setup("VICTORY", Color.green, "Enemy Core Destroyed, you earned 100 Gold and 20 Coins");
            return;
        }

        Setup("VICTORY", Color.green, "Enemy Core Destroyed, you earned 100 Gold");
    }

    public void ShowLose()
    {
        if (GameRunContext.IsDungeonRun)
        {
            if (GameRunContext.DungeonData.augments.Contains(DungeonShop.Augment.ExtraLife))
            {
                DungeonManager.Instance.CurrentRun.augments.Remove(DungeonShop.Augment.ExtraLife);
                DungeonManager.Instance.SaveRunData();
                Setup("DEFEAT", Color.red, "Your Core Was Destroyed, your extra life keeps you alive for one more chance !");
                restartBtn.SetActive(false);
                return;
            }
            else
                DungeonManager.Instance.ResetStreak(); 
            restartBtn.SetActive(false); 
        }
        Setup("DEFEAT", Color.red, "Your Core Was Destroyed, you earned 20 Gold as compensation");
    }
    public void LeaveToMenu()
    {
        if (GameRunContext.IsDungeonRun) {
            if (resultText.color != Color.red)
            { SceneManager.LoadScene("DungeonAdventure"); }
            else { SceneManager.LoadScene("DungeonMenu"); }
        } 
        else SceneManager.LoadScene("Main_Menu");
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
