using TMPro;
using UnityEngine;

public class UIInputFocusTracker : MonoBehaviour
{
    public static bool IsAnyTMPFocused { get; private set; }

    void Update()
    {
        IsAnyTMPFocused = false;

        TMP_InputField[] fields = FindObjectsByType<TMP_InputField>(sortMode:FindObjectsSortMode.None);

        foreach (var field in fields)
        {
            if (field.isFocused)
            {
                IsAnyTMPFocused = true;
                return;
            }
        }
    }
}
