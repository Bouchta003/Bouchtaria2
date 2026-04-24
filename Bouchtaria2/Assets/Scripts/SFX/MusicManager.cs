using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using SFB;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Sources")]
    public AudioSource sourceA;
    public AudioSource sourceB;

    private AudioSource activeSource;
    private AudioSource inactiveSource;

    [Header("Scene Music Mapping")]
    [SerializeField] private List<SceneMusicEntry> sceneMusic = new List<SceneMusicEntry>();
    [SerializeField] private TMP_Dropdown musicDropdown;
    [SerializeField] private TextMeshProUGUI musicCurrentlyPlaying;
    [SerializeField] private TMP_Dropdown musicImportedDropdown;

    [Header("Settings")]
    [SerializeField] private float defaultFadeTime = 1.5f;

    [Header("Dialogue Ducking")]
    [Tooltip("Volume the active music fades to when dialogue begins (0–1). 0.12 feels cinematic.")]
    [SerializeField] [Range(0f, 1f)] private float dialogueDuckVolume = 0.12f;
    [Tooltip("How long (seconds) the music takes to duck down when dialogue starts.")]
    [SerializeField] private float dialogueDuckFadeIn = 1.5f;
    [Tooltip("How long (seconds) the music takes to come back up when dialogue ends with no track change.")]
    [SerializeField] private float dialogueUnduckFadeOut = 2.0f;

    [Header("Default Music")]
    [SerializeField] private AudioClip defaultMusic;

    private AudioClip currentClip;
    private Coroutine crossfadeCoroutine;

    // Tracks whether we are currently in a ducked state so unduck knows the right target.
    private bool isDucked = false;
    // Volume the active source was at before we ducked — so we restore to the same level.
    private float preDuckVolume = 1f;
    private Coroutine duckCoroutine;

    [System.Serializable]
    public class SceneMusicEntry
    {
        public string sceneName;
        public AudioClip music;
    }

    // ─────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        activeSource = sourceA;
        inactiveSource = sourceB;

        if (activeSource == null || inactiveSource == null)
            Debug.LogError("MusicManager: assign both audio sources in the inspector.");

        if (activeSource != null) activeSource.playOnAwake = false;
        if (inactiveSource != null) inactiveSource.playOnAwake = false;
    }

    private void Start()
    {
        LoadSavedMusicFolder();
        PopulateDropdown();
        Invoke(nameof(PlayInitialMusic), 0.05f);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    // ─────────────────────────────────────────────────────────────
    //  Public – basic playback
    // ─────────────────────────────────────────────────────────────

    /// <summary>Hard-pause the active source (use DuckForDialogue instead for cutscenes).</summary>
    public void PauseCurrentMusic()
    {
        activeSource.Pause();
    }

    /// <summary>Resume after a hard pause (use UnduckAfterDialogue instead for cutscenes).</summary>
    public void PlayCurrentMusic()
    {
        activeSource.Play();
    }

    // ─────────────────────────────────────────────────────────────
    //  Public – Dialogue ducking API
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Call when a dialogue/cutscene begins. Smoothly lowers the active music to
    /// a quiet underscore level so voices feel front-and-centre, film-style.
    /// </summary>
    public void DuckForDialogue()
    {
        if (duckCoroutine != null) StopCoroutine(duckCoroutine);

        // Remember what volume we're ducking FROM so we can restore it later.
        preDuckVolume = activeSource.volume;
        isDucked = true;

        duckCoroutine = StartCoroutine(FadeSourceVolume(activeSource, dialogueDuckVolume, dialogueDuckFadeIn));
    }

    /// <summary>
    /// Call when dialogue ends and there is NO incoming track switch.
    /// Smoothly restores the music to its pre-duck volume.
    /// </summary>
    public void UnduckAfterDialogue()
    {
        if (!isDucked) return;

        if (duckCoroutine != null) StopCoroutine(duckCoroutine);

        isDucked = false;
        duckCoroutine = StartCoroutine(FadeSourceVolume(activeSource, preDuckVolume, dialogueUnduckFadeOut));
    }

    /// <summary>
    /// Call when dialogue ends AND a new track should take over.
    /// Crossfades cleanly from the ducked music into the new clip.
    /// Because the active source is already at a low volume the outgoing fade
    /// sounds intentional, and the incoming track swells in naturally.
    /// </summary>
    public void UnduckAndCrossfadeTo(AudioClip newClip, float fadeTime = -1f)
    {
        if (duckCoroutine != null)
        {
            StopCoroutine(duckCoroutine);
            duckCoroutine = null;
        }

        isDucked = false;

        if (newClip == null)
        {
            // Nothing to play — just unduck
            UnduckAfterDialogue();
            return;
        }

        float usedFade = fadeTime < 0f ? defaultFadeTime : fadeTime;

        // Force a fresh crossfade even if the clip is the same (e.g. same track re-entering).
        currentClip = null;
        PlayMusic(newClip, usedFade);
    }

    // ─────────────────────────────────────────────────────────────
    //  Public – music selection helpers
    // ─────────────────────────────────────────────────────────────

    public void PlaySelectedMusic()
    {
        if (musicDropdown == null || allMusicClips.Count == 0) return;

        int index = musicDropdown.value;
        if (index < 0 || index >= allMusicClips.Count) return;

        PlayMusic(allMusicClips[index], defaultFadeTime);
    }

    public void PlaySelectedImportedMusic()
    {
        if (musicImportedDropdown == null || importedClips.Count == 0) return;

        int index = musicImportedDropdown.value;
        if (index < 0 || index >= importedClips.Count) return;

        PlayMusic(importedClips[index], defaultFadeTime);
    }

    public void PlayMusic(AudioClip newClip, float fadeTime)
    {
        if (newClip == null) return;

        if (currentClip == newClip && crossfadeCoroutine == null) return;

        currentClip = newClip;

        if (musicCurrentlyPlaying != null)
            musicCurrentlyPlaying.text = "Now Playing: " + newClip.name;

        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
            crossfadeCoroutine = null;
        }

        crossfadeCoroutine = StartCoroutine(Crossfade(newClip, fadeTime));
    }

    public AudioClip GetMusicForAdventure(int id)
    {
        List<AudioClip> candidates = new List<AudioClip>();

        foreach (SceneMusicEntry entry in sceneMusic)
            if (entry.sceneName == "Adventure" + id.ToString())
                candidates.Add(entry.music);

        return GetRandomClip(candidates);
    }

    // ─────────────────────────────────────────────────────────────
    //  Public – folder / import
    // ─────────────────────────────────────────────────────────────

    private List<AudioClip> allMusicClips = new List<AudioClip>();
    private List<AudioClip> importedClips = new List<AudioClip>();

    public void ClearSavedFolder()
    {
        PlayerPrefs.DeleteKey("MusicFolderPath");
        importedClips.Clear();
        musicImportedDropdown.ClearOptions();
    }

    public void BrowseFolder()
    {
        var paths = StandaloneFileBrowser.OpenFolderPanel("Select Music Folder", "", false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            PlayerPrefs.SetString("MusicFolderPath", paths[0]);
            PlayerPrefs.Save();
            StartCoroutine(LoadMusicFromFolder(paths[0]));
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Public – stop
    // ─────────────────────────────────────────────────────────────

    public void StopMusicImmediate()
    {
        if (crossfadeCoroutine != null) { StopCoroutine(crossfadeCoroutine); crossfadeCoroutine = null; }
        if (duckCoroutine != null) { StopCoroutine(duckCoroutine); duckCoroutine = null; }

        isDucked = false;

        if (activeSource != null) { activeSource.Stop(); activeSource.volume = 0f; }
        if (inactiveSource != null) { inactiveSource.Stop(); inactiveSource.volume = 0f; }

        currentClip = null;
    }

    // ─────────────────────────────────────────────────────────────
    //  Scene loading
    // ─────────────────────────────────────────────────────────────

    private void PlayInitialMusic() => PlayMusicForScene(SceneManager.GetActiveScene().name);

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => PlayMusicForScene(scene.name);

    private void PlayMusicForScene(string sceneName)
    {
        AudioClip newClip = GetMusicForScene(sceneName);
        List<AudioClip> firebase = GetFirebaseMusicCandidates();

        if (newClip == null)
        {
            bool alreadyOnFirebase = currentClip != null && firebase.Contains(currentClip);
            if (alreadyOnFirebase) return;
            newClip = GetRandomClip(firebase) ?? defaultMusic;
        }

        if (newClip != null)
        {
            if (SceneManager.GetActiveScene().name == "Combat" && GameRunContext.IsAdventureCombat)
                newClip = GetMusicForAdventure(GameRunContext.AdventureFightId);
            else
            PlayMusic(newClip, defaultFadeTime);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Private helpers
    // ─────────────────────────────────────────────────────────────

    private AudioClip GetMusicForScene(string sceneName)
    {
        List<AudioClip> candidates = new List<AudioClip>();
        foreach (SceneMusicEntry entry in sceneMusic)
            if (entry.sceneName == sceneName && entry.music != null)
                candidates.Add(entry.music);
        return GetRandomClip(candidates);
    }

    private List<AudioClip> GetFirebaseMusicCandidates()
    {
        List<AudioClip> list = new List<AudioClip>();
        foreach (SceneMusicEntry entry in sceneMusic)
            if (entry.sceneName == "Firebase" && entry.music != null)
                list.Add(entry.music);
        return list;
    }

    private AudioClip GetRandomClip(List<AudioClip> candidates)
    {
        if (candidates == null || candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)];
    }

    private void PopulateDropdown()
    {
        musicDropdown.ClearOptions();
        allMusicClips.Clear();

        List<string> options = new List<string>();
        foreach (SceneMusicEntry entry in sceneMusic)
        {
            if (entry.music != null && !allMusicClips.Contains(entry.music))
            {
                allMusicClips.Add(entry.music);
                options.Add(entry.music.name);
            }
        }
        musicDropdown.AddOptions(options);
    }

    private void LoadSavedMusicFolder()
    {
        if (!PlayerPrefs.HasKey("MusicFolderPath")) return;
        string path = PlayerPrefs.GetString("MusicFolderPath");
        if (string.IsNullOrEmpty(path)) return;
        if (!System.IO.Directory.Exists(path)) { Debug.LogWarning("Saved music folder no longer exists."); return; }
        StartCoroutine(LoadMusicFromFolder(path));
    }

    private IEnumerator LoadMusicFromFolder(string folderPath)
    {
        importedClips.Clear();
        musicImportedDropdown.ClearOptions();

        string[] files = System.IO.Directory.GetFiles(folderPath, "*.mp3");
        List<string> options = new List<string>();

        foreach (string file in files)
        {
            yield return StartCoroutine(LoadAudioFile(file, (clip) =>
            {
                if (clip != null)
                {
                    importedClips.Add(clip);
                    options.Add(System.IO.Path.GetFileNameWithoutExtension(file));
                }
            }));
        }

        if (options.Count == 0)
            Debug.LogWarning("No MP3 files found in folder.");
        else
            musicImportedDropdown.AddOptions(options);
    }

    private IEnumerator LoadAudioFile(string path, System.Action<AudioClip> onLoaded)
    {
        using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                clip.name = System.IO.Path.GetFileNameWithoutExtension(path);
                onLoaded?.Invoke(clip);
            }
            else
            {
                Debug.LogError("Failed to load: " + path);
                onLoaded?.Invoke(null);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Coroutines
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Smoothly ramps a single AudioSource to targetVolume over duration seconds.
    /// Used for ducking/unducking — does not touch the inactive source.
    /// </summary>
    private IEnumerator FadeSourceVolume(AudioSource source, float targetVolume, float duration)
    {
        if (source == null) yield break;

        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        source.volume = targetVolume;
        duckCoroutine = null;
    }

    private IEnumerator Crossfade(AudioClip newClip, float fadeTime)
    {
        // Immediate (no fade) path
        if (fadeTime <= 0f)
        {
            if (inactiveSource == null || activeSource == null) yield break;

            inactiveSource.clip = newClip;
            inactiveSource.loop = true;
            inactiveSource.volume = 1f;
            inactiveSource.Play();

            activeSource.Stop();

            var tmp = activeSource;
            activeSource = inactiveSource;
            inactiveSource = tmp;

            inactiveSource.volume = 0f;
            crossfadeCoroutine = null;
            yield break;
        }

        // Prepare the incoming source
        inactiveSource.clip = newClip;
        inactiveSource.loop = true;
        inactiveSource.volume = 0f;

        if (!inactiveSource.isPlaying)
            inactiveSource.Play();

        float elapsed = 0f;
        // Fade OUT from whatever volume the active source is currently at.
        // This is key: if we're ducked to 0.12, we fade from 0.12 → 0, not 1 → 0,
        // so the crossfade sounds smooth regardless of the current duck state.
        float startVolume = activeSource.isPlaying ? activeSource.volume : 1f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeTime);

            if (activeSource != null) activeSource.volume = Mathf.Lerp(startVolume, 0f, t);
            if (inactiveSource != null) inactiveSource.volume = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        if (activeSource != null) activeSource.volume = 0f;
        if (inactiveSource != null) inactiveSource.volume = 1f;

        if (activeSource != null && activeSource.isPlaying)
            activeSource.Stop();

        var temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;

        inactiveSource.volume = 0f;
        crossfadeCoroutine = null;

        // Reset duck state — after a crossfade we are always at full volume.
        isDucked = false;
        preDuckVolume = 1f;
    }
}