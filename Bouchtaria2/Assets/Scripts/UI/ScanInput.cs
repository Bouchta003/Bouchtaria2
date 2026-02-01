using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScanInput : MonoBehaviour
{
    public static ScanInput Instance;
    public bool IsScanActive { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (UIInputFocusTracker.IsAnyTMPFocused)
        {
            IsScanActive = false;
            return;
        }

        IsScanActive = Input.GetKey(KeyCode.Space);
    }
}

