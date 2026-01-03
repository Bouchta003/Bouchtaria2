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
        yield return new WaitForSeconds(0.3f);

        // Spells
        yield return StartCoroutine(TryPlaySpells());

        // Summons
        yield return StartCoroutine(TrySummon());

        // Attacks
        yield return StartCoroutine(TryAttack());
        yield return StartCoroutine(TryAttack());

        //Summon if new place
        yield return StartCoroutine(TrySummon());

        yield return new WaitForSeconds(0.2f);

        EndEnemyTurn();
    }


    private IEnumerator TryPlaySpells()
    {
        if (enemyHand.handCards.Count == 0)
            yield break;

        List<CardInstance> playableSpells = new();

        foreach (GameObject cardGO in enemyHand.handCards)
        {
            CardInstance inst = cardGO.GetComponent<CardInstance>();
            if (inst == null)
                continue;

            if (inst.Data.cardType.ToLower() != "spell")
                continue;
            
            if (inst.CurrentEffect.Contains("monsterpart"))
                continue;

            if (inst.CurrentManaCost > gameManager.EnemyCurrentMana)
                continue;

            playableSpells.Add(inst);
        }

        playableSpells.Sort((a, b) =>
            b.CurrentManaCost.CompareTo(a.CurrentManaCost));

        foreach (CardInstance spell in playableSpells)
        {
            if (spell == null)
                continue;

            if (spell.CurrentManaCost > gameManager.EnemyCurrentMana)
                break;

            yield return StartCoroutine(
                gameManager.ShowEnemySpell(spell.Data)
            );

            PlaySpell(spell);


            // 🔹 WAIT between spells
            yield return new WaitForSeconds(0.35f);
        }
    }

    private void PlaySpell(CardInstance spell)
    {
        //Verify if there is board before playing buff spell :
        if (spell.CurrentEffect.Contains("gear") || (spell.CurrentEffect.Contains("heal")&& !spell.CurrentEffect.Contains("autoheal")) || spell.CurrentEffect.Contains("buff"))
        {
            if (enemyBoard.enemyPrefabCards.Count <= 0) return;
        }

            // Spend mana
        gameManager.UseMana(spell.CurrentManaCost, PlayerOwner.Enemy);

        // Trigger deploy effects (spells use deploy)
        spell.OnPlaySpell();

        // Remove from hand & destroy
        enemyHand.handCards.Remove(spell.gameObject);
        Destroy(spell.gameObject);
    }
    private IEnumerator TrySummon()
    {
        while (true)
        {
            if (enemyBoard.enemyPrefabCards.Count >=enemyBoard.maxBoardSize)
                yield break;
            CardInstance card = GetBestPlayableMinion();
            if (card == null)
                yield break;
            Summon(card.GetComponent<Card>());

            // 🔹 WAIT between summons
            yield return new WaitForSeconds(0.35f);
        }
    }
    CardInstance GetBestPlayableMinion()
    {
        int availableMana = gameManager.EnemyCurrentMana;
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
        if(playable.Count>0 && playable != null)
        {
            playable.Sort((b, a) => b.CurrentManaCost.CompareTo(a.CurrentManaCost));
            return playable[0];
        }
        return null;
    }
    private void Summon(Card card)
    {
        enemyBoard.OnCardDrop(card);
    }
    private IEnumerator TryAttack()
    {
        List<CardInstance> attackers = new();

        foreach (GameObject go in enemyBoard.enemyPrefabCards)
        {
            if (go == null || !go.activeSelf)
                continue;

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
            yield break;

        // Strongest first (optional)
        attackers.Sort((a, b) => b.CurrentAttack.CompareTo(a.CurrentAttack));

        foreach (CardInstance attacker in attackers)
        {
            if (attacker == null || (attacker.HasAttackedThisTurn && !attacker.HasKeyword("haste")) ||
            (attacker.HasAttackedThisTurn && attacker.HasKeyword("haste") && attacker.HasAttackedTwiceThisTurn)
                || attacker.CurrentAttack<=0)
                continue;

            var targets = gameManager.GetValidTargets(attacker);
            if (targets.Count == 0)
                continue;

            gameManager.QueueAttack(attacker, targets[0]);

            // 🔹 Wait until THIS attack finishes
            yield return new WaitUntil(() => !gameManager.IsResolvingAttackQueue());

            // 🔹 Small delay for readability
            yield return new WaitForSeconds(0.25f);
        }
    }
    private void EndEnemyTurn()
    {
        TurnManager.Instance.EndTurn();
    }

}
