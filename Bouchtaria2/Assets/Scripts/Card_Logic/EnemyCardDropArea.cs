using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;

public class EnemyCardDropArea : MonoBehaviour, ICardDropArea
{
    [SerializeField] GameObject compactPrefab;
    [SerializeField] GameObject GameManager;
    [SerializeField] HandManager handManager;
    [SerializeField] SplineContainer enemyBoardSpline;

    GameManager gm;
    public int maxBoardSize = 6;

    public List<GameObject> enemyPrefabCards = new List<GameObject>();
    private void Start()
    {
        gm = GameManager.GetComponent<GameManager>();
    }
    public void OnCardDrop(Card card)
    {
        //Verify Mana Legality 
        if (card.gameObject.GetComponent<CardInstance>().CurrentManaCost > gm.CurrentMana ||
            card.gameObject.GetComponent<CardInstance>().Data.cardType.ToLower() == "spell")
        {
            card.ResetCard();
            return;
        }        
        //Verify board space Legality
        if (enemyPrefabCards.Count >= maxBoardSize) return;


        // ----- Card is legal -----

        //Remove card from hand
        handManager.RemoveCardFromHand(card.gameObject);

        //Use Mana
        gm.UseMana(card.gameObject.GetComponent<CardInstance>().CurrentManaCost);

        //Instantiate card compact instead on board
        card.gameObject.GetComponent<CardInstance>().SetZone(CardZone.Board);
        card.gameObject.GetComponent<CardInstance>().Owner = PlayerOwner.Enemy;
        card.gameObject.GetComponent<CardView>().UpdateMode();

        //Add to list of ally cards
        enemyPrefabCards.Add(card.gameObject);
        UpdateEnemyCardPositions();

        Debug.Log("Card dropped in ally slot");
    }
    public void HandleEnemyDeath(CardInstance instance)
    {
        GameObject cardGO = instance.gameObject;

        if (!enemyPrefabCards.Contains(cardGO))
            return;

        enemyPrefabCards.Remove(cardGO);

        Destroy(cardGO);

        UpdateEnemyCardPositions();
    }

    public void UpdateEnemyCardPositions()
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
