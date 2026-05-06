using UnityEngine;
using System.Collections.Generic;

public class SFXManager : MonoBehaviour
{
    [SerializeField] AudioSource SFXprefab;

    public static SFXManager Instance;

    // Prevents identical clips from stacking instantly
    private Dictionary<AudioClip, float> lastPlayedTime = new();

    [SerializeField] private float sameClipCooldown = 0.05f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void PlaySFXClip(AudioClip clip, Transform spawnTransform, float volume)
    {
        if (clip == null) return;

        // Block spam
        if (lastPlayedTime.TryGetValue(clip, out float lastTime))
        {
            if (Time.time - lastTime < sameClipCooldown)
                return;
        }

        lastPlayedTime[clip] = Time.time;

        AudioSource audioSource = Instantiate(
            SFXprefab,
            spawnTransform.position,
            Quaternion.identity
        );

        audioSource.clip = clip;

        // Tiny variation makes repeats sound natural
        audioSource.pitch = Random.Range(0.96f, 1.04f);

        audioSource.volume = volume;
        audioSource.Play();

        Destroy(audioSource.gameObject, clip.length);
    }

    public void PlayRandomSFXClip(AudioClip[] clips, Transform spawnTransform, float volume)
    {
        if (clips == null || clips.Length == 0) return;

        int rand = Random.Range(0, clips.Length);
        PlaySFXClip(clips[rand], spawnTransform, volume);
    }

    public void PlayRandomSFXClip(List<AudioClip> clips, Transform spawnTransform, float volume)
    {
        if (clips == null || clips.Count == 0) return;

        int rand = Random.Range(0, clips.Count);
        PlaySFXClip(clips[rand], spawnTransform, volume);
    }
}