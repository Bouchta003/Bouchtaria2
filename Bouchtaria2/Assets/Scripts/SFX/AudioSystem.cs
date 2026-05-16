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
        if (GameRunContext.IsDungeonRun || GameRunContext.IsPathOfPowerRun) MenuButton.GetComponentInChildren<TextMeshProUGUI>().text = "Concede";
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

            AdventureProgressionService.RecordFightResult(
                GameRunContext.AdventureFightId,
                false,
                GameRunContext.IsAdventureHardMode);

            Screen.SetResolution(
                Screen.currentResolution.width,
                Screen.currentResolution.height,
                FullScreenMode.FullScreenWindow);
        }
        else
        {
            // Restore previous windowed resolution
            Screen.SetResolution(
                _windowedResolution.x,
                _windowedResolution.y,
                FullScreenMode.Windowed);
        }
    }
    public void MainMenu()
    {
        TogglePause();

        // Treat leaving mid-game as a forfeit for both adventure and dungeon
        bool gameStillInProgress = GameManager.Instance != null &&
                                   GameManager.Instance.CurrentGameState == GameState.Playing;

        if (gameStillInProgress && GameRunContext.IsAdventureCombat)
        {
            AdventureProgressionService.SetAdventureCombatActive(false);
            AdventureProgressionService.RecordFightResult(GameRunContext.AdventureFightId, false);
        }

        if (GameRunContext.IsPathOfPowerRun)
        {
            PathOfPowerCombatService.MarkCombatLost();
            GameFlowController.Instance.GoToPathOfPower();
            return;
        }

        if (GameRunContext.IsDungeonRun)
        {
            DungeonManager.Instance.ConcedeRun();
            DungeonManager.SetDungeonCombatActive(false);
            GameFlowController.Instance.GoToDungeon();
            return;
        }

        GameFlowController.Instance.GoToMainMenu();
    }

    private void OnApplicationQuit()
    {
        bool gameStillInProgress = GameManager.Instance != null &&
                                   GameManager.Instance.CurrentGameState == GameState.Playing;
        if (!gameStillInProgress) return;

        if (GameRunContext.IsAdventureCombat)
        {
            AdventureProgressionService.SetAdventureCombatActive(false);
            AdventureProgressionService.RecordFightResult(GameRunContext.AdventureFightId, false);
        }

        if (GameRunContext.IsPathOfPowerRun)
        {
            PathOfPowerCombatService.MarkCombatLost();
        }

        if (GameRunContext.IsDungeonRun)
        {
            DungeonManager.Instance?.ConcedeRun();
            DungeonManager.SetDungeonCombatActive(false);
        }
    }
}
