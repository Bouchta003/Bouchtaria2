using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GlobalUISelectionClearer : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name == "Firebase")
            return;

        if (EventSystem.current == null)
            return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        bool typingInInput =
            selected != null &&
            (selected.GetComponent<TMP_InputField>() != null ||
             selected.GetComponent<InputField>() != null);

        // Clear selection on mouse click
        if (Input.GetMouseButtonDown(0))
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // Clear selection on gameplay keys (but NOT while typing)
        if (!typingInInput &&
            (Input.GetKeyDown(KeyCode.Space) ||
             Input.GetKeyDown(KeyCode.Escape) ||
             Input.GetKeyDown(KeyCode.Tab)))
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
