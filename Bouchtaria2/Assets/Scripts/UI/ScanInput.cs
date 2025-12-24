using UnityEngine;

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
        IsScanActive = Input.GetKey(KeyCode.Space);
    }
}

