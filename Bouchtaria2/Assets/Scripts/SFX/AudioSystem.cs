using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioSystem : MonoBehaviour
{
    public static AudioSystem Instance;
    [Header("Audio Children")]
    [SerializeField] SoundMixer soundMixer;
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
    }
    public void MainMenu()
    {
        if (GameRunContext.IsDungeonRun)
        {
            DungeonManager.Instance.ConcedeRun();
            GameFlowController.Instance.GoToDungeon();
        }
        else
            GameFlowController.Instance.GoToMainMenu();
    }
}
