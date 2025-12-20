using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using DG.Tweening;

public class HandManager : MonoBehaviour
{
    [SerializeField] int maxHandSize;

    [SerializeField] GameObject cardPrefab;
    [SerializeField] SplineContainer splineContainer;
    [SerializeField] Transform spawnPoint;

    List<GameObject> handCards = new();
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) DrawCard(-1);
    }
    private void DrawCard(int cardId)
    {
        GameObject prefab = new GameObject();
        if (cardId == -1) prefab = cardPrefab;
        if (handCards.Count >= maxHandSize) return;
        GameObject g = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        handCards.Add(g);
        UpdateCardPositions();
    }
    private void UpdateCardPositions()
    {
        if (handCards.Count == 0) return;
        float cardSpacing = 1f / maxHandSize;
        float firstCardPosition = 0.5f - (handCards.Count-1) * cardSpacing/2;
        Spline spline = splineContainer.Spline;

        for(int i = 0; i < handCards.Count; i++)
        {
            float p = firstCardPosition + i * cardSpacing;
            Vector3 splinePosition = spline.EvaluatePosition(p);
            Vector3 forward = spline.EvaluateTangent(p);
            Vector3 up = spline.EvaluateUpVector(p);
            Quaternion rotation = Quaternion.LookRotation(up, Vector3.Cross(up, forward).normalized);
            handCards[i].transform.DOMove(splinePosition, 0.25f);
            handCards[i].transform.DOLocalRotateQuaternion(rotation, 0.25f);
        }
    }
}
