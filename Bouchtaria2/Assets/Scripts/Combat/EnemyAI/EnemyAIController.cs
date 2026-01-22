using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAIController : MonoBehaviour
{
    [SerializeField] private HandManager enemyHand;
    [SerializeField] private EnemyCardDropArea enemyBoard;
    [SerializeField] private AllyCardDropArea allyBoard;
    [SerializeField] private GameManager gameManager;
    public event System.Action<CardInstance> OnCardPlayed;
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
        if (HasLethalThisTurn())
        {
            yield return StartCoroutine(TryAttack());
            EndEnemyTurn();
            yield break;
        }

        yield return new WaitForSeconds(0.3f);

        // Spells
        if(!gameManager.DistortionWorld)
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
    private int EvaluateSpell(CardInstance spell)
    {
        int score = 0;

        if (spell.CurrentEffect.Contains("damage"))
            score += 40;

        if (spell.CurrentEffect.Contains("buff"))
            score += enemyBoard.enemyPrefabCards.Count * 10;

        if (spell.CurrentEffect.Contains("heal"))
            score += 20;

        if (spell.CurrentEffect.Contains("gear"))
            score += 30;

        // Penalize low-impact expensive spells
        score -= spell.CurrentManaCost * 5;

        return score;
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
            EvaluateSpell(b).CompareTo(EvaluateSpell(a)));

        foreach (CardInstance spell in playableSpells)
        {
            if (spell == null)
                continue;

            if (spell.CurrentManaCost > gameManager.EnemyCurrentMana)
                break;

            if (spell.CurrentEffect.Contains("gear") || (spell.CurrentEffect.Contains("heal") && !spell.CurrentEffect.Contains("autoheal")) || spell.CurrentEffect.Contains("buff"))
            {
                if (enemyBoard.enemyPrefabCards.Count <= 0) continue;
            }
            else if (spell.CurrentEffect.Contains("targetunit"))
            {
                if (allyBoard.allyPrefabCards.Count <= 0) continue;
            }
            // 🔑 HARD VALIDATION — no ghost spells
            if (!CanEnemyActuallyCastSpell(spell))
                continue;

            // Only NOW show the spell
            yield return StartCoroutine(
                gameManager.ShowEnemySpell(spell.Data)
            );

            // Commit immediately after showing
            PlaySpell(spell);

            // Small delay for readability
            yield return new WaitForSeconds(0.35f);

            // 🔹 WAIT between spells
            yield return new WaitForSeconds(0.35f);
        }
    }
    private bool CanEnemyActuallyCastSpell(CardInstance spell)
    {
        if (spell == null)
            return false;

        // Mana check (already mostly done, but keep it strict)
        if (spell.CurrentManaCost > gameManager.EnemyCurrentMana)
            return false;

        // Empty / invalid effect
        if (string.IsNullOrWhiteSpace(spell.CurrentEffect))
            return false;

        // Distortion World blocks spells entirely
        if (gameManager.DistortionWorld)
            return false;

        // Buff / Gear / non-auto Heal → requires enemy board
        if (
            spell.CurrentEffect.Contains("gear") ||
            spell.CurrentEffect.Contains("buff") ||
            (spell.CurrentEffect.Contains("heal") && !spell.CurrentEffect.Contains("autoheal"))
        )
        {
            if (enemyBoard.enemyPrefabCards.Count == 0)
                return false;
        }

        // Targeted unit spell → requires ally board
        if (spell.CurrentEffect.Contains("targetunit"))
        {
            if (allyBoard.allyPrefabCards.Count == 0)
                return false;
        }

        // Targeted core spell → requires core (always true, but explicit)
        if (spell.CurrentEffect.Contains("targetcore"))
        {
            if (gameManager.PlayerCore == null)
                return false;
        }

        return true;
    }

    private void PlaySpell(CardInstance spell)
    {
        //Verify if there is board before playing buff spell :

        // Trigger deploy effects (spells use deploy)
        spell.OnPlaySpell();
        OnCardPlayed?.Invoke(spell);

        // Remove from hand & destroy
        enemyHand.handCards.Remove(spell.gameObject);
        Destroy(spell.gameObject);
    }
    private int EvaluateMinion(CardInstance minion)
    {
        int value = 0;

        value += minion.CurrentAttack * 2;
        value += minion.CurrentHealth;

        if (minion.HasKeyword("protect")) value += 10;
        if (minion.HasKeyword("haste")) value += 5;
        if (minion.HasKeyword("quickstrike")) value += 10;

        value -= minion.CurrentManaCost;

        return value;
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
            playable.Sort((a, b) =>
                EvaluateMinion(b).CompareTo(EvaluateMinion(a)));

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
            if (attacker == null || attacker.IsAsleep || (attacker.HasAttackedThisTurn && !attacker.HasKeyword("haste")) ||
            (attacker.HasAttackedThisTurn && attacker.HasKeyword("haste") && attacker.HasAttackedTwiceThisTurn)
                || attacker.CurrentAttack<=0)
                continue;

            var targets = gameManager.GetValidTargets(attacker);
            if (targets.Count == 0)
                continue;

            IAttackable bestTarget = ChooseBestAttackTarget(attacker);
            if (bestTarget != null)
            {
                gameManager.QueueAttack(attacker, bestTarget);
            }


            // 🔹 Wait until THIS attack finishes
            yield return new WaitUntil(() => !gameManager.IsResolvingAttackQueue());

            // 🔹 Small delay for readability
            yield return new WaitForSeconds(0.25f);
        }
    }
    private IAttackable ChooseBestAttackTarget(CardInstance attacker)
    {
        var targets = gameManager.GetValidTargets(attacker);

        IAttackable best = null;
        int bestScore = int.MinValue;

        foreach (var target in targets)
        {
            // 🔒 SUMMON-TURN HARD FILTER
            if (attacker.IsSummoningSick)
            {
                if (target is CoreInstance && !attacker.CanAttackCoreOnSummon())
                    continue;

                if (target is CardInstance && !attacker.CanAttackUnitOnSummon())
                    continue;
            }

            int score = 0;

            // CORE
            if (target is CoreInstance)
            {
                score += 50;

                if (HasLethalThisTurn())
                    score += 10000;
            }
            // UNIT
            else if (target is CardInstance unit)
            {
                if (attacker.CurrentAttack >= unit.CurrentHealth)
                    score += 60;

                if (unit.CurrentAttack > attacker.CurrentHealth)
                    score -= 10;

                if (unit.HasKeyword("haste"))
                    score += 100;

                score += unit.CurrentAttack;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = target;
            }
        }

        return best;
    }

    private bool HasLethalThisTurn()
    {
        int totalAttack = 0;

        foreach (GameObject go in enemyBoard.enemyPrefabCards)
        {
            if (go == null) continue;

            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci == null) continue;

            if (!gameManager.CanSelectAttacker(ci))
                continue;

            totalAttack += ci.CurrentAttack;
        }

        return totalAttack >= gameManager.PlayerCore.CurrentHealth;
    }

    private void EndEnemyTurn()
    {
        TurnManager.Instance.EndTurn();
    }

}
