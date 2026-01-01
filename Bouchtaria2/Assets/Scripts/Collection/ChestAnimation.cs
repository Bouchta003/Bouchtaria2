using System.Collections;
using UnityEngine;

public class ChestAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform chestCap;

    [Header("Animation Settings")]
    [SerializeField] private float openHeight = 0.6f;
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float floatAmplitude = 0.05f;
    [SerializeField] private float floatSpeed = 6f;

    private Vector3 closedLocalPos;
    private Coroutine animRoutine;
    private bool isOpen;
    Card hoveringCard;
    bool isTriggered = false;
    private void Awake()
    {
        closedLocalPos = chestCap.localPosition;
    }
    private void Update()
    {
        if(!isOpen && isTriggered)
        {
            OpenChest();
        }
        if(isOpen && !isTriggered)
        {
            CloseChest();
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        Card card = other.GetComponent<Card>();
        if (card == null)
            return;

        if (!card.isDragging)
            return;

        hoveringCard = card;
        isTriggered = true;
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        Card card = other.GetComponent<Card>();
        if (card == hoveringCard)
        {
            hoveringCard = null;
            isTriggered = false;
        }
    }
    public Card GetHoveringCard()
    {
        return hoveringCard;
    }

    public bool IsTriggered()
    {
        return isTriggered;
    }
    public void OpenChest()
    {
        if (isOpen) return;
        StartAnimation(true);
    }

    public void CloseChest()
    {
        if (!isOpen) return;
        StartAnimation(false);
    }

    public void ToggleChest()
    {
        StartAnimation(!isOpen);
    }

    private void StartAnimation(bool open)
    {
        if (animRoutine != null)
            StopCoroutine(animRoutine);

        animRoutine = StartCoroutine(AnimateCap(open));
    }

    private IEnumerator AnimateCap(bool open)
    {
        isOpen = open;

        Vector3 start = chestCap.localPosition;
        Vector3 end = closedLocalPos +
                      (open ? Vector3.up * openHeight : Vector3.zero);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float eased = EaseOutCubic(t);

            Vector3 pos = Vector3.Lerp(start, end, eased);

            // Magical floating effect
            pos.y += Mathf.Sin(Time.time * floatSpeed) * floatAmplitude * eased;

            chestCap.localPosition = pos;
            yield return null;
        }

        chestCap.localPosition = end;
        animRoutine = null;
    }

    private float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }
}
