using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAIController : MonoBehaviour
{
    [SerializeField] private HandManager enemyHand;
    [SerializeField] private EnemyCardDropArea enemyBoard;
    [SerializeField] private GameManager gameManager;
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

        StartCoroutine(EnemyTurnRoutine());
    }
    private IEnumerator EnemyTurnRoutine()
    {
        yield return null; // wait until TurnPhase.Main

        TrySummon();
        TryAttack();
        EndEnemyTurn();
    }

    private void TryAttack()
    {
        List<CardInstance> attackers = new();

        foreach (GameObject go in enemyBoard.enemyPrefabCards)
        {
            CardInstance instance = go.GetComponent<CardInstance>();
            if (instance == null)
                continue;

            if (!gameManager.CanSelectAttacker(instance))
                continue;

            attackers.Add(instance);
        }

        if (attackers.Count == 0)
            return;

        CardInstance attacker = attackers[0]; // Only attack with first attacker
        AttackWith(attacker);
    }
    private void AttackWith(CardInstance attacker)
    {
        var targets = gameManager.GetValidTargets(attacker);

        if (targets.Count == 0)
            return;

        var target = targets[0]; // Only attack first target

        gameManager.ResolveAttack(attacker, target);
    }
    private void EndEnemyTurn()
    {
        TurnManager.Instance.EndTurn();
    }

}
