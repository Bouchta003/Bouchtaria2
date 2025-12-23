using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TabInputNavigator : MonoBehaviour
{
    [SerializeField] private TMP_InputField next;
    [SerializeField] private TMP_InputField previous;

    void Update()
    {
        if (!GetComponent<TMP_InputField>().isFocused)
            return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (shift && previous != null)
            {
                previous.Select();
                previous.ActivateInputField();
            }
            else if (!shift && next != null)
            {
                next.Select();
                next.ActivateInputField();
            }
        }
    }
}
