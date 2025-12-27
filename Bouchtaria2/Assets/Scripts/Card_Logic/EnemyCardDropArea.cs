using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;

public class EnemyCardDropArea : MonoBehaviour, ICardDropArea
{
    [SerializeField] GameObject GameManager;
    [SerializeField] HandManager handManager;
    [SerializeField] AllyCardDropArea allyCardDropArea;
    [SerializeField] SplineContainer enemyBoardSpline;
    public PlayerOwner Owner => PlayerOwner.Player;
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
        CardInstance cardInst = card.gameObject.GetComponent<CardInstance>();
        cardInst.SetZone(CardZone.Board);
        cardInst.Owner = PlayerOwner.Enemy;
        if (cardInst.HasKeyword("quickstrike") || cardInst.HasKeyword("charge"))
            cardInst.IsSummoningSick = false;
        else
            cardInst.IsSummoningSick = true;
        cardInst.OnEnterBoard();

        card.gameObject.GetComponent<CardView>().UpdateMode();

        //Add to list of ally cards
        enemyPrefabCards.Add(card.gameObject);
        UpdateEnemyCardPositions();

        Debug.Log("Card dropped in ally slot");
    }
    public List<GameObject> GetCards()
    {
        return enemyPrefabCards;
    }
    public bool HasProtectUnits()
    {
        foreach (GameObject cardGO in enemyPrefabCards)
        {
            CardInstance instance = cardGO.GetComponent<CardInstance>();
            if (instance != null && instance.HasKeyword("protect"))
                return true;
        }
        return false;
    }
    private void OnEnable()
    {
        TurnManager.Instance.OnTurnStarted += HandleTurnStart;
        TurnManager.Instance.OnTurnEnded += HandleTurnEnd;
    }
    private void OnDisable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted -= HandleTurnStart;
            TurnManager.Instance.OnTurnEnded -= HandleTurnEnd;
        }
    }
    private void HandleTurnStart(PlayerOwner owner)
    {
        if (owner != PlayerOwner.Enemy)
            return;

        foreach (var cardGO in enemyPrefabCards)
        {
            var instance = cardGO.GetComponent<CardInstance>();
            instance.OnTurnStart();
        }
    }
    private void HandleTurnEnd(PlayerOwner owner)
    {
        // Only trigger for the owner of THIS board
        if (owner != PlayerOwner.Enemy)
            return;

        //TriggerGunners(1);
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
