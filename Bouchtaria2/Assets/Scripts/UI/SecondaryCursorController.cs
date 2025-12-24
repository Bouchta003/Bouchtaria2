using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SecondaryCursorController : MonoBehaviour
{
    public static SecondaryCursorController Instance;

    [Header("Cursors")]
    [SerializeField] private Texture2D scanCursor;

    [Header("Optional")]
    [SerializeField] private string[] disabledScenes;

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
        bool shouldScan =
            Input.GetKey(KeyCode.Space) &&
            !IsSceneDisabled();

        if (shouldScan)
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

    private bool IsSceneDisabled()
    {
        string scene = SceneManager.GetActiveScene().name;
        foreach (var s in disabledScenes)
        {
            if (scene == s)
                return true;
        }
        return false;
    }
}
