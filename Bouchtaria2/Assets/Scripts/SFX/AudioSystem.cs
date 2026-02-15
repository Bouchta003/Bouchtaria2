using UnityEngine;

public class AudioSystem : MonoBehaviour
{
    public static AudioSystem Instance;
    [Header("Audio Children")]
    [SerializeField] SoundMixer soundMixer;

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
}
