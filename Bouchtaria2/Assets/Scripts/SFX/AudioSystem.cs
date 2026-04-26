using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioSystem : MonoBehaviour
{
    public static AudioSystem Instance;
    [Header("Audio Children")]
    [SerializeField] public SoundMixer soundMixer;
    [SerializeField] public GameObject PauseCanvas;
    [SerializeField] GameObject MenuButton;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
    }
    public void TogglePause()
    {
        PauseCanvas.SetActive(!PauseCanvas.activeSelf);
        if (GameRunContext.IsDungeonRun) MenuButton.GetComponentInChildren<TextMeshProUGUI>().text = "Concede";
        if (SceneManager.GetActiveScene().name == "Firebase") MenuButton.SetActive(false);
        else MenuButton.SetActive(true);
    }
    private Vector2Int _windowedResolution = new Vector2Int(1920, 1080);

    public void ToggleFullscreen()
    {
        if (Screen.fullScreenMode == FullScreenMode.Windowed)
        {
            // Save current windowed size before going fullscreen
            _windowedResolution = new Vector2Int(Screen.width, Screen.height);

            // Use the native desktop resolution for fullscreen — no zoom, no stretch
            Resolution native = Screen.currentResolution;
            Screen.SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            // Restore the exact windowed resolution we had before
            Screen.SetResolution(
                _windowedResolution.x,
                _windowedResolution.y,
                FullScreenMode.Windowed);
        }
    }
    public void MainMenu()
    {
        TogglePause();
        if (GameRunContext.IsDungeonRun)
        {
            DungeonManager.Instance.ConcedeRun();
            GameFlowController.Instance.GoToDungeon();
        }
        else
            GameFlowController.Instance.GoToMainMenu();
    }
}
