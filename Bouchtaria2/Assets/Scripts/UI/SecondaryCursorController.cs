using UnityEngine;
using UnityEngine.SceneManagement;

public class SecondaryCursorController : MonoBehaviour
{
    public static SecondaryCursorController Instance;

    [SerializeField] private Camera targetCamera;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        gameObject.SetActive(false);
    }

    void Update()
    {
        // Show / hide
        if (Input.GetKeyDown(KeyCode.Space))
            gameObject.SetActive(true);

        if (Input.GetKeyUp(KeyCode.Space))
            gameObject.SetActive(false);

        // Follow mouse when active
        if (gameObject.activeSelf)
            FollowMouse();
    }

    void FollowMouse()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(targetCamera.transform.position.z);

        Vector3 worldPos = targetCamera.ScreenToWorldPoint(mousePos);
        worldPos.z = 0f;

        transform.position = worldPos;
    }

    // Call this when scenes change
    public void RefreshCamera()
    {
        targetCamera = Camera.main;
    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshCamera();
    }
}
