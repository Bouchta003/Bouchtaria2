using UnityEngine;

public class EnemyAIController : MonoBehaviour
{
    [SerializeField] private HandManager enemyHand;
    [SerializeField] private EnemyCardDropArea enemyBoard;

    private void TrySummon()
    {
        if (enemyHand.handCards.Count == 0)
            return;

        if (enemyBoard.enemyPrefabCards.Count >= enemyBoard.maxBoardSize)
            return;

        // Pick the first card (dumb AI)
        GameObject cardGO = enemyHand.handCards[0];
        Card card = cardGO.GetComponent<Card>();

        if (card == null)
            return;

        enemyBoard.OnCardDrop(card);enemyBoard.UpdateEnemyCardPositions();
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

        TrySummon();
    }

}
