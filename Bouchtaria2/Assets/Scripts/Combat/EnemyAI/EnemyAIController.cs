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

    // We keep action planning simple and deterministic:
    // evaluate legal combinations under mana + board constraints,
    // then execute the highest score sequence.
    private enum PlannedActionType
    {
        Spell,
        Summon
    }

    private class PlannedAction
    {
        public PlannedActionType Type;
        public CardInstance Card;
        public int Score;
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
        if (HasLethalThisTurn())
        {
            yield return StartCoroutine(TryAttack());
            EndEnemyTurn();
            yield break;
        }

        yield return new WaitForSeconds(0.3f);

        // Build a smarter turn plan that optimizes mana usage and board space,
        // instead of playing the first acceptable card.
        yield return StartCoroutine(PlayBestMainPhaseSequence());

        // Attacks
        yield return StartCoroutine(TryAttack());
        yield return StartCoroutine(TryAttack());

        // If combat created new strategic value (e.g. a post-combat summon), try once more.
        yield return StartCoroutine(PlayBestMainPhaseSequence());

        yield return new WaitForSeconds(0.2f);

        EndEnemyTurn();
    }
    private int EvaluateSpell(CardInstance spell)
    {
        int score = 0;
        string effect = spell.CurrentEffect.ToLowerInvariant();

        // Damage spells are highly valuable, and even more valuable when they can clear units.
        if (effect.Contains("damage"))
        {
            score += 40;
            if (allyBoard.allyPrefabCards.Count > 0) score += 10;
        }

        if (effect.Contains("buff"))
            score += enemyBoard.enemyPrefabCards.Count * 10;

        if (effect.Contains("heal"))
            score += 20;

        if (effect.Contains("gear"))
            score += 30;

        // If opponent board is threatening, value interaction more.
        score += EvaluateAllyBoardThreat() / 3;

        // Penalize low-impact expensive spells
        score -= spell.CurrentManaCost * 5;

        return score;
    }

    private int EvaluateAllyBoardThreat()
    {
        int threat = 0;
        foreach (GameObject allyGo in allyBoard.allyPrefabCards)
        {
            if (allyGo == null) continue;
            CardInstance ally = allyGo.GetComponent<CardInstance>();
            if (ally == null) continue;

            threat += ally.CurrentAttack * 2;
            if (ally.HasKeyword("haste")) threat += 3;
            if (ally.HasKeyword("protect")) threat += 6;
            if (ally.HasKeyword("quickstrike")) threat += 4;
        }
        return threat;
    }

    private IEnumerator PlayBestMainPhaseSequence()
    {
        List<PlannedAction> plan = BuildBestMainPhasePlan();
        if (plan.Count == 0)
            yield break;

        foreach (PlannedAction action in plan)
        {
            if (action.Card == null)
                continue;

            if (action.Type == PlannedActionType.Spell)
            {
                if (!CanEnemyActuallyCastSpell(action.Card))
                    continue;

                yield return StartCoroutine(gameManager.ShowEnemySpell(action.Card.Data));
                PlaySpell(action.Card);
            }
            else
            {
                if (enemyBoard.enemyPrefabCards.Count >= enemyBoard.maxBoardSize)
                    continue;

                if (action.Card.CurrentManaCost > gameManager.EnemyCurrentMana)
                    continue;

                Summon(action.Card.GetComponent<Card>());
            }

            yield return new WaitForSeconds(0.35f);
        }
    }

    private List<PlannedAction> BuildBestMainPhasePlan()
    {
        int mana = gameManager.EnemyCurrentMana;
        int freeSlots = enemyBoard.maxBoardSize - enemyBoard.enemyPrefabCards.Count;

        List<CardInstance> candidates = new();

        foreach (GameObject cardGO in enemyHand.handCards)
        {
            if (cardGO == null) continue;
            CardInstance inst = cardGO.GetComponent<CardInstance>();
            if (inst == null) continue;
            if (inst.CurrentManaCost > mana) continue;

            string cardType = inst.Data.cardType.ToLowerInvariant();
            if (cardType == "spell")
            {
                if (inst.CurrentEffect.Contains("monsterpart"))
                    continue;
                if (!CanEnemyActuallyCastSpell(inst))
                    continue;
                candidates.Add(inst);
            }
            else if (cardType == "minion")
            {
                if (freeSlots <= 0) continue;
                candidates.Add(inst);
            }
        }

        int bestScore = int.MinValue;
        List<PlannedAction> bestPlan = new();

        BuildBestPlanDfs(candidates, 0, mana, mana, freeSlots, new List<PlannedAction>(), 0, ref bestScore, ref bestPlan);

        // Fallback: if DFS elected to pass despite playable cards, force a mana-spending plan.
        if (bestPlan.Count == 0 && candidates.Count > 0)
        {
            bestPlan = BuildFallbackManaSpendingPlan(candidates, mana, freeSlots);
        }

        return bestPlan;
    }

    private List<PlannedAction> BuildFallbackManaSpendingPlan(List<CardInstance> candidates, int mana, int freeSlots)
    {
        List<PlannedAction> plan = new();
        List<CardInstance> remaining = new(candidates);

        while (remaining.Count > 0)
        {
            CardInstance best = null;
            int bestManaCost = -1;

            foreach (CardInstance card in remaining)
            {
                if (card == null || card.CurrentManaCost > mana)
                    continue;

                string cardType = card.Data.cardType.ToLowerInvariant();
                if (cardType == "minion" && freeSlots <= 0)
                    continue;

                if (cardType == "spell" && !CanEnemyActuallyCastSpell(card))
                    continue;

                if (card.CurrentManaCost > bestManaCost)
                {
                    best = card;
                    bestManaCost = card.CurrentManaCost;
                }
            }

            if (best == null)
                break;

            string bestType = best.Data.cardType.ToLowerInvariant();
            plan.Add(new PlannedAction
            {
                Type = bestType == "spell" ? PlannedActionType.Spell : PlannedActionType.Summon,
                Card = best,
                Score = 0
            });

            mana -= best.CurrentManaCost;
            if (bestType == "minion")
                freeSlots--;

            remaining.Remove(best);
        }

        return plan;
    }

    private void BuildBestPlanDfs(
        List<CardInstance> candidates,
        int index,
        int initialMana,
        int manaLeft,
        int freeSlots,
        List<PlannedAction> current,
        int currentScore,
        ref int bestScore,
        ref List<PlannedAction> bestPlan)
    {
        if (index >= candidates.Count)
        {
            // Strongly reward spending mana so AI doesn't pass with playable cards.
            int manaSpent = initialMana - manaLeft;
            int finalScore = currentScore + (manaSpent * 100) - (manaLeft * 2);
            if (finalScore > bestScore)
            {
                bestScore = finalScore;
                bestPlan = new List<PlannedAction>(current);
            }
            return;
        }

        CardInstance card = candidates[index];

        // Option A: skip
        BuildBestPlanDfs(candidates, index + 1, initialMana, manaLeft, freeSlots, current, currentScore, ref bestScore, ref bestPlan);

        if (card == null || card.CurrentManaCost > manaLeft)
            return;

        string cardType = card.Data.cardType.ToLowerInvariant();

        if (cardType == "spell")
        {
            if (!CanEnemyActuallyCastSpell(card))
                return;

            current.Add(new PlannedAction
            {
                Type = PlannedActionType.Spell,
                Card = card,
                Score = EvaluateSpell(card)
            });

            BuildBestPlanDfs(
                candidates,
                index + 1,
                initialMana,
                manaLeft - card.CurrentManaCost,
                freeSlots,
                current,
                currentScore + EvaluateSpell(card),
                ref bestScore,
                ref bestPlan);

            current.RemoveAt(current.Count - 1);
        }
        else if (cardType == "minion" && freeSlots > 0)
        {
            int minionScore = EvaluateMinion(card) + 4; // Base tempo value
            current.Add(new PlannedAction
            {
                Type = PlannedActionType.Summon,
                Card = card,
                Score = minionScore
            });

            BuildBestPlanDfs(
                candidates,
                index + 1,
                initialMana,
                manaLeft - card.CurrentManaCost,
                freeSlots - 1,
                current,
                currentScore + minionScore,
                ref bestScore,
                ref bestPlan);

            current.RemoveAt(current.Count - 1);
        }
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

        string effect = spell.CurrentEffect.ToLowerInvariant();

        // =====================================================================
        // AI SPELL SAFETY CONDITIONS (CUSTOMIZATION ZONE)
        // Add custom checks here whenever a specific spell type should be skipped
        // if the board/game state would make it wasteful.
        //
        // Pattern:
        // if (effect.Contains("yourkeyword") && !YourCondition())
        //     return false;
        // =====================================================================

        // Do not spend draw if hand is already almost full (requested threshold: 9+).
        if (effect.Contains("draw") && enemyHand.handCards.Count >= 9)
            return false;

        // Don't play refreshattack with no ally unit that can actually benefit.
        if (effect.Contains("refreshattack") && !HasRefreshAttackTarget())
            return false;

        // Don't spend heal if every valid friendly heal target is already at full health.
        if (effect.Contains("heal") && effect.Contains("targetunit") && !effect.Contains("autoheal") && !HasDamagedFriendlyUnit())
            return false;

        // Buff / Gear / non-auto Heal → requires enemy board
        if (
            effect.Contains("gear") ||
            effect.Contains("buff") ||
            (effect.Contains("heal") && !effect.Contains("autoheal"))
        )
        {
            if (enemyBoard.enemyPrefabCards.Count == 0)
                return false;
        }

        // Targeted unit spell → requires ally board
        if (effect.Contains("targetunit"))
        {
            bool needsEnemyUnits = effect.Contains("heal") || effect.Contains("buff") || effect.Contains("gear");
            PlayerOwner targetOwner = needsEnemyUnits ? PlayerOwner.Enemy : PlayerOwner.Player;

            List<IAttackable> validTargets = gameManager.GetValidTargets(targetOwner);
            bool hasValidUnit = false;

            foreach (IAttackable t in validTargets)
            {
                if (t is not CardInstance unit)
                    continue;

                if (effect.Contains("sleep") && unit.IsAsleep)
                    continue;

                hasValidUnit = true;
                break;
            }

            if (!hasValidUnit)
                return false;
        }

        // Targeted core spell → requires core (always true, but explicit)
        if (effect.Contains("targetcore"))
        {
            if (gameManager.PlayerCore == null)
                return false;
        }

        // Never heal at full life (board + core already full HP).
        if (effect.Contains("heal") && !HasMissingHealthOnEnemySide())
            return false;

        return true;
    }

    private bool HasMissingHealthOnEnemySide()
    {
        if (gameManager.EnemyCore != null && gameManager.EnemyCore.CurrentHealth < gameManager.EnemyCore.MaxHealth)
            return true;

        foreach (GameObject allyGo in enemyBoard.enemyPrefabCards)
        {
            if (allyGo == null)
                continue;

            CardInstance ally = allyGo.GetComponent<CardInstance>();
            if (ally == null)
                continue;

            if (ally.CurrentHealth < ally.CurrentMaxHealth)
                return true;
        }

        return false;
    }

    private bool HasDamagedFriendlyUnit()
    {
        foreach (GameObject allyGo in enemyBoard.enemyPrefabCards)
        {
            if (allyGo == null)
                continue;

            CardInstance ally = allyGo.GetComponent<CardInstance>();
            if (ally == null || ally.IsDead)
                continue;

            if (ally.CurrentHealth < ally.CurrentMaxHealth)
                return true;
        }

        return false;
    }

    private bool HasRefreshAttackTarget()
    {
        foreach (GameObject allyGo in enemyBoard.enemyPrefabCards)
        {
            if (allyGo == null)
                continue;

            CardInstance ally = allyGo.GetComponent<CardInstance>();
            if (ally == null || ally.IsDead || ally.CurrentAttack <= 0)
                continue;

            bool canStillAttackWithHaste = ally.HasKeyword("haste") && !ally.HasAttackedTwiceThisTurn;
            bool canStillAttackNormally = !ally.HasAttackedThisTurn;

            // If it can already attack now, refreshattack has no value for this unit.
            if (canStillAttackWithHaste || canStillAttackNormally)
                continue;

            return true;
        }

        return false;
    }

    private void PlaySpell(CardInstance spell)
    {
        // OnPlaySpell already handles legality resolution, mana spending,
        // and clean removal from hand. Keeping it centralized avoids illegal casts.
        spell.OnPlaySpell();
        OnCardPlayed?.Invoke(spell);
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
                score += 45;

                // If this exact hit is lethal, prioritize it heavily.
                if (attacker.CurrentAttack >= gameManager.PlayerCore.CurrentHealth)
                    score += 10000;

                if (HasLethalThisTurn())
                    score += 10000;
            }
            // UNIT
            else if (target is CardInstance unit)
            {
                // Prefer clean trades and removing high-value keywords.
                if (attacker.CurrentAttack >= unit.CurrentHealth)
                    score += 60;
                else
                    score -= 20;

                if (unit.HasKeyword("protect"))
                    score += 30;

                if (unit.HasKeyword("quickstrike"))
                    score += 18;

                if (unit.CurrentAttack > attacker.CurrentHealth)
                    score -= 10;

                if (unit.HasKeyword("haste"))
                    score += 100;

                score += unit.CurrentAttack;
                score += Mathf.Max(0, unit.CurrentAttack - unit.CurrentHealth / 2);
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
