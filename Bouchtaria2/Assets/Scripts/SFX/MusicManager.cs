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
    private void Start()
    {
        LoadSavedMusicFolder();
        PopulateDropdown();
        Invoke(nameof(PlayInitialMusic), 0.05f);
    }
    public void PauseCurrentMusic()
    {
        activeSource.Pause();
    }
    public void PlayCurrentMusic()
    {
        activeSource.Play();
    }
    public void PlaySelectedMusic()
    {
        if (musicDropdown == null || allMusicClips.Count == 0)
            return;

        int index = musicDropdown.value;

        if (index < 0 || index >= allMusicClips.Count)
            return;

        AudioClip selectedClip = allMusicClips[index];
        PlayMusic(selectedClip, defaultFadeTime);
    }
    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private List<AudioClip> allMusicClips = new List<AudioClip>();
    private List<AudioClip> importedClips = new List<AudioClip>();
    public void ClearSavedFolder()
    {
        PlayerPrefs.DeleteKey("MusicFolderPath");
        importedClips.Clear();
        musicImportedDropdown.ClearOptions();
    }
    private void LoadSavedMusicFolder()
    {
        if (!PlayerPrefs.HasKey("MusicFolderPath"))
            return;

        string folderPath = PlayerPrefs.GetString("MusicFolderPath");

        if (string.IsNullOrEmpty(folderPath))
            return;

        if (!System.IO.Directory.Exists(folderPath))
        {
            Debug.LogWarning("Saved music folder no longer exists.");
            return;
        }

        StartCoroutine(LoadMusicFromFolder(folderPath));
    }
    public void BrowseFolder()
    {
        var paths = StandaloneFileBrowser.OpenFolderPanel("Select Music Folder", "", false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string folderPath = paths[0];

            // Save it
            PlayerPrefs.SetString("MusicFolderPath", folderPath);
            PlayerPrefs.Save();

            StartCoroutine(LoadMusicFromFolder(folderPath));
        }
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
        {
            Debug.LogWarning("No MP3 files found in folder.");

        }else
        musicImportedDropdown.AddOptions(options);
    }
    public void PlaySelectedImportedMusic()
    {
        if (musicImportedDropdown == null || importedClips.Count == 0)
            return;

        int index = musicImportedDropdown.value;

        if (index < 0 || index >= importedClips.Count)
            return;

        AudioClip selectedClip = importedClips[index];
        PlayMusic(selectedClip, defaultFadeTime);
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
    public AudioClip GetMusicForAdventure(int id)
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

        // ✅ UPDATE UI TEXT HERE
        if (musicCurrentlyPlaying != null)
        {
            musicCurrentlyPlaying.text = "Now Playing: " + newClip.name;
        }

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
