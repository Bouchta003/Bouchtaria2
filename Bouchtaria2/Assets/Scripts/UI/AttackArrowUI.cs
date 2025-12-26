using UnityEngine;
using UnityEngine.UI;

public class AttackArrowUI : MonoBehaviour
{
    [SerializeField] private RectTransform arrowRect;
    [SerializeField] private float arrowThickness = 12f;

    private bool isActive;
    private Vector2 startScreenPos;

    void Awake()
    {
        arrowRect.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isActive)
            return;

        UpdateArrow();
    }

    public void BeginArrow(Vector2 attackerScreenPosition)
    {
        startScreenPos = attackerScreenPosition;
        isActive = true;
        arrowRect.gameObject.SetActive(true);
    }

    public void EndArrow()
    {
        isActive = false;
        arrowRect.gameObject.SetActive(false);
    }

    private void UpdateArrow()
    {
        Vector2 mousePos = Input.mousePosition;
        Vector2 direction = mousePos - startScreenPos;

        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        arrowRect.position = startScreenPos;
        arrowRect.sizeDelta = new Vector2(distance, arrowThickness);
        arrowRect.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
