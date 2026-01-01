using System.Collections;
using UnityEngine;

public class LockOverlayAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform lockSprite;
    [SerializeField] private Transform greyOverlay;

    [Header("Animation")]
    [SerializeField] private float lockFallSpeed = 6f;
    [SerializeField] private float overlayFallSpeed = 4f;
    [SerializeField] private float fallDistance = 30f;
    [SerializeField] private float delayBetween = 0.15f;

    private Vector3 lockStartPos;
    private Vector3 overlayStartPos;

    private void Awake()
    {
        lockStartPos = lockSprite.localPosition;
        overlayStartPos = greyOverlay.localPosition;
    }

    public void PlayUnlockAnimation()
    {
        StartCoroutine(UnlockRoutine());
    }

    private IEnumerator UnlockRoutine()
    {
        // 1️⃣ Lock falls
        yield return StartCoroutine(Fall(lockSprite, lockFallSpeed));

        // 2️⃣ Small delay for readability
        yield return new WaitForSeconds(delayBetween);

        // 3️⃣ Grey overlay falls
        yield return StartCoroutine(Fall(greyOverlay, overlayFallSpeed));

        // 4️⃣ Disable both
        lockSprite.gameObject.SetActive(false);
        greyOverlay.gameObject.SetActive(false);
    }

    private IEnumerator Fall(Transform target, float speed)
    {
        Vector3 startPos = target.localPosition;
        Vector3 endPos = startPos + Vector3.down * fallDistance;

        while (Vector3.Distance(target.localPosition, endPos) > 0.01f)
        {
            target.localPosition =
                Vector3.MoveTowards(target.localPosition, endPos, speed * Time.deltaTime);
            yield return null;
        }

        target.localPosition = endPos;
    }
}
