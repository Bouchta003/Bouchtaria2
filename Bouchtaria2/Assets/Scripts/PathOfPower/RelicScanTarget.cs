using UnityEngine;
using UnityEngine.EventSystems;

public class RelicScanTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RelicDefinition relicDefinition;
    private bool isHovered;
    private bool scanVisible;

    public void Initialize(RelicDefinition definition)
    {
        relicDefinition = definition;
        scanVisible = false;
    }

    private void Update()
    {
        if (!isHovered)
            return;

        if (IsScanActive())
        {
            if (!scanVisible)
                ShowRelicScan();
        }
        else if (scanVisible)
        {
            HideRelicScan();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (IsScanActive())
            ShowRelicScan();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        HideRelicScan();
    }

    private void ShowRelicScan()
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

        scanVisible = true;
    }

    private void HideRelicScan()
    {
        if (ScanController.Instance != null && ScanController.Instance.panelInstance != null)
            ScanController.Instance.panelInstance.Hide();

        scanVisible = false;
    }

    private bool IsScanActive()
    {
        if (ScanInput.Instance != null)
            return ScanInput.Instance.IsScanActive;

        return !UIInputFocusTracker.IsAnyTMPFocused && Input.GetKey(KeyCode.Space);
    }
}
