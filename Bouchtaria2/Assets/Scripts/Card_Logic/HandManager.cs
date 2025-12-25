using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Rendering;


public class HandManager : MonoBehaviour
{
    [SerializeField] int maxHandSize;

    [SerializeField] GameObject cardPrefab;
    [SerializeField] SplineContainer splineContainer;
    [SerializeField] Transform spawnPoint;

    [SerializeField] int baseSortingOrder = 100;
    public List<GameObject> handCards = new();
    void Start()
    {

    }
    
    public void RemoveCardFromHand(GameObject cardToRemove)
    {
        if (handCards.Remove(cardToRemove))
        {
            UpdateCardPositions();
        }
    }

    public void UpdateCardPositions()
    {
        if (handCards.Count == 0) return;

        float cardSpacing = 1f / maxHandSize;
        float firstCardPosition = 0.5f - (handCards.Count - 1) * cardSpacing / 2;
        Spline spline = splineContainer.Spline;

        for (int i = 0; i < handCards.Count; i++)
        {
            float p = firstCardPosition + i * cardSpacing;

            Vector3 splinePosition = spline.EvaluatePosition(p);
            Vector3 forward = spline.EvaluateTangent(p);
            Vector3 up = spline.EvaluateUpVector(p);

            // 🔒 ORIGINAL ROTATION (unchanged)
            Quaternion rotation = Quaternion.LookRotation(
                up,
                Vector3.Cross(up, forward).normalized
            );

            GameObject card = handCards[i];

            card.transform.DOMove(splinePosition, 0.25f);
            card.transform.DOLocalRotateQuaternion(rotation, 0.25f);

            // 🎨 Sorting polish
            SortingGroup group = card.GetComponent<SortingGroup>();
            if (group != null)
            {
                group.sortingOrder = baseSortingOrder + i;
            }
        }
    }

    // =========================
    // POLISH HOOKS (CALLED BY CARD)
    // =========================

    public void RaiseCard(GameObject card, int bonus = 100)
    {
        SortingGroup group = card.GetComponent<SortingGroup>();
        if (group != null)
        {
            group.sortingOrder = baseSortingOrder + maxHandSize + bonus;
        }
    }

    public void RestoreCardOrder()
    {
        UpdateCardPositions();
    }


}
