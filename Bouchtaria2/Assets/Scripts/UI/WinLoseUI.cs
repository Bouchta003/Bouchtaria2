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
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetInteractionEnabled(true);
        gameObject.SetActive(false);
    }
    public void SetInteractionEnabled(bool enabled)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = true;
    }

    public void ShowWin()
    {
            int i = Random.Range(0,GameManager.Instance.winSFX.Count);
            SFXManager.Instance.PlaySFXClip(GameManager.Instance.winSFX[i], transform, 1f);
        if (GameRunContext.IsDungeonRun)
        {
            DungeonManager.Instance.IncrementStreak();

            restartBtn.SetActive(false);
            Setup("VICTORY", Color.green, "Enemy Core Destroyed, you earned 100 Gold and 20 Coins");
            SFXManager.Instance.PlaySFXClip(GameManager.Instance.winSFX[i], transform, 1f);
            return;
        }
        if (GameRunContext.IsAdventureCombat && !GameManager.Instance.adventureBossSecondPhaseTriggered)
        {
            restartBtn.SetActive(false);
            Setup("VICTORY", Color.green, "Enemy defeated, you earned 100 Gold");
            SFXManager.Instance.PlaySFXClip(GameManager.Instance.winSFX[i], transform, 1f);
            return;
        }
        if (GameRunContext.IsAdventureCombat && !GameManager.Instance.adventureBossFinalDialogueTriggered)
        {
            restartBtn.SetActive(false);
            Setup("VICTORY", Color.green, "Congrats !!!\nYou ... Win ?");
            SFXManager.Instance.PlaySFXClip(GameManager.Instance.winSFX[i], transform, 1f);
            return;
        }
        if (GameRunContext.IsAdventureCombat && GameManager.Instance.adventureBossSecondPhaseTriggered)
        {
            restartBtn.SetActive(false);
            if(GameRunContext.IsAdventureHardMode)
            {
                Setup("VICTORY", Color.green, "You completed Bouchtaria 1 in Hard Mode, but remember this was only the prequel. The true story hasn't even begun !\n(+2000 Gold btw you deserve it)");
                SFXManager.Instance.PlaySFXClip(GameManager.Instance.winSFX[i], transform, 1f);
                return;
            }
            Setup("VICTORY", Color.green, "You completed Bouchtaria 1...but you know this was only the fun easy mode.\nNow you get to do it in hard mode !");
            SFXManager.Instance.PlaySFXClip(GameManager.Instance.winSFX[i], transform, 1f);
            return;
        }

        Setup("VICTORY", Color.green, "Enemy Core Destroyed, you earned 100 Gold");
        SFXManager.Instance.PlaySFXClip(GameManager.Instance.winSFX[i], transform, 1f);
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
                SFXManager.Instance.PlaySFXClip(GameManager.Instance.fahSFX, transform, 1f);
                restartBtn.SetActive(false);
                return;
            }
            else
                DungeonManager.Instance.ResetStreak(); 
            restartBtn.SetActive(false); 
        }
        Setup("DEFEAT", Color.red, "Your Core Was Destroyed, you earned 20 Gold as compensation");
        SFXManager.Instance.PlaySFXClip(GameManager.Instance.fahSFX, transform, 1f);
    }
    public void LeaveToMenu()
    {
        if (GameRunContext.IsDungeonRun) {
            if (resultText.color != Color.red)
            { SceneManager.LoadScene("DungeonAdventure"); }
            else { SceneManager.LoadScene("DungeonMenu"); }
            return;
        }

        if (GameRunContext.IsAdventureCombat)
        {
            SceneManager.LoadScene("Main_Menu");
            return;
        }

        SceneManager.LoadScene("Main_Menu");
    }
    private void Setup(string title, Color color, string subtitle)
    {
        SetInteractionEnabled(true);
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
