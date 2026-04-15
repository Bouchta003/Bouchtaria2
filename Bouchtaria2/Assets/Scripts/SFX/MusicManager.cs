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
    [SerializeField, Range(0f, 1f)]
    private float adventureDialogueVolumeMultiplier = 0.05f;

    [Header("Default Music")]
    [SerializeField]
    private AudioClip defaultMusic;

    private AudioClip currentClip;
    private bool isAdventureDialogueMusicDucked;

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

    public void PauseCurrentMusic()
    {
        activeSource.Pause();
    }
    public void PlayCurrentMusic()
    {
        activeSource.Play();
    }
    public void SetAdventureDialogueMusicDuck(bool isActive)
    {
        if (!GameRunContext.IsAdventureCombat)
            return;

        isAdventureDialogueMusicDucked = isActive;

        if (isActive)
            PlayCurrentMusic();

        ApplyCurrentVolumes();
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
        PauseCurrentMusic();
    }

    private void PlayMusicForScene(string sceneName)
    {
        AudioClip newClip = GetMusicForScene(sceneName);
        List<AudioClip> firebaseMusics = GetFirebaseMusicCandidates();

        // Use Firebase music as the default fallback for scenes without a mapping.
        if (newClip == null)
        {
            bool firebaseMusicAlreadyPlaying = currentClip != null && firebaseMusics.Contains(currentClip);

            // Keep the current track if we're already on one of the Firebase tracks
            // so scene transitions stay musically smooth.
            if (firebaseMusicAlreadyPlaying)
                return;

            newClip = GetRandomClip(firebaseMusics) ?? defaultMusic;
        }

        if (newClip != null)
        {
            if(SceneManager.GetActiveScene().name=="Combat" && GameRunContext.IsAdventureCombat)
            {
                newClip = GetMusicForAdventure(GameRunContext.AdventureFightId);
            }
            PlayMusic(newClip, defaultFadeTime);
        }
    }
    private AudioClip GetMusicForAdventure(int id)
    {
        List<AudioClip> candidates = new List<AudioClip>();

        foreach (SceneMusicEntry entry in sceneMusic)
        {
            if (entry.sceneName == "Adventure"+id.ToString())
            {
                candidates.Add(entry.music);
            }
        }

        if (candidates.Count == 0)
            return null;

        return GetRandomClip(candidates);
    }
    private AudioClip GetMusicForScene(string sceneName)
    {
        List<AudioClip> candidates = new List<AudioClip>();

        foreach (SceneMusicEntry entry in sceneMusic)
        {
            if (entry.sceneName == sceneName && entry.music != null)
            {
                candidates.Add(entry.music);
            }
        }

        if (candidates.Count == 0)
            return null;

        return GetRandomClip(candidates);
    }

    private List<AudioClip> GetFirebaseMusicCandidates()
    {
        List<AudioClip> firebaseCandidates = new List<AudioClip>();

        foreach (SceneMusicEntry entry in sceneMusic)
        {
            if (entry.sceneName == "Firebase" && entry.music != null)
            {
                firebaseCandidates.Add(entry.music);
            }
        }

        return firebaseCandidates;
    }

    private AudioClip GetRandomClip(List<AudioClip> candidates)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

        int index = Random.Range(0, candidates.Count);
        return candidates[index];
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

    private float GetTargetMusicVolume()
    {
        if (GameRunContext.IsAdventureCombat && isAdventureDialogueMusicDucked)
            return adventureDialogueVolumeMultiplier;

        return 1f;
    }

    private void ApplyCurrentVolumes()
    {
        float targetVolume = GetTargetMusicVolume();

        if (activeSource != null)
            activeSource.volume = targetVolume;

        if (inactiveSource != null && crossfadeCoroutine == null)
            inactiveSource.volume = 0f;
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
            inactiveSource.volume = GetTargetMusicVolume();
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
            float targetVolume = GetTargetMusicVolume();

            if (activeSource != null)
                activeSource.volume = Mathf.Lerp(startVolume, 0f, t);

            if (inactiveSource != null)
                inactiveSource.volume = Mathf.Lerp(0f, targetVolume, t);

            yield return null;
        }

        // Ensure final volumes
        if (activeSource != null)
            activeSource.volume = 0f;

        if (inactiveSource != null)
            inactiveSource.volume = GetTargetMusicVolume();

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
