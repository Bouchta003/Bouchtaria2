using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScanInput : MonoBehaviour
{
    public static ScanInput Instance;
    public bool IsScanActive { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (UIInputFocusTracker.IsAnyTMPFocused)
        {
            IsScanActive = false;
            return;
        }

        IsScanActive = Input.GetKey(KeyCode.Space);
    }

    bool IsTypingInAnyInputField()
    {
        if (EventSystem.current == null)
            return false;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
            return false;

        return selected.GetComponent<TMP_InputField>() != null ||
               selected.GetComponent<InputField>() != null;
    }

    private bool IsSceneDisabled()
    {
        string scene = SceneManager.GetActiveScene().name;
        foreach (var s in FindFirstObjectByType<SecondaryCursorController>().disabledScenes)
        {
            if (scene == s)
                return true;
        }
        return false;
    }
}

