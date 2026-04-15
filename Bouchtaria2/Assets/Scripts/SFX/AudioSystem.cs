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
    public void ToggleFullscreen()
    {
        if (Screen.fullScreenMode == FullScreenMode.Windowed)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
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
