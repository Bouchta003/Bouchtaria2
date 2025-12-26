using TMPro;
using UnityEngine;

public class CoreView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI shieldText;

    private CoreInstance core;
    public void Bind(CoreInstance instance)
    {
        core = instance;
        core.OnCoreChanged += Refresh;
        Refresh();
    }
    private void OnDestroy()
    {
        core.OnCoreChanged -= Refresh;
    }
    public void Refresh()
    {
        if (core == null) return;

        healthText.text = $"{core.CurrentHealth}/{core.MaxHealth}";

        shieldText.gameObject.SetActive(core.Shield > 0);
        shieldText.text = core.Shield.ToString();
    }
}
