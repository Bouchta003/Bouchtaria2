using TMPro;
using UnityEngine;

public class WinLoseUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI subText;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void ShowWin()
    {
        gameObject.SetActive(true);
        resultText.text = "VICTORY";
        resultText.color = Color.green;

        if (subText != null)
            subText.text = "Enemy Core Destroyed";
    }

    public void ShowLose()
    {
        gameObject.SetActive(true);
        resultText.text = "DEFEAT";
        resultText.color = Color.red;

        if (subText != null)
            subText.text = "Your Core Was Destroyed";
    }
}
