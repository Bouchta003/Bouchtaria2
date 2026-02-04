using UnityEngine;

public class DamageVFXManager : MonoBehaviour
{
    public static DamageVFXManager Instance { get; private set; }

    [Header("Particle impact prefabs")]
    public GameObject[] hitEffects;
    [Header("UI Root (for core & UI hits)")]
    public RectTransform uiVfxRootAlly;
    public RectTransform uiVfxRootEnemy;
    [Header("UI VFX Scale")]
    public float uiScaleMultiplier = 60f;
    public string uiSortingLayer = "UI";
    public int uiSortingOrder = 500;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void PlayRandomHit(Vector3 position)
    {
        if (hitEffects == null || hitEffects.Length == 0)
            return;

        GameObject go =
            Instantiate(hitEffects[Random.Range(0, hitEffects.Length)],
                        position,
                        Quaternion.identity);

        ForceFrontLayer(go);
        AutoDestroy(go);
    }
    public void PlayRandomHitOnCore(RectTransform parent)
    {
        if (parent == null || hitEffects.Length == 0)
            return;

        GameObject go =
            Instantiate(hitEffects[Random.Range(0, hitEffects.Length)]);

        // 🔒 PARENT FIRST
        go.transform.SetParent(parent, false);

        // 🔒 FORCE POSITION
        go.transform.localPosition = Vector3.zero;

        // 🔒 FORCE ROTATION
        go.transform.localRotation = Quaternion.identity;

        // 🔒 FORCE SCALE
        go.transform.localScale = Vector3.one * uiScaleMultiplier;

        ForceUISorting(go);
        AutoDestroy(go);
    }
    void ForceUISorting(GameObject vfx)
    {
        var renderers = vfx.GetComponentsInChildren<ParticleSystemRenderer>();

        foreach (var r in renderers)
        {
            r.sortingLayerName = uiSortingLayer;
            r.sortingOrder = uiSortingOrder;
            r.renderMode = ParticleSystemRenderMode.Billboard;
        }
    }

    void ForceFrontLayer(GameObject vfx)
    {
        var renderers = vfx.GetComponentsInChildren<ParticleSystemRenderer>();

        foreach (var r in renderers)
        {
            r.sortingLayerName = "Effects";
            r.sortingOrder = 999;
        }
    }
    void AutoDestroy(GameObject vfx)
    {
        var systems = vfx.GetComponentsInChildren<ParticleSystem>();

        float longestLifetime = 0f;

        foreach (var ps in systems)
        {
            var main = ps.main;
            float lifetime = main.duration + main.startLifetime.constantMax;
            if (lifetime > longestLifetime)
                longestLifetime = lifetime;
        }

        Destroy(vfx, longestLifetime + 0.2f);
    }

}
