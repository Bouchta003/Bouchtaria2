using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GlobalUISelectionClearer : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name != "Firebase")
        {
            if (EventSystem.current == null)
                return;

            // Clear selection on mouse click
            if (Input.GetMouseButtonDown(0))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            // Clear selection on gameplay keys
            if (Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.Escape) ||
                Input.GetKeyDown(KeyCode.Tab))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }
}
