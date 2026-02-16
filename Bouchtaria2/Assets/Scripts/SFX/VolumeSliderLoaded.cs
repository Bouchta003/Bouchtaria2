using UnityEngine;
using UnityEngine.UI;

public class VolumeSliderLoader : MonoBehaviour
{
    [SerializeField] private string playerPrefKey;
    [SerializeField] private Slider slider;

    private void Start()
    {
        slider.value = PlayerPrefs.GetFloat(playerPrefKey, 1f);
    }
}
