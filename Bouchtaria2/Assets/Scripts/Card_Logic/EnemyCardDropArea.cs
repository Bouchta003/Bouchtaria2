using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;

public class EnemyCardDropArea : MonoBehaviour, ICardDropArea
{
    [SerializeField] GameObject compactPrefab;
    [SerializeField] HandManager handManager;
    [SerializeField] SplineContainer enemyBoardSpline;

    public List<GameObject> enemyPrefabCards = new List<GameObject>();

    public int maxBoardSize = 6;
    public void OnCardDrop(Card card)
    {
        if (enemyPrefabCards.Count >= maxBoardSize) return;

        //Remove card from hand
        handManager.RemoveCardFromHand(card.gameObject);
        card.gameObject.SetActive(false);

        //Instantiate card compact instead on board
        GameObject prefabOnBoard = Instantiate(compactPrefab, transform.position, Quaternion.Euler(0, 0, 180));
        enemyPrefabCards.Add(prefabOnBoard);
        Debug.Log("Card dropped in enemy slot");
        UpdateEnemyyCardPositions();
    }
    public void UpdateEnemyyCardPositions()
    {
        if (enemyPrefabCards.Count == 0) return;

        float cardSpacing = (1f / maxBoardSize) + 0.1f / enemyPrefabCards.Count;
        float firstCardPosition = 0.5f - (enemyPrefabCards.Count - 1) * cardSpacing / 2;

        Spline spline = enemyBoardSpline.Spline;

        for (int i = 0; i < enemyPrefabCards.Count; i++)
        {
            float p = firstCardPosition + i * cardSpacing;

            // Convert spline local position → world position
            Vector3 worldPos = enemyBoardSpline.transform.TransformPoint(
                spline.EvaluatePosition(p)
            );

            // Get tangent (direction along spline)
            Vector3 forward = spline.EvaluateTangent(p);

            // 2D rotation angle
            float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

            enemyPrefabCards[i].transform.DOMove(worldPos, 0.25f);
            enemyPrefabCards[i].transform.DORotate(
                new Vector3(0, 0, angle),
                0.25f
            );
        }
    }
}
