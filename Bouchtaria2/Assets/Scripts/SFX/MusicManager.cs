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

    private Coroutine crossfadeCoroutine;
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

        // safety checks
        if (activeSource == null || inactiveSource == null)
            Debug.LogError("MusicManager: assign both audio sources in the inspector.");

        // ensure sources are configured correctly
        if (activeSource != null) activeSource.playOnAwake = false;
        if (inactiveSource != null) inactiveSource.playOnAwake = false;
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Start()
    {
        Invoke(nameof(PlayInitialMusic), 0.05f);
    }
    private void PlayInitialMusic()
    {
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
        if (newClip == null)
            return;

        if (currentClip == newClip && crossfadeCoroutine == null)
            return;

        currentClip = newClip;

        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
            crossfadeCoroutine = null;
        }

        crossfadeCoroutine = StartCoroutine(Crossfade(newClip, fadeTime));
    }


    private IEnumerator Crossfade(AudioClip newClip, float fadeTime)
    {
        // Immediate (no fade) path
        if (fadeTime <= 0f)
        {
            if (inactiveSource == null || activeSource == null)
                yield break;

            inactiveSource.clip = newClip;
            inactiveSource.loop = true;
            inactiveSource.volume = 1f;
            inactiveSource.Play();

            activeSource.Stop();

            // swap
            var tmp = activeSource;
            activeSource = inactiveSource;
            inactiveSource = tmp;

            inactiveSource.volume = 0f;
            crossfadeCoroutine = null;
            yield break;
        }

        // Prepare inactive source with new clip
        inactiveSource.clip = newClip;
        inactiveSource.loop = true;
        inactiveSource.volume = 0f;

        if (!inactiveSource.isPlaying)
            inactiveSource.Play();

        float elapsed = 0f;
        float startVolume = activeSource.isPlaying ? activeSource.volume : 1f;

        // Do the fade
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeTime);

            if (activeSource != null)
                activeSource.volume = Mathf.Lerp(startVolume, 0f, t);

            if (inactiveSource != null)
                inactiveSource.volume = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        // Ensure final volumes
        if (activeSource != null)
            activeSource.volume = 0f;

        if (inactiveSource != null)
            inactiveSource.volume = 1f;

        // Stop old source and swap references
        if (activeSource != null && activeSource.isPlaying)
            activeSource.Stop();


        var temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;

        // make sure inactive is silent (so it's ready next time)
        inactiveSource.volume = 0f;

        crossfadeCoroutine = null;
    }
    // Optional quick stop
    public void StopMusicImmediate()
    {
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
            crossfadeCoroutine = null;
        }

        if (activeSource != null)
        {
            activeSource.Stop();
            activeSource.volume = 0f;
        }

        if (inactiveSource != null)
        {
            inactiveSource.Stop();
            inactiveSource.volume = 0f;
        }

        currentClip = null;
    }
}