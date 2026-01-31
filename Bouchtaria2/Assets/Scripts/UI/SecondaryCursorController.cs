using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SecondaryCursorController : MonoBehaviour
{
    public static SecondaryCursorController Instance;

    [Header("Cursors")]
    [SerializeField] private Texture2D scanCursor;

    [Header("Optional")]
    [SerializeField] public string[] disabledScenes;

    private bool cursorIsScan;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetDefaultCursor();
    }
    void Update()
    {
        if (UIInputFocusTracker.IsAnyTMPFocused)
        {
            SetDefaultCursor();
            return;
        }

        if (ScanInput.Instance != null && ScanInput.Instance.IsScanActive)
            SetScanCursor();
        else
            SetDefaultCursor();
    }
    private void SetScanCursor()
    {
        if (cursorIsScan) return;

        cursorIsScan = true;
        Cursor.SetCursor(scanCursor, Vector2.zero, CursorMode.Auto);
    }

    private void SetDefaultCursor()
    {
        if (!cursorIsScan) return;

        cursorIsScan = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

}
