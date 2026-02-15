using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Sources")]
    public AudioSource sourceA;
    public AudioSource sourceB;

    private AudioSource activeSource;
    private AudioSource inactiveSource;

    [Header("Scene Music Mapping")]
    [SerializeField]
    private List<SceneMusicEntry> sceneMusic = new List<SceneMusicEntry>();

    [Header("Settings")]
    [SerializeField]
    private float defaultFadeTime = 1.5f;

    [Header("Default Music")]
    [SerializeField]
    private AudioClip defaultMusic;

    private AudioClip currentClip;

    [System.Serializable]
    public class SceneMusicEntry
    {
        public string sceneName;
        public AudioClip music;
    }

    private void Awake()
    {
        // Singleton safety
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        activeSource = sourceA;
        inactiveSource = sourceB;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Play music for current scene on startup
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    private void PlayMusicForScene(string sceneName)
    {
        AudioClip newClip = GetMusicForScene(sceneName);

        // Use default music if scene not found
        if (newClip == null)
        {
            newClip = defaultMusic;
        }

        if (newClip != null)
        {
            PlayMusic(newClip, defaultFadeTime);
        }
    }

    private AudioClip GetMusicForScene(string sceneName)
    {
        foreach (SceneMusicEntry entry in sceneMusic)
        {
            if (entry.sceneName == sceneName)
                return entry.music;
        }

        return null;
    }

    public void PlayMusic(AudioClip newClip, float fadeTime)
    {
        if (currentClip == newClip)
            return;

        currentClip = newClip;

        StartCoroutine(Crossfade(newClip, fadeTime));
    }

    private IEnumerator Crossfade(AudioClip newClip, float fadeTime)
    {
        inactiveSource.clip = newClip;
        inactiveSource.volume = 0;
        inactiveSource.loop = true;
        inactiveSource.Play();

        float time = 0f;
        float startVolume = activeSource.volume;

        while (time < fadeTime)
        {
            time += Time.deltaTime;

            activeSource.volume = Mathf.Lerp(startVolume, 0, time / fadeTime);
            inactiveSource.volume = Mathf.Lerp(0, 1, time / fadeTime);

            yield return null;
        }

        activeSource.Stop();

        // swap sources
        AudioSource temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;
    }
}
