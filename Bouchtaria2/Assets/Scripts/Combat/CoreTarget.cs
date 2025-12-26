using UnityEngine;
using UnityEngine.EventSystems;

public class CoreTarget : MonoBehaviour, IPointerClickHandler
{
    private CoreInstance core;

    private void Awake()
    {
        core = GetComponentInChildren<CoreInstance>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm == null)
            return;

        gm.TryAttackCore(core);
    }
}
