using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;

public class EnemyCardDropArea : MonoBehaviour, ICardDropArea
{
    [SerializeField] GameObject GameManager;
    [SerializeField] HandManager handManager;
    [SerializeField] SplineContainer enemyBoardSpline;

    public PlayerOwner Owner => PlayerOwner.Enemy;

    private GameManager gm;
    public int maxBoardSize = 6;

    public List<GameObject> enemyPrefabCards = new List<GameObject>();
    public event System.Action<CardInstance> OnCardPlayed;

    private void Start()
    {
        gm = GameManager.GetComponent<GameManager>();
    }

    public void OnCardDrop(Card card)
    {
        CardInstance cardInst = card.GetComponent<CardInstance>();

        // Mana / legality
        if (cardInst.CurrentManaCost > gm.EnemyCurrentMana ||
            cardInst.Data.cardType.ToLower() == "spell")
        {
            card.ResetCard();
            return;
        }

        if (enemyPrefabCards.Count >= maxBoardSize)
            return;

        // Remove from hand
        handManager.RemoveCardFromHand(card.gameObject);

        // Use mana
        gm.UseMana(cardInst.CurrentManaCost, PlayerOwner.Enemy);

        // Board setup
        cardInst.SetZone(CardZone.Board);
        cardInst.Owner = PlayerOwner.Enemy;

        if (cardInst.HasKeyword("quickstrike") || cardInst.HasKeyword("charge"))
            cardInst.IsSummoningSick = false;
        else
            cardInst.IsSummoningSick = true;

        cardInst.OnEnterBoard();

        OnCardPlayed?.Invoke(cardInst);

        // Switch to compact board view
        card.GetComponent<CardView>().UpdateMode();

        enemyPrefabCards.Add(card.gameObject);
        UpdateEnemyCardPositions();
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
    }

    private void OnDisable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted -= HandleTurnStart;
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
        // ❗ Do not reflow during attack animations
        if (gm != null && gm.IsResolvingAttackQueue())
            return;

        if (enemyPrefabCards.Count == 0)
            return;

        float cardSpacing = (1f / maxBoardSize) + 0.1f / enemyPrefabCards.Count;
        float firstCardPosition = 0.5f - (enemyPrefabCards.Count - 1) * cardSpacing / 2;

        Spline spline = enemyBoardSpline.Spline;

        for (int i = 0; i < enemyPrefabCards.Count; i++)
        {
            float p = firstCardPosition + i * cardSpacing;

            Vector3 worldPos = enemyBoardSpline.transform.TransformPoint(
                spline.EvaluatePosition(p)
            );

            Vector3 forward = spline.EvaluateTangent(p);
            float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

            GameObject cardGO = enemyPrefabCards[i];
            CardView view = cardGO.GetComponent<CardView>();

            // Kill previous layout tweens
            cardGO.transform.DOKill();

            cardGO.transform.DOMove(worldPos, 0.25f)
                .OnComplete(() =>
                {
                    if (view != null)
                        view.BoardPosition = view.transform.position;
                });

            cardGO.transform.DORotate(
                new Vector3(0, 0, angle),
                0.25f
            );
        }
    }
}
