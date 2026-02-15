using UnityEngine;
using UnityEngine.Audio;

public class SoundMixer : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] GameObject MainCanvas;
    [SerializeField] public Canvas PauseCanvas;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
    }
    public void TogglePause()
    {
        gameObject.SetActive(PauseCanvas.gameObject.activeSelf);
        PauseCanvas.gameObject.SetActive(!PauseCanvas.gameObject.activeSelf);
    }
    public void SetMasterVolume(float level)
    {
        audioMixer.SetFloat("masterVolume", Mathf.Log10(level) * 20f);
    }
    public void SetMusicVolume(float level)
    {
        audioMixer.SetFloat("musicVolume", Mathf.Log10(level) * 20f);
    }
    public void SetSFXVolume(float level)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(level) * 20f);
    }
}
