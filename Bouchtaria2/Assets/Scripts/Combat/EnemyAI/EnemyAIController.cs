using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAIController : MonoBehaviour
{
    [SerializeField] private HandManager enemyHand;
    [SerializeField] private EnemyCardDropArea enemyBoard;
    [SerializeField] private GameManager gameManager;
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
        // Small delay at start of enemy turn (readability)
        yield return new WaitForSeconds(0.3f);

        TrySummon();

        // Pause so summon is readable
        yield return new WaitForSeconds(0.3f);

        TryAttack();

        // 🔴 IMPORTANT: wait until all attack animations are done
        yield return new WaitUntil(() => !gameManager.IsResolvingAttackQueue());

        // Small delay before ending turn
        yield return new WaitForSeconds(0.2f);

        EndEnemyTurn();
    }
    private void TrySummon()
    {
        if (enemyHand.handCards.Count == 0)
            return;

        int availableMana = gameManager.EnemyCurrentMana;

        if (enemyBoard.enemyPrefabCards.Count >= enemyBoard.maxBoardSize)
            return;

        // Collect playable minions
        List<CardInstance> playable = new();

        foreach (GameObject cardGO in enemyHand.handCards)
        {
            CardInstance inst = cardGO.GetComponent<CardInstance>();
            if (inst == null)
                continue;

            if (inst.Data.cardType.ToLower() != "minion")
                continue;

            if (inst.CurrentManaCost > availableMana)
                continue;

            playable.Add(inst);
        }

        // Sort by cheapest first (maximize mana usage)
        playable.Sort((a, b) => a.CurrentManaCost.CompareTo(b.CurrentManaCost));

        // Greedy summon loop
        foreach (CardInstance inst in playable)
        {
            if (enemyBoard.enemyPrefabCards.Count >= enemyBoard.maxBoardSize)
                break;

            if (inst.CurrentManaCost > gameManager.EnemyCurrentMana)
                continue;

            Card card = inst.GetComponent<Card>();
            enemyBoard.OnCardDrop(card);

            // small readability delay handled by EnemyTurnRoutine
        }
    }
    private void TryAttack()
    {
        List<CardInstance> attackers = new();

        foreach (GameObject go in enemyBoard.enemyPrefabCards)
        {
            CardInstance instance = go.GetComponent<CardInstance>();
            if (instance == null)
                continue;
            if (instance.CurrentAttack <= 0)
                continue;

            if (!gameManager.CanSelectAttacker(instance))
                continue;

            attackers.Add(instance);
        }

        if (attackers.Count == 0)
            return;

        // Optional: sort attackers (biggest first = more pressure)
        attackers.Sort((a, b) => b.CurrentAttack.CompareTo(a.CurrentAttack));

        foreach (CardInstance attacker in attackers)
        {
            AttackWith(attacker);
        }
    }

    private void AttackWith(CardInstance attacker)
    {
        var targets = gameManager.GetValidTargets(attacker);

        if (targets.Count == 0)
            return;

        var target = targets[0]; // Only attack first target

        gameManager.QueueAttack(attacker, target);

    }
    private void EndEnemyTurn()
    {
        TurnManager.Instance.EndTurn();
    }

}
