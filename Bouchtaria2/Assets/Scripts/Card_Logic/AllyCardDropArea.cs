using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;
using DG.Tweening;

public class AllyCardDropArea : MonoBehaviour, ICardDropArea
{
    [SerializeField] GameObject compactPrefab;
    [SerializeField] HandManager handManager;
    [SerializeField] SplineContainer allyBoardSpline;

    public int maxBoardSize = 6;

    public List<GameObject> allyPrefabCards = new List<GameObject>();
    public void OnCardDrop(Card card)
    {
        if (allyPrefabCards.Count >= maxBoardSize) return;

        //Remove card from hand
        handManager.RemoveCardFromHand(card.gameObject);
        card.gameObject.SetActive(false);

        //Instantiate card compact instead on board
        GameObject prefabOnBoard = Instantiate(compactPrefab, transform.position, Quaternion.identity);
        allyPrefabCards.Add(prefabOnBoard);
        UpdateAllyCardPositions();
        Debug.Log("Card dropped in ally slot");
    }
    public void UpdateAllyCardPositions()
    {
        if (allyPrefabCards.Count == 0) return;

        float cardSpacing = (1f / maxBoardSize)  + 0.1f/allyPrefabCards.Count;
        float firstCardPosition = 0.5f - (allyPrefabCards.Count - 1) * cardSpacing / 2;

        Spline spline = allyBoardSpline.Spline;

        for (int i = 0; i < allyPrefabCards.Count; i++)
        {
            float p = firstCardPosition + i * cardSpacing;

            // Convert spline local position → world position
            Vector3 worldPos = allyBoardSpline.transform.TransformPoint(
                spline.EvaluatePosition(p)
            );

            // Get tangent (direction along spline)
            Vector3 forward = spline.EvaluateTangent(p);

            // 2D rotation angle
            float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

            allyPrefabCards[i].transform.DOMove(worldPos, 0.25f);
            allyPrefabCards[i].transform.DORotate(
                new Vector3(0, 0, angle),
                0.25f
            );
        }
    }
}
