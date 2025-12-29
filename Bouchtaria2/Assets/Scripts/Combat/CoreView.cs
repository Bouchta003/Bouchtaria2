using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoreView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI shieldText;

    [SerializeField] private RectTransform coreUI;
    [SerializeField] private Image coreImage;

    private Color baseColor;

    private void Awake()
    {
        if (coreImage != null)
            baseColor = coreImage.color;
    }

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
    public IEnumerator PlayHitReaction(int damage)
    {
        // UI flash
        if (coreImage != null)
        {
            coreImage.DOColor(Color.white, 0.05f)
                     .SetLoops(2, LoopType.Yoyo)
                     .OnComplete(() => coreImage.color = baseColor);
        }

        // Camera shake

        GameManager gm = FindFirstObjectByType<GameManager>();
        gm.ShakeCameraForDamage(damage);

        yield return null;
    }

    public void Refresh()
    {
        if (core == null) return;

        healthText.text = $"{core.CurrentHealth}/{core.MaxHealth}";

        shieldText.gameObject.SetActive(core.Shield > 0);
        shieldText.text = core.Shield.ToString();
    }
}
