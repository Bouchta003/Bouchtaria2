using UnityEngine;

public class SFXManager : MonoBehaviour
{
    [SerializeField] AudioSource SFXprefab;
    public static SFXManager Instance;

    private void Awake()
    {
        if(Instance==null)
        Instance = this;
    }
    public void PlaySFXClip(AudioClip clip, Transform spawnTransform, float volume)
    {
        AudioSource audioSource = Instantiate(SFXprefab, spawnTransform.position, Quaternion.identity);

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();

        float clipLength = audioSource.clip.length;
    }
    public void PlayRandomSFXClip(AudioClip[] clip, Transform spawnTransform, float volume)
    {
        int rand = Random.Range(0, clip.Length);

        AudioSource audioSource = Instantiate(SFXprefab, spawnTransform.position, Quaternion.identity);

        audioSource.clip = clip[rand];
        audioSource.volume = volume;
        audioSource.Play();

        float clipLength = audioSource.clip.length;
    }
}
