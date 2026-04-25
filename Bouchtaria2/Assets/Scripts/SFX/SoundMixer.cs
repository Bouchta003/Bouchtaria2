using UnityEngine;
using UnityEngine.Audio;

public class SoundMixer : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    private const string MASTER_KEY = "MasterVolume";
    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SFXVolume";

    private void Awake()
    {
        LoadVolumes();
    }

    public void SetMasterVolume(float level)
    {
        SetVolume("masterVolume", MASTER_KEY, level);
    }

    public void SetMusicVolume(float level)
    {
        SetVolume("musicVolume", MUSIC_KEY, level);
    }

    public void SetSFXVolume(float level)
    {
        SetVolume("SFXVolume", SFX_KEY, level);
    }

    private void SetVolume(string mixerParameter, string prefKey, float level)
    {
        float clampedLevel = Mathf.Clamp(level, 0.0001f, 1f); // prevent 0
        float db = Mathf.Log10(clampedLevel) * 20f;

        audioMixer.SetFloat(mixerParameter, db);

        PlayerPrefs.SetFloat(prefKey, level);
        PlayerPrefs.Save();
    }

    private void LoadVolumes()
    {
        float master = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        float music = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        float sfx = PlayerPrefs.GetFloat(SFX_KEY, 1f);

        audioMixer.SetFloat("masterVolume", Mathf.Log10(master) * 20f);
        audioMixer.SetFloat("musicVolume", Mathf.Log10(music) * 20f);
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(sfx) * 20f);
    }
}
