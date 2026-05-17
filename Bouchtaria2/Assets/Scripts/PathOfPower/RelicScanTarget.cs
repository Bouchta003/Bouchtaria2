using UnityEngine;
using UnityEngine.EventSystems;

public class RelicScanTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RelicDefinition relicDefinition;

    public void Initialize(RelicDefinition definition)
    {
        relicDefinition = definition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowRelicScan();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ScanController.Instance != null && ScanController.Instance.panelInstance != null)
            ScanController.Instance.panelInstance.Hide();
    }

    public void ShowRelicScan()
    {
        if (relicDefinition == null)
            return;

        if (PathOfPowerManager.Instance != null)
            PathOfPowerManager.Instance.ShowRelicInScanPanel(relicDefinition);
        else if (ScanController.Instance != null && ScanController.Instance.panelInstance != null)
        {
            ScanController.Instance.panelInstance.PopulateRelic(new Relic(relicDefinition));
            ScanController.Instance.panelInstance.Slide(true);
        }
    }
}
