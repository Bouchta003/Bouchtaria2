using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAIController : MonoBehaviour
{
    private const float EffectsSettleSoftWarningSeconds = 8f;
    private const float EffectsSettleHardTimeoutSeconds = 45f;
    private const float AttackQueueSoftWarningSeconds = 8f;
    private const float AttackQueueHardTimeoutSeconds = 20f;
    private const float EndTurnPhaseTimeoutSeconds = 2f;

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

    private class AttackPlan
    {
        public IAttackable Target;
        public List<CardInstance> Attackers = new();
        public int Score;
    }

    private Coroutine activeEnemyTurnRoutine;
    private void OnEnable()
    {
        TurnManager.Instance.OnTurnStarted += HandleTurnStart;
    }

    private void OnDisable()
    {
        if (activeEnemyTurnRoutine != null)
        {
            StopCoroutine(activeEnemyTurnRoutine);
            activeEnemyTurnRoutine = null;
        }

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted -= HandleTurnStart;
    }
    private void HandleTurnStart(PlayerOwner owner)
    {
        if (owner != PlayerOwner.Enemy)
            return;

        if (activeEnemyTurnRoutine != null)
            StopCoroutine(activeEnemyTurnRoutine);

        activeEnemyTurnRoutine = StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator WaitForEffectsToSettle()
    {
        float startTime = Time.time;
        bool didWarn = false;

        while (gameManager != null && gameManager.IsResolvingEffects)
        {
            float elapsed = Time.time - startTime;

            // Soft warning for observability, but keep waiting for intentionally long effects.
            if (!didWarn && elapsed >= EffectsSettleSoftWarningSeconds)
            {
                didWarn = true;
                Debug.LogWarning($"[EnemyAI] Effects are taking longer than expected ({elapsed:F1}s, active={gameManager.ActiveEffectCount}). Still waiting.");
            }

            // Hard fail-safe to avoid infinite stalls if an effect never calls EndEffect().
            if (elapsed >= EffectsSettleHardTimeoutSeconds)
            {
                Debug.LogWarning($"[EnemyAI] Hard timeout waiting for effects ({elapsed:F1}s, active={gameManager.ActiveEffectCount}). Continuing to avoid soft-lock.");
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator WaitForEnemyMainPhase()
    {
        float timeoutAt = Time.time + EndTurnPhaseTimeoutSeconds;
        while (TurnManager.Instance != null &&
               (TurnManager.Instance.CurrentPlayer != PlayerOwner.Enemy ||
                TurnManager.Instance.CurrentPhase != TurnPhase.Main))
        {
            if (Time.time >= timeoutAt)
            {
                Debug.LogWarning("[EnemyAI] Timed out waiting for enemy main phase; continuing with fail-safe flow.");
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator WaitForAttackQueueToSettle()
    {
        float startTime = Time.time;
        bool didWarn = false;

        while (gameManager != null && gameManager.IsResolvingAttackQueue())
        {
            float elapsed = Time.time - startTime;

            if (!didWarn && elapsed >= AttackQueueSoftWarningSeconds)
            {
                didWarn = true;
                Debug.LogWarning($"[EnemyAI] Attack queue is taking longer than expected ({elapsed:F1}s). Still waiting.");
            }

            if (elapsed >= AttackQueueHardTimeoutSeconds)
            {
                Debug.LogWarning($"[EnemyAI] Hard timeout waiting for attack queue ({elapsed:F1}s). Continuing to avoid infinite loop.");
                yield break;
            }

            yield return null;
        }
    }

    private bool HasNonDeployEffect(string effectText)
    {
        if (string.IsNullOrWhiteSpace(effectText))
            return false;

        string[] triggerBlocks = effectText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (string block in triggerBlocks)
        {
            int open = block.IndexOf('[');
            if (open <= 0)
                continue;

            string triggerStr = block.Substring(0, open);
            int parenIndex = triggerStr.IndexOf('(');
            if (parenIndex > 0)
                triggerStr = triggerStr.Substring(0, parenIndex);

            // d[...] = deploy trigger, already spent once and not worth silencing now.
            if (!string.Equals(triggerStr, "d", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private bool IsWorthSilencing(CardInstance unit)
    {
        if (unit == null || unit.IsDead)
            return false;

        return HasNonDeployEffect(unit.CurrentEffect);
    }

    private IEnumerator EnemyTurnRoutine()
    {
        yield return StartCoroutine(WaitForEnemyMainPhase());
        yield return StartCoroutine(WaitForEffectsToSettle());

        if (HasLethalThisTurn())
        {
            yield return StartCoroutine(PlayBestMainPhaseSequence());
            yield return StartCoroutine(WaitForEffectsToSettle());
            yield return StartCoroutine(TryAttack());
            yield return StartCoroutine(WaitForEffectsToSettle());
            yield return StartCoroutine(EndEnemyTurnSafely());
            activeEnemyTurnRoutine = null;
            yield break;
        }

        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(WaitForEffectsToSettle());

        // Build a smarter turn plan that optimizes mana usage and board space,
        // instead of playing the first acceptable card.
        yield return StartCoroutine(PlayBestMainPhaseSequence());
        yield return StartCoroutine(WaitForEffectsToSettle());

        // Attacks
        yield return StartCoroutine(TryAttack());
        yield return StartCoroutine(WaitForEffectsToSettle());

        // Second pass only if new attackers might have appeared (e.g. post-combat summons)
        bool hasUnusedAttackers = enemyBoard.enemyPrefabCards.Any(go => {
            if (go == null) return false;
            CardInstance ci = go.GetComponent<CardInstance>();
            return ci != null && gameManager.CanSelectAttacker(ci);
        });
        if (hasUnusedAttackers)
        {
            yield return StartCoroutine(TryAttack());
            yield return StartCoroutine(WaitForEffectsToSettle());
        }
        yield return StartCoroutine(WaitForEffectsToSettle());

        // If combat created new strategic value (e.g. a post-combat summon), try once more.
        yield return StartCoroutine(PlayBestMainPhaseSequence());
        yield return StartCoroutine(WaitForEffectsToSettle());

        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(EndEnemyTurnSafely());
        activeEnemyTurnRoutine = null;
    }
    private int EvaluateSpell(CardInstance spell)
    {
        int score = 0;
        string effect = spell.CurrentEffect.ToLowerInvariant();

        // Damage — single target only (aoe handled separately below)
        if (effect.Contains("damage") && !effect.Contains("damageaoe") && !effect.Contains("damagerandomenemy"))
        {
            score += 40;
            if (allyBoard.allyPrefabCards.Count > 0) score += 10;
        }

        // Sleep — single target only (sleepall handled below)
        if (effect.Contains("sleep") && !effect.Contains("sleepall"))
            score += 20 + EvaluateAllyBoardThreat() / 4;

        // Sleepall — separate scoring
        if (effect.Contains("sleepall"))
            score += 15 + allyBoard.allyPrefabCards.Count * 12;

        if (effect.Contains("buff"))
            score += enemyBoard.enemyPrefabCards.Count * 10;

        // Prefer buffing our own 'princeloc' units when present — they are high priority targets
        int princelocCount = enemyBoard.enemyPrefabCards.Count(go => {
            if (go == null) return false;
            CardInstance ci = go.GetComponent<CardInstance>();
            return ci != null && ci.HasKeyword("princeloc");
        });
        if (princelocCount > 0 && effect.Contains("buff"))
        {
            // Increase value of buff spells when we have princeloc units to buff
            score += princelocCount * 20;
        }

        if (effect.Contains("grantall"))
        {
            int currentMinions = enemyBoard.enemyPrefabCards.Count;
            score += currentMinions * 18;
            int attackersReady = enemyBoard.enemyPrefabCards
                .Select(go => go != null ? go.GetComponent<CardInstance>() : null)
                .Count(ci => ci != null && ci.CurrentAttack > 0 && !ci.IsAsleep && !ci.HasAttackedThisTurn);
            score += attackersReady * 12;
            int remainingManaAfterCast = gameManager.EnemyCurrentMana - spell.CurrentManaCost;
            int playableMinionsBeforeGrant = CountPlayableMinionsWithinBudget(remainingManaAfterCast);
            if (playableMinionsBeforeGrant > 0)
                score -= 35 + playableMinionsBeforeGrant * 8;

            if (currentMinions <= 1)
                score -= playableMinionsBeforeGrant > 0 ? 120 : 45;
            // Grant effects also become more valuable if we can buff princeloc units
            if (princelocCount > 0)
                score += princelocCount * 18;
        }
        else if (effect.Contains("grant"))
        {
            // Grant is always positive, so aggressively value single-target grants too.
            score += enemyBoard.enemyPrefabCards.Count > 0 ? 40 : -40;
        }

        if (effect.Contains("heal"))
        {
            int missingHp = gameManager.EnemyCore.MaxHealth - gameManager.EnemyCore.CurrentHealth;
            score += Mathf.Clamp(missingHp, 5, 30);
        }

        if (effect.Contains("gear"))
            score += 30 + enemyBoard.enemyPrefabCards.Count * 5;

        // Kill / removal spells — extremely high value
        if (effect.Contains("kill") || effect.Contains("killrandom") ||
            effect.Contains("killlow") || effect.Contains("killhigh"))
            score += 50 + allyBoard.allyPrefabCards.Count * 8;

        // AOE damage — scales with how many targets exist
        if (effect.Contains("damageaoe") || effect.Contains("damagerandomenemy"))
        {
            int dmgVal = GetSingleIntFromEffectByPrefix(effect, "damageaoe");
            score += 25 + allyBoard.allyPrefabCards.Count * (dmgVal > 0 ? dmgVal : 5);
        }

        // Silence — valuable against effect-heavy boards
        if (effect.Contains("silence"))
        {
            int effectfulUnits = allyBoard.allyPrefabCards.Count(go => {
                if (go == null) return false;
                CardInstance ci = go.GetComponent<CardInstance>();
                return IsWorthSilencing(ci);
            });
            score += 20 + effectfulUnits * 10;
        }

        // Wipeboard — massive swing, scale with board size difference
        if (effect.Contains("wipeboard") || effect.Contains("scrambleallstats"))
            score += allyBoard.allyPrefabCards.Count * 20;

        // Resource denial
        if (effect.Contains("discardenemy") || effect.Contains("skipenemydraw"))
            score += 25;
        if (effect.Contains("limitenemyspace") || effect.Contains("enemymanaloss"))
            score += 20;

        // Handbuff — scales with hand size
        if (effect.Contains("handbuffall") || effect.Contains("handbuffrandom"))
            score += enemyHand.handCards.Count * 6;

        // If opponent board is threatening, value interaction more
        score += EvaluateAllyBoardThreat() / 3;

        // Penalize expensive spells proportionally
        score -= spell.CurrentManaCost * 4;

        return score;
    }
    private int CountPlayableMinionsWithinBudget(int manaBudget)
    {
        if (manaBudget <= 0 || enemyBoard.enemyPrefabCards.Count >= enemyBoard.maxBoardSize)
            return 0;

        int freeSlots = enemyBoard.maxBoardSize - enemyBoard.enemyPrefabCards.Count;
        int playable = 0;

        foreach (GameObject cardGO in enemyHand.handCards)
        {
            if (cardGO == null)
                continue;

            CardInstance inst = cardGO.GetComponent<CardInstance>();
            if (inst == null || !inst.Data.cardType.Equals("minion", System.StringComparison.OrdinalIgnoreCase))
                continue;

            if (inst.CurrentManaCost > manaBudget || !CanEnemyPlayMinion(inst))
                continue;

            playable++;
            if (playable >= freeSlots)
                break;
        }

        return playable;
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
        // Re-plan after each successful action so the enemy keeps spending mana
        // even if board state changes invalidate parts of an earlier plan.
        int safety = 0;
        while (safety++ < 20)
        {
            List<PlannedAction> plan = BuildBestMainPhasePlan();
            if (plan.Count == 0)
                yield break;

            bool playedSomething = false;

            foreach (PlannedAction action in plan)
            {
                if (action.Card == null)
                    continue;

                if (action.Type == PlannedActionType.Spell)
                {
                    if (!CanEnemyActuallyCastSpell(action.Card))
                        continue;

                    yield return StartCoroutine(gameManager.ShowEnemySpell(action.Card.Data));
                    int manaBefore = gameManager.EnemyCurrentMana;
                    int handCountBefore = enemyHand.handCards.Count;

                    PlaySpell(action.Card);
                    playedSomething = gameManager.EnemyCurrentMana < manaBefore ||
                                     enemyHand.handCards.Count < handCountBefore;
                }
                else
                {
                    if (enemyBoard.enemyPrefabCards.Count >= enemyBoard.maxBoardSize)
                        continue;

                    if (action.Card.CurrentManaCost > gameManager.EnemyCurrentMana)
                        continue;

                    if (!CanEnemyPlayMinion(action.Card))
                        continue;

                    int manaBefore = gameManager.EnemyCurrentMana;
                    int handCountBefore = enemyHand.handCards.Count;

                    Summon(action.Card.GetComponent<Card>());

                    playedSomething = gameManager.EnemyCurrentMana < manaBefore ||
                                     enemyHand.handCards.Count < handCountBefore;
                }

                if (playedSomething)
                {
                    yield return StartCoroutine(WaitForEffectsToSettle());
                    yield return new WaitForSeconds(0.35f);
                    break;
                }
            }

            if (!playedSomething)
                yield break;
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
                if (!CanEnemyPlayMinion(inst)) continue;
                candidates.Add(inst);
            }
        }

        int bestScore = int.MinValue;
        int bestManaSpent = int.MinValue;
        List<PlannedAction> bestPlan = new();

        BuildBestPlanDfs(
            candidates,
            0,
            mana,
            mana,
            freeSlots,
            new List<PlannedAction>(),
            0,
            ref bestScore,
            ref bestManaSpent,
            ref bestPlan);

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
        ref int bestManaSpent,
        ref List<PlannedAction> bestPlan)
    {
        if (index >= candidates.Count)
        {
            int manaSpent = initialMana - manaLeft;
            int finalScore = currentScore;
            // Blend score and mana spent so high-value cheap plays can beat
            // wasteful expensive ones. Weight of 3 per mana keeps efficiency
            // relevant without completely overriding card quality.
            int blended = finalScore + manaSpent * 3;
            int bestBlended = bestScore + bestManaSpent * 3;

            bool isBetterPlan =
                blended > bestBlended ||
                (blended == bestBlended && current.Count > bestPlan.Count);

            if (isBetterPlan)
            {
                bestManaSpent = manaSpent;
                bestScore = finalScore;
                bestPlan = new List<PlannedAction>(current);
            }
            return;
        }

        CardInstance card = candidates[index];

        // Option A: skip
        BuildBestPlanDfs(candidates, index + 1, initialMana, manaLeft, freeSlots, current, currentScore, ref bestScore, ref bestManaSpent, ref bestPlan);

        if (card == null || card.CurrentManaCost > manaLeft)
            return;

        string cardType = card.Data.cardType.ToLowerInvariant();
        if (cardType == "spell")
        {
            if (!CanEnemyActuallyCastSpell(card))
                return;

            int spellScore = EvaluateSpell(card); // compute once
            current.Add(new PlannedAction
            {
                Type = PlannedActionType.Spell,
                Card = card,
                Score = spellScore
            });

            BuildBestPlanDfs(
                candidates, index + 1, initialMana,
                manaLeft - card.CurrentManaCost, freeSlots,
                current, currentScore + spellScore,
                ref bestScore, ref bestManaSpent, ref bestPlan);

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
                ref bestManaSpent,
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
        return CanEnemyActuallyCastSpell(spell, skipTemporaryManaFollowupCheck: false);
    }

    private bool CanEnemyActuallyCastSpell(CardInstance spell, bool skipTemporaryManaFollowupCheck)
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

        if (effect.Contains("grantall"))
        {
            int minionCount = enemyBoard.enemyPrefabCards.Count;
            int remainingMana = gameManager.EnemyCurrentMana - spell.CurrentManaCost;
            int playableMinionsBeforeGrant = CountPlayableMinionsWithinBudget(remainingMana);

            if (minionCount <= 1 && playableMinionsBeforeGrant > 0)
                return false;
        }

        if (gameManager.ShouldBlockRandomCardPlay(spell))
            return false;

        if (EnemyHasBoardAdvantage() && (effect.Contains("wipeboard") || effect.Contains("tawakkul")))
            return false;
        // Don't silence if no player unit has a meaningful effect
        if (effect.Contains("sleepall") && allyBoard.allyPrefabCards.Count == 0)
            return false;

        // Don't silenceall if no player unit has a meaningful effect worth silencing
        if (effect.Contains("silenceall"))
        {
            bool anyEffectful = allyBoard.allyPrefabCards.Any(go => {
                if (go == null) return false;
                CardInstance ci = go.GetComponent<CardInstance>();
                return IsWorthSilencing(ci);
            });
            if (!anyEffectful) return false;
        }

        if (effect.Contains("silence") && !effect.Contains("silenceall"))
        {
            bool hasEffectfulTarget = gameManager.GetValidTargets(PlayerOwner.Player)
                .OfType<CardInstance>()
                .Any(IsWorthSilencing);
            if (!hasEffectfulTarget) return false;
        }

        // Don't sleep a unit that's already asleep
        if (effect.Contains("sleep") && !effect.Contains("sleepall"))
        {
            bool hasAwakeTarget = gameManager.GetValidTargets(PlayerOwner.Player)
                .OfType<CardInstance>()
                .Any(u => !u.IsAsleep);
            if (!hasAwakeTarget) return false;
        }

        // Don't cast kill without a valid target
        if ((effect.Contains("kill") && effect.Contains("targetunit")) &&
            !effect.Contains("killrandom") && !effect.Contains("killlow") && !effect.Contains("killhigh"))
        {
            if (allyBoard.allyPrefabCards.Count == 0) return false;
        }

        // Don't discard enemy hand if it's empty
        if (effect.Contains("discardenemy") && enemyHand.handCards.Count == 0)
            return false;

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
        // Don't summon from deck if board is full or deck has no minions to pull
        if (effect.Contains("summondeck"))
        {
            if (!gameManager.HasBoardSpaceFor(PlayerOwner.Enemy)) return false;
            if (!FindFirstObjectByType<DeckManager>().HasMinionInDeck(PlayerOwner.Enemy)) return false;
        }
        // Don't play refreshattack with no ally unit that can actually benefit.
        if (effect.Contains("refreshattack") && !HasRefreshAttackTarget())
            return false;

        // Don't cast temporary mana gain unless that mana unlocks a real follow-up play.
        if (!skipTemporaryManaFollowupCheck && ContainsTopLevelEffect(effect, "managain") && !CanUseTemporaryManaFrom(spell))
            return false;

        // Never cast resurrect effects without a dead target in graveyard.
        if (effect.Contains("resurrect") && !HasResurrectTarget())
            return false;

        // Don't spend heal if every valid friendly heal target is already at full health unless he has autheal logic.
        if (effect.Contains("heal") && effect.Contains("targetunit") && !effect.Contains("autoheal") && !HasDamagedFriendlyUnit() && !gameManager.OwnerHasTrait(PlayerOwner.Enemy, CardData.Trait.Healer, 2))
            return false;

        // Buff / Gear / non-auto Heal → requires enemy board
        if (
            effect.Contains("gear") ||
            effect.Contains("buff") ||
            effect.Contains("grant") ||
            (effect.Contains("heal") && !effect.Contains("autoheal"))
        )
        {
            bool isCoreOnlySupport = effect.Contains("targetcore") && !effect.Contains("targetunit");
            if (!isCoreOnlySupport && enemyBoard.enemyPrefabCards.Count == 0)
                return false;
        }

        // Targeted unit spell validation
        if (effect.Contains("targetunit"))
        {
            // Ditto-like morph can target ANY other board unit.
            if (effect.Contains("morphto"))
            {
                if (!HasAnyDittoTargetOnBoard())
                    return false;
            }
            else
            {
                bool needsEnemyUnits = effect.Contains("heal") || effect.Contains("buff") || effect.Contains("gear") || effect.Contains("grant");
                if (effect.StartsWith("ally?"))
                    needsEnemyUnits = true;
                PlayerOwner targetOwner = needsEnemyUnits ? PlayerOwner.Enemy : PlayerOwner.Player;

                List<IAttackable> validTargets = gameManager.GetValidTargets(targetOwner);
                bool hasValidUnit = false;

                foreach (IAttackable t in validTargets)
                {
                    if (t is not CardInstance unit)
                        continue;

                    if (effect.Contains("sleep") && unit.IsAsleep)
                        continue;

                    if (effect.Contains("catch"))
                    {
                        int catchValue = GetCatchValueFromEffect(effect);
                        if (catchValue <= 0 || unit.CurrentTotalStats >= catchValue)
                            continue;
                    }

                    hasValidUnit = true;
                    break;
                }

                if (!hasValidUnit)
                    return false;
            }
        }

        // Targeted core spell → requires core (always true, but explicit)
        if (effect.Contains("targetcore"))
        {
            CoreInstance targetCore = GetPreferredCoreTargetForSpell(effect);
            if (targetCore == null)
                return false;
        }

        // Never cast pure-heal effects at full life (but still allow mixed effects such as damagenheal).
        if (effect.Contains("heal") && !effect.Contains("damage") && !HasMissingHealthOnEnemySide())
            return false;

        return true;
    }
    private int GetSingleIntFromEffect(string effect)
    {
        int start = effect.IndexOf('(');
        int end = effect.IndexOf(')');
        if (start < 0 || end <= start + 1)
            return -1;

        return int.TryParse(effect.Substring(start + 1, end - start - 1), out int v)
            ? v
            : -1;
    }

    private int GetCatchValueFromEffect(string effect)
    {
        if (string.IsNullOrWhiteSpace(effect))
            return -1;

        foreach (string token in effect.ToLowerInvariant().Split(' '))
        {
            if (!token.StartsWith("catch"))
                continue;

            return GetSingleIntFromEffect(token);
        }

        return -1;
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

    private bool HasResurrectTarget()
    {
        return gameManager != null
            && gameManager.EnemyGraveyard != null
            && gameManager.EnemyGraveyard.Cards.Count > 0;
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

    private bool ContainsTopLevelEffect(string effect, string effectName)
    {
        if (string.IsNullOrWhiteSpace(effect) || string.IsNullOrWhiteSpace(effectName))
            return false;

        foreach (string token in effect.Split(' '))
        {
            if (token.StartsWith(effectName + "("))
                return true;
        }

        return false;
    }

    private bool CanUseTemporaryManaFrom(CardInstance sourceSpell)
    {
        if (sourceSpell == null)
            return false;

        int tempGain = GetSingleIntFromEffectByPrefix(sourceSpell.CurrentEffect, "managain");
        if (tempGain <= 0)
            return false;

        int futureMana = gameManager.EnemyCurrentMana - sourceSpell.CurrentManaCost + tempGain;

        foreach (GameObject cardGO in enemyHand.handCards)
        {
            if (cardGO == null)
                continue;

            CardInstance candidate = cardGO.GetComponent<CardInstance>();
            if (candidate == null || candidate == sourceSpell)
                continue;

            if (candidate.CurrentManaCost > futureMana)
                continue;

            string cardType = candidate.Data.cardType.ToLowerInvariant();
            // Prevent recursive managain->managain validation loops while still checking
            // normal spell legality (targets, board state, distortion world, etc.).
            if (cardType == "spell" && !CanEnemyActuallyCastSpell(candidate, skipTemporaryManaFollowupCheck: true))
                continue;

            if (cardType == "minion" && !CanEnemyPlayMinion(candidate))
                continue;

            if (cardType == "minion" && enemyBoard.enemyPrefabCards.Count >= enemyBoard.maxBoardSize)
                continue;

            return true;
        }

        return false;
    }

    private int GetSingleIntFromEffectByPrefix(string fullEffect, string prefix)
    {
        if (string.IsNullOrWhiteSpace(fullEffect) || string.IsNullOrWhiteSpace(prefix))
            return -1;

        foreach (string token in fullEffect.ToLowerInvariant().Split(' '))
        {
            if (!token.StartsWith(prefix + "("))
                continue;

            int start = token.IndexOf('(');
            int end = token.IndexOf(')');
            if (start < 0 || end <= start + 1)
                continue;

            if (int.TryParse(token.Substring(start + 1, end - start - 1), out int value))
                return value;
        }

        return -1;
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
            if (canStillAttackWithHaste || canStillAttackNormally || ally.IsAsleep)
                continue;

            return true;
        }

        return false;
    }

    private void PlaySpell(CardInstance spell)
    {
        IAttackable forcedTarget = GetForcedSpellTarget(spell);

        // OnPlaySpell already handles legality resolution, mana spending,
        // and clean removal from hand. Keeping it centralized avoids illegal casts.
        spell.OnPlaySpell(forcedTarget);
        OnCardPlayed?.Invoke(spell);
    }
    private IAttackable GetForcedSpellTarget(CardInstance spell)
    {
        bool targetsFriendly = false;
        if (spell == null || string.IsNullOrWhiteSpace(spell.CurrentEffect))
            return null;

        string effect = spell.CurrentEffect.ToLowerInvariant();

        bool hasTargetAny = effect.Contains("targetany");
        // Spells with no explicit target need no forced target
        if (!hasTargetAny && !effect.Contains("targetunit") && !effect.Contains("targetcore"))
            return null;

        if (hasTargetAny)
        {
            if (effect.Contains("damage"))
                return gameManager.PlayerCore;

            targetsFriendly = effect.Contains("buff") || effect.Contains("heal") ||
                                   effect.Contains("gear") || effect.Contains("grant");
            return targetsFriendly
                ? GetBestFriendlySpellTarget(effect)
                : GetBestHostileSpellTarget(effect);
        }

        if (effect.Contains("targetcore"))
            return GetPreferredCoreTargetForSpell(effect);

        // --- targetunit spells: pick the best unit for each effect type ---

        if (effect.Contains("catch"))
            return GetBestCatchTarget(spell.CurrentEffect);

        // Buff/heal/grant/gear target our OWN units
        targetsFriendly = effect.Contains("buff") || effect.Contains("heal") ||
                               effect.Contains("gear") || effect.Contains("grant") ||
                               effect.Contains("equipself") || effect.Contains("morphto");

        if (targetsFriendly)
            return GetBestFriendlySpellTarget(effect);

        // Everything else targets the player's units
        return GetBestHostileSpellTarget(effect);
    }

    private IAttackable GetBestFriendlySpellTarget(string effect)
    {
        CardInstance best = null;
        int bestScore = int.MinValue;

        foreach (GameObject go in enemyBoard.enemyPrefabCards)
        {
            if (go == null) continue;
            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci == null || ci.IsDead) continue;

            int score = 0;

            if (effect.Contains("heal"))
            {
                // Prefer most damaged unit
                int missingHp = ci.CurrentMaxHealth - ci.CurrentHealth;
                if (missingHp <= 0) continue;
                score += missingHp * 3;
            }

            if (effect.Contains("buff") || effect.Contains("grant") || effect.Contains("gear"))
            {
                // Prefer highest-attack units (they benefit most from buffs)
                score += ci.CurrentAttack * 2 + ci.CurrentHealth;
                // Prefer units that haven't attacked yet (buff applies this turn)
                if (!ci.HasAttackedThisTurn) score += 10;
            }

            if (effect.Contains("morphto"))
            {
                // Prefer copying the highest-stat player unit
                CardInstance bestAlly = GetHighestStatPlayerUnit();
                return bestAlly;
            }
            // Prioritize our own 'princeloc' units for buff-like effects
            if ((effect.Contains("buff") || effect.Contains("grant") || effect.Contains("gear")) && ci.HasKeyword("princeloc"))
            {
                // Strong immediate bonus to ensure princeloc units are chosen
                score += 40;
            }

            score += ci.CurrentAttack + ci.CurrentHealth;

            if (score > bestScore)
            {
                bestScore = score;
                best = ci;
            }
        }

        return best;
    }

    private CoreInstance GetPreferredCoreTargetForSpell(string effect)
    {
        bool targetsFriendlyCore = effect.Contains("heal") || effect.Contains("buff") || effect.Contains("grant") || effect.Contains("gear");
        return targetsFriendlyCore ? gameManager.EnemyCore : gameManager.PlayerCore;
    }

    private IAttackable GetBestHostileSpellTarget(string effect)
    {
        CardInstance best = null;
        int bestScore = int.MinValue;

        List<IAttackable> validTargets = gameManager.GetValidTargets(PlayerOwner.Player);

        foreach (IAttackable t in validTargets)
        {
            if (t is not CardInstance unit) continue;
            if (unit.IsDead) continue;

            int score = 0;

            if (effect.Contains("kill"))
            {
                // Kill the highest-value unit
                score += unit.CurrentAttack * 3 + unit.CurrentHealth;
                if (unit.HasKeyword("protect")) score += 10;
                if (unit.HasKeyword("haste")) score += 15;
            }
            else if (effect.Contains("silence"))
            {
                // Silence units with the most impactful effects — skip vanilla units
                if (!IsWorthSilencing(unit)) continue;
                score += unit.CurrentAttack + unit.CurrentHealth;
                if (unit.HasKeyword("protect")) score += 25;
                if (unit.HasKeyword("haste")) score += 20;
                if (unit.HasKeyword("quickstrike")) score += 15;
                if (unit.HasKeyword("regeneration")) score += 20;
                if (unit.HasKeyword("blessed")) score += 10;
            }
            else if (effect.Contains("sleep"))
            {
                if (unit.IsAsleep) continue;
                // Prioritize high-attack units we can't kill yet
                score += unit.CurrentAttack * 3;
                if (unit.HasKeyword("haste")) score += 10;
            }
            else if (effect.Contains("damage"))
            {
                // Prefer units we can finish off, or high-attack threats
                int dmgVal = GetSingleIntFromEffectByPrefix(effect, "damage");
                if (dmgVal > 0 && unit.CurrentHealth <= dmgVal) score += 50; // kills it
                score += unit.CurrentAttack * 2 + unit.CurrentHealth;
            }
            else if (effect.Contains("applybleed"))
            {
                // Prefer high-HP units (bleed hurts them more over time)
                score += unit.CurrentMaxHealth * 2;
            }
            else
            {
                score += unit.CurrentAttack + unit.CurrentHealth;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = unit;
            }
        }

        return best;
    }

    private CardInstance GetHighestStatPlayerUnit()
    {
        CardInstance best = null;
        int bestStats = int.MinValue;

        foreach (GameObject go in allyBoard.allyPrefabCards)
        {
            if (go == null) continue;
            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci == null || ci.IsDead) continue;
            int stats = ci.CurrentAttack + ci.CurrentHealth;
            if (stats > bestStats) { bestStats = stats; best = ci; }
        }

        return best;
    }

    private CardInstance GetBestCatchTarget(string effect)
    {
        int catchValue = GetCatchValueFromEffect(effect);
        if (catchValue <= 0)
            return null;

        CardInstance best = null;
        int bestStats = int.MinValue;

        List<IAttackable> validTargets = gameManager.GetValidTargets(PlayerOwner.Player);
        foreach (IAttackable target in validTargets)
        {
            if (target is not CardInstance unit)
                continue;

            if (unit.CurrentTotalStats >= catchValue)
                continue;

            if (unit.CurrentTotalStats > bestStats)
            {
                best = unit;
                bestStats = unit.CurrentTotalStats;
            }
        }

        return best;
    }
    private bool HasAnyDittoTargetOnBoard()
    {
        foreach (GameObject go in allyBoard.allyPrefabCards)
        {
            if (go == null) continue;
            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci != null && !ci.IsDead)
                return true;
        }

        foreach (GameObject go in enemyBoard.enemyPrefabCards)
        {
            if (go == null) continue;
            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci != null && !ci.IsDead)
                return true;
        }

        return false;
    }

    private int GetBoardStatsTotal(List<GameObject> cards)
    {
        int total = 0;
        foreach (GameObject go in cards)
        {
            if (go == null)
                continue;

            CardInstance unit = go.GetComponent<CardInstance>();
            if (unit == null || unit.IsDead)
                continue;

            total += unit.CurrentAttack + unit.CurrentHealth;
        }

        return total;
    }

    private bool EnemyHasBoardAdvantage()
    {
        int enemyStats = GetBoardStatsTotal(enemyBoard.enemyPrefabCards);
        int allyStats = GetBoardStatsTotal(allyBoard.allyPrefabCards);
        return enemyStats > allyStats;
    }

    private bool CanEnemyPlayMinion(CardInstance minion)
    {
        if (minion == null)
            return false;

        if (gameManager.ShouldBlockRandomCardPlay(minion))
            return false;

        string effect = minion.CurrentEffect?.ToLowerInvariant() ?? string.Empty;

        if (EnemyHasBoardAdvantage() && (effect.Contains("wipeboard") || effect.Contains("tawakkul")))
            return false;

        // Ditto/morph minions need at least one existing board unit to copy.
        if (effect.Contains("morphto") && effect.Contains("targetunit"))
            return HasAnyDittoTargetOnBoard();

        return true;
    }
    private int EvaluateMinion(CardInstance minion)
    {
        int value = 0;
        bool enemyCoreIsCritical = gameManager.EnemyCore.CurrentHealth <= 8;
        bool boardUnderPressure = EvaluateAllyBoardThreat() >= 10;

        value += minion.CurrentAttack * 3;
        value += minion.CurrentHealth;

        // Protect is extremely valuable when survival matters
        if (minion.HasKeyword("protect"))
            value += (enemyCoreIsCritical || boardUnderPressure) ? 30 : 10;

        // Haste is less relevant when we need to survive — prefer bulk
        if (minion.HasKeyword("haste"))
            value += enemyCoreIsCritical ? 2 : 5;
        // Reward units with on-strike effects — they generate value every attack
        if (minion.CurrentEffect != null && minion.CurrentEffect.Contains("s[")) value += 5;

        // High-health units are more valuable under pressure — they survive trades
        if (boardUnderPressure && minion.HasKeyword("protect"))
            value += 10; 
        if (boardUnderPressure && minion.HasKeyword("quickstrike"))
            value += 10;
        if (boardUnderPressure && minion.HasKeyword("protect") && minion.HasKeyword("blessed"))
            value += 50;
        // Fragile units played into a threatening board just die immediately
        if (boardUnderPressure && !minion.HasKeyword("protect"))
            value -= 10;
        value -= minion.CurrentManaCost;

        // Card 275 is a priority play — always score it highly
        if (!boardUnderPressure && minion.Data != null && minion.Data.id == 275)
            value += 15;

        return value;
    
    }
    private void Summon(Card card)
    {
        enemyBoard.OnCardDrop(card);
    }
    private IEnumerator TryAttack()
    {
        int safety = 0;

        while (safety++ < 20)
        {
            bool lethalThisTurn = HasLethalThisTurn();
            List<CardInstance> attackers = GetReadyAttackers(lethalThisTurn);
            if (attackers.Count == 0)
                yield break;

            AttackPlan plan = BuildBestAttackPlan(attackers, lethalThisTurn);
            if (plan == null || plan.Target == null || plan.Attackers.Count == 0)
                yield break;

            bool executedAnyAttack = false;
            foreach (CardInstance attacker in plan.Attackers.ToList())
            {
                if (attacker == null || attacker.IsDead || plan.Target == null)
                    break;

                if (plan.Target is CardInstance targetUnit && targetUnit.IsDead)
                    break;

                lethalThisTurn = HasLethalThisTurn();
                if (!CanAttackerAct(attacker, lethalThisTurn))
                    continue;

                List<IAttackable> validTargets = gameManager.GetValidTargets(attacker);
                if (!validTargets.Contains(plan.Target))
                    break;

                gameManager.QueueAttack(attacker, plan.Target);
                executedAnyAttack = true;
                yield return StartCoroutine(WaitForAttackQueueToSettle());
                yield return new WaitForSeconds(0.25f);

                if (plan.Target is CoreInstance)
                    break;
            }

            if (!executedAnyAttack)
                yield break;
        }
    }

    private List<CardInstance> GetReadyAttackers(bool lethalThisTurn)
    {
        List<CardInstance> attackers = new();

        foreach (GameObject go in enemyBoard.enemyPrefabCards)
        {
            if (go == null || !go.activeSelf)
                continue;

            CardInstance instance = go.GetComponent<CardInstance>();
            if (instance == null || !gameManager.CanSelectAttacker(instance))
                continue;

            if (!CanAttackerAct(instance, lethalThisTurn))
                continue;

            attackers.Add(instance);
        }

        return attackers;
    }

    private AttackPlan BuildBestAttackPlan(List<CardInstance> attackers, bool lethalThisTurn)
    {
        if (attackers == null || attackers.Count == 0)
            return null;

        int totalReadyAttack = attackers.Sum(a => Mathf.Max(0, a.CurrentAttack));
        int incomingDamage = EstimateIncomingPlayerBoardDamage();
        bool enemyCoreInDanger = gameManager.EnemyCore.CurrentHealth <= incomingDamage + 2;
        bool hasMajorThreat = HasMajorThreatOnDefendingBoard();

        AttackPlan bestPlan = null;

        AttackPlan corePlan = BuildCoreAttackPlan(attackers, lethalThisTurn, hasMajorThreat, enemyCoreInDanger, totalReadyAttack);
        ConsiderAttackPlan(ref bestPlan, corePlan);

        foreach (CardInstance target in GetAttackableEnemyUnits(attackers))
        {
            AttackPlan unitPlan = BuildUnitAttackPlan(target, attackers, totalReadyAttack, enemyCoreInDanger);
            ConsiderAttackPlan(ref bestPlan, unitPlan);
        }

        return bestPlan;
    }

    private void ConsiderAttackPlan(ref AttackPlan bestPlan, AttackPlan candidate)
    {
        if (candidate == null || candidate.Target == null || candidate.Attackers == null || candidate.Attackers.Count == 0)
            return;

        if (bestPlan == null || candidate.Score > bestPlan.Score)
            bestPlan = candidate;
    }

    private AttackPlan BuildCoreAttackPlan(
        List<CardInstance> attackers,
        bool lethalThisTurn,
        bool hasMajorThreat,
        bool enemyCoreInDanger,
        int totalReadyAttack)
    {
        CoreInstance playerCore = gameManager.PlayerCore;
        if (playerCore == null || playerCore.CurrentHealth <= 0)
            return null;

        List<CardInstance> coreAttackers = attackers
            .Where(a => gameManager.GetValidTargets(a).Any(t => t is CoreInstance))
            .ToList();

        if (coreAttackers.Count == 0)
            return null;

        if (lethalThisTurn)
        {
            List<CardInstance> lethalCombo = FindMinimumDamageCombination(coreAttackers, playerCore.CurrentHealth, null, false);
            if (lethalCombo.Count > 0)
            {
                int committedAttack = lethalCombo.Sum(a => a.CurrentAttack);
                return new AttackPlan
                {
                    Target = playerCore,
                    Attackers = OrderFaceAttackers(lethalCombo),
                    Score = 50000 - (committedAttack - playerCore.CurrentHealth) * 20 - lethalCombo.Count * 3
                };
            }
        }

        if (enemyCoreInDanger)
            return null;

        CardInstance bestAttacker = coreAttackers
            .OrderByDescending(a => a.CurrentAttack)
            .ThenByDescending(GetAttackerPreservationValue)
            .FirstOrDefault();

        if (bestAttacker == null)
            return null;

        int score = 55 + bestAttacker.CurrentAttack * 12;
        if (!hasMajorThreat) score += 150;
        else score -= 90;

        int remainingDamageAfterPush = totalReadyAttack - bestAttacker.CurrentAttack;
        score += remainingDamageAfterPush * 2;

        return new AttackPlan
        {
            Target = playerCore,
            Attackers = new List<CardInstance> { bestAttacker },
            Score = score
        };
    }

    private AttackPlan BuildUnitAttackPlan(CardInstance target, List<CardInstance> attackers, int totalReadyAttack, bool enemyCoreInDanger)
    {
        if (target == null || target.IsDead)
            return null;

        List<CardInstance> candidates = attackers
            .Where(a => gameManager.GetValidTargets(a).Contains(target))
            .ToList();

        if (candidates.Count == 0)
            return null;

        int threatScore = EvaluateUnitThreat(target);
        if (threatScore < 70 && !enemyCoreInDanger && !target.HasKeyword("protect"))
            return null;

        bool targetBlessed = target.HasKeyword("blessed");
        List<CardInstance> combo = FindMinimumDamageCombination(candidates, target.CurrentHealth, target, targetBlessed);

        if (combo.Count == 0)
        {
            if (!targetBlessed || threatScore < 170)
                return null;

            CardInstance popper = candidates.OrderBy(GetBlessedPopCost).FirstOrDefault();
            if (popper == null)
                return null;

            return new AttackPlan
            {
                Target = target,
                Attackers = new List<CardInstance> { popper },
                Score = threatScore * 5 - GetBlessedPopCost(popper) * 4 - 120
            };
        }

        int committedAttack = combo.Sum(a => a.CurrentAttack);
        int effectiveDamage = targetBlessed
            ? combo.Skip(1).Sum(a => a.CurrentAttack)
            : committedAttack;
        int overkill = Mathf.Max(0, effectiveDamage - target.CurrentHealth);
        int remainingDamage = Mathf.Max(0, totalReadyAttack - committedAttack);
        int attackerLossCost = combo.Sum(a => EstimateAttackerLossCost(a, target));
        bool cleanKillAvailable = combo.Any(a => a.CurrentAttack >= target.CurrentHealth && !targetBlessed);

        int score = 300 + threatScore * 9;
        score += remainingDamage * 6;
        score -= overkill * 28;
        score -= committedAttack * 3;
        score -= combo.Count * 18;
        score -= attackerLossCost;

        if (target.HasKeyword("protect")) score += 350;
        if (enemyCoreInDanger) score += target.CurrentAttack * 80;
        if (cleanKillAvailable && combo.Count == 1) score += 70;
        if (target.CurrentAttack <= 0 && !HasOngoingEffect(target)) score -= 220;

        return new AttackPlan
        {
            Target = target,
            Attackers = OrderTradeAttackers(combo, target),
            Score = score
        };
    }

    private List<CardInstance> FindMinimumDamageCombination(
        List<CardInstance> candidates,
        int requiredHealth,
        CardInstance target,
        bool targetIsBlessed)
    {
        List<CardInstance> best = new();
        int bestOverkill = int.MaxValue;
        int bestCommittedAttack = int.MaxValue;
        int bestLossCost = int.MaxValue;
        int bestPreservationCost = int.MaxValue;
        int count = candidates.Count;
        int subsetCount = 1 << count;

        for (int mask = 1; mask < subsetCount; mask++)
        {
            List<CardInstance> subset = new();
            for (int i = 0; i < count; i++)
            {
                if ((mask & (1 << i)) != 0)
                    subset.Add(candidates[i]);
            }

            if (targetIsBlessed && subset.Count < 2)
                continue;

            List<CardInstance> ordered = targetIsBlessed
                ? OrderBlessedCombo(subset, target)
                : OrderTradeAttackers(subset, target);

            int effectiveDamage = targetIsBlessed
                ? ordered.Skip(1).Sum(a => a.CurrentAttack)
                : ordered.Sum(a => a.CurrentAttack);

            if (effectiveDamage < requiredHealth)
                continue;

            int committedAttack = ordered.Sum(a => a.CurrentAttack);
            int overkill = effectiveDamage - requiredHealth;
            int lossCost = target == null ? 0 : ordered.Sum(a => EstimateAttackerLossCost(a, target));
            int preservationCost = ordered.Sum(GetAttackerPreservationValue);

            bool isBetter =
                overkill < bestOverkill ||
                (overkill == bestOverkill && committedAttack < bestCommittedAttack) ||
                (overkill == bestOverkill && committedAttack == bestCommittedAttack && subset.Count < best.Count) ||
                (overkill == bestOverkill && committedAttack == bestCommittedAttack && subset.Count == best.Count && lossCost < bestLossCost) ||
                (overkill == bestOverkill && committedAttack == bestCommittedAttack && subset.Count == best.Count && lossCost == bestLossCost && preservationCost < bestPreservationCost);

            if (isBetter)
            {
                best = ordered;
                bestOverkill = overkill;
                bestCommittedAttack = committedAttack;
                bestLossCost = lossCost;
                bestPreservationCost = preservationCost;
            }
        }

        return best;
    }

    private List<CardInstance> OrderBlessedCombo(List<CardInstance> attackers, CardInstance target)
    {
        CardInstance popper = attackers
            .OrderBy(GetBlessedPopCost)
            .ThenBy(a => a.CurrentAttack)
            .FirstOrDefault();

        List<CardInstance> ordered = new();
        if (popper != null)
            ordered.Add(popper);

        ordered.AddRange(attackers
            .Where(a => a != popper)
            .OrderByDescending(a => a.CurrentAttack)
            .ThenBy(a => EstimateAttackerLossCost(a, target)));

        return ordered;
    }

    private List<CardInstance> OrderTradeAttackers(List<CardInstance> attackers, CardInstance target)
    {
        return attackers
            .OrderByDescending(a => a.CurrentAttack)
            .ThenBy(a => EstimateAttackerLossCost(a, target))
            .ThenBy(GetAttackerPreservationValue)
            .ToList();
    }

    private List<CardInstance> OrderFaceAttackers(List<CardInstance> attackers)
    {
        return attackers
            .OrderBy(a => a.CurrentAttack)
            .ThenBy(GetAttackerPreservationValue)
            .ToList();
    }

    private List<CardInstance> GetAttackableEnemyUnits(List<CardInstance> attackers)
    {
        HashSet<CardInstance> units = new();

        foreach (CardInstance attacker in attackers)
        {
            foreach (IAttackable target in gameManager.GetValidTargets(attacker))
            {
                if (target is CardInstance unit && unit != null && !unit.IsDead && !unit.HasKeyword("hidden"))
                    units.Add(unit);
            }
        }

        return units
            .OrderByDescending(EvaluateUnitThreat)
            .ThenByDescending(u => u.CurrentAttack)
            .ToList();
    }

    private int EvaluateUnitThreat(CardInstance unit)
    {
        if (unit == null || unit.IsDead || unit.HasKeyword("hidden"))
            return 0;

        string effect = (unit.CurrentEffect ?? string.Empty).ToLowerInvariant();
        int score = unit.CurrentAttack * 14 + unit.CurrentHealth * 4;

        if (unit.HasKeyword("protect")) score += 220;
        if ((unit.HasKeyword("haste") || unit.HasKeyword("charge")) && unit.CurrentAttack >= 4) score += 150 + unit.CurrentAttack * 12;
        if (unit.CurrentAttack >= 5 && unit.CurrentHealth <= 3) score += 115;
        if (HasProgressionEffect(effect)) score += 130;
        if (HasStrikeEffect(effect)) score += 95;
        if (HasTurnCycleEffect(effect)) score += 100;
        if (unit.HasKeyword("berserk")) score += unit.CurrentHealth <= 4 ? 80 : 35;
        if (HasValueEngineEffect(effect)) score += 75;
        if (HasSummonEngineEffect(effect)) score += 70;
        if (unit.HasKeyword("blessed")) score += 55;
        if (unit.HasKeyword("quickstrike")) score += 45;

        if (IsDeployOnlyEffect(effect)) score -= 70;
        if (unit.HasKeyword("requiem") && unit.CurrentAttack + unit.CurrentHealth <= 4) score -= 45;
        if (unit.CurrentAttack <= 1 && unit.CurrentHealth >= 5 && !HasOngoingEffect(unit)) score -= 80;
        if (unit.CurrentAttack == 0 && !HasOngoingEffect(unit)) score -= 140;

        return Mathf.Max(0, score);
    }

    private bool HasProgressionEffect(string effect)
    {
        return effect.Contains("progress") || effect.Contains("progresseot") || effect.Contains("progressdamage") || effect.Contains("progressheal");
    }

    private bool HasStrikeEffect(string effect)
    {
        return effect.Contains("s[") || effect.Contains("strike");
    }

    private bool HasTurnCycleEffect(string effect)
    {
        return effect.Contains("eot[") || effect.Contains("sot[") || effect.Contains("startofturn") || effect.Contains("endofturn") || effect.Contains("starter");
    }

    private bool HasValueEngineEffect(string effect)
    {
        return effect.Contains("draw") || effect.Contains("discover") || effect.Contains("copy") || effect.Contains("addcard") || effect.Contains("mana");
    }

    private bool HasSummonEngineEffect(string effect)
    {
        return effect.Contains("summon") || effect.Contains("token") || effect.Contains("spawn") || effect.Contains("monsterpart");
    }

    private bool IsDeployOnlyEffect(string effect)
    {
        if (string.IsNullOrWhiteSpace(effect))
            return false;

        bool sawTrigger = false;
        foreach (string block in effect.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            int open = block.IndexOf('[');
            if (open <= 0)
                continue;

            sawTrigger = true;
            string trigger = block.Substring(0, open);
            int paren = trigger.IndexOf('(');
            if (paren > 0)
                trigger = trigger.Substring(0, paren);

            if (!string.Equals(trigger, "d", StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return sawTrigger;
    }

    private bool HasOngoingEffect(CardInstance unit)
    {
        if (unit == null)
            return false;

        string effect = (unit.CurrentEffect ?? string.Empty).ToLowerInvariant();
        return HasNonDeployEffect(effect) ||
               HasProgressionEffect(effect) ||
               HasStrikeEffect(effect) ||
               HasTurnCycleEffect(effect) ||
               HasValueEngineEffect(effect) ||
               HasSummonEngineEffect(effect) ||
               unit.HasKeyword("protect") ||
               unit.HasKeyword("berserk") ||
               unit.HasKeyword("quickstrike") ||
               unit.HasKeyword("blessed");
    }

    private int EstimateAttackerLossCost(CardInstance attacker, CardInstance defender)
    {
        if (attacker == null || defender == null)
            return 0;

        int defenderDamage = defender.CurrentAttack;

        if (defenderDamage < attacker.CurrentHealth)
            return 0;

        return GetAttackerPreservationValue(attacker) + Mathf.Max(0, attacker.CurrentAttack - defender.CurrentHealth) * 2;
    }

    private int GetAttackerPreservationValue(CardInstance attacker)
    {
        if (attacker == null)
            return 0;

        int value = attacker.CurrentAttack * 12 + attacker.CurrentHealth * 3;
        if (attacker.HasKeyword("haste") || attacker.HasKeyword("charge")) value += 35;
        if (attacker.HasKeyword("quickstrike")) value += 30;
        if (attacker.HasKeyword("lifesteal")) value += 25;
        if ((attacker.CurrentEffect ?? string.Empty).ToLowerInvariant().Contains("s[")) value += 20;
        if (attacker.CurrentAttack >= 6 && attacker.CurrentHealth <= 3) value += 45;
        return value;
    }

    private int GetBlessedPopCost(CardInstance attacker)
    {
        if (attacker == null)
            return int.MaxValue;

        int cost = GetAttackerPreservationValue(attacker) + attacker.CurrentAttack * 18;
        if (attacker.CurrentAttack <= 1) cost -= 35;
        if (attacker.CurrentHealth <= 1) cost -= 15;
        if (attacker.CurrentAttack >= 6 && attacker.CurrentHealth <= 3) cost += 90;
        return cost;
    }

    private bool CanAttackerAct(CardInstance attacker, bool lethalThisTurn)
    {
        if (attacker == null || attacker.IsAsleep || attacker.CurrentAttack <= 0)
            return false;

        if (attacker.HasAttackedThisTurn && !attacker.HasKeyword("haste"))
            return false;

        if (attacker.HasAttackedThisTurn && attacker.HasKeyword("haste") && attacker.HasAttackedTwiceThisTurn)
            return false;

        // Keep "starter" units defensive unless this turn is lethal.
        if (!lethalThisTurn && (attacker.CurrentEffect ?? string.Empty).ToLowerInvariant().Contains("starter"))
            return false;

        return true;
    }

    private bool HasMajorThreatOnDefendingBoard()
    {
        foreach (GameObject go in allyBoard.allyPrefabCards)
        {
            if (go == null)
                continue;

            CardInstance unit = go.GetComponent<CardInstance>();
            if (unit == null || unit.IsDead)
                continue;

            if (EvaluateUnitThreat(unit) >= 120)
                return true;
        }

        return false;
    }

    private int EstimateIncomingPlayerBoardDamage()
    {
        int damage = 0;
        foreach (GameObject go in allyBoard.allyPrefabCards)
        {
            if (go == null)
                continue;

            CardInstance unit = go.GetComponent<CardInstance>();
            if (unit == null || unit.IsDead || unit.IsAsleep || unit.CurrentAttack <= 0)
                continue;

            damage += unit.CurrentAttack;
        }

        return damage;
    }

    private IAttackable ChooseBestAttackTarget(CardInstance attacker)
    {
        AttackPlan plan = BuildBestAttackPlan(new List<CardInstance> { attacker }, HasLethalThisTurn());
        return plan?.Target;
    }
    private bool HasLethalThisTurn()
    {
        int spellDamage = EstimatePlayableCoreSpellDamageThisTurn();
        if (spellDamage >= gameManager.PlayerCore.CurrentHealth)
            return true;

        // If any enemy unit (from AI's perspective = player's board) has protect,
        // the player core cannot be targeted — no lethal possible this turn.
        bool coreIsBlocked = allyBoard.allyPrefabCards.Any(go =>
        {
            if (go == null) return false;
            CardInstance ci = go.GetComponent<CardInstance>();
            return ci != null && !ci.IsDead && !ci.HasKeyword("hidden") && ci.HasKeyword("protect");
        });
        if (coreIsBlocked) return false;

        int totalAttack = 0;
        foreach (GameObject go in enemyBoard.enemyPrefabCards)
        {
            if (go == null) continue;
            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci == null) continue;
            if (!gameManager.CanSelectAttacker(ci)) continue;
            totalAttack += ci.CurrentAttack;
        }
        return totalAttack + spellDamage >= gameManager.PlayerCore.CurrentHealth;
    }

    private int EstimatePlayableCoreSpellDamageThisTurn()
    {
        int manaLeft = gameManager.EnemyCurrentMana;
        int totalDamage = 0;

        List<CardInstance> damageSpells = new();
        foreach (GameObject cardGO in enemyHand.handCards)
        {
            if (cardGO == null)
                continue;

            CardInstance inst = cardGO.GetComponent<CardInstance>();
            if (inst == null || inst.Data.cardType.ToLowerInvariant() != "spell")
                continue;

            string effect = inst.CurrentEffect?.ToLowerInvariant() ?? string.Empty;
            if (!effect.Contains("damage"))
                continue;

            bool canHitCore = effect.Contains("targetcore") || effect.Contains("targetany");
            if (!canHitCore)
                continue;

            if (!CanEnemyActuallyCastSpell(inst))
                continue;

            int dmg = GetSingleIntFromEffectByPrefix(effect, "damage");
            if (dmg <= 0)
                continue;

            damageSpells.Add(inst);
        }

        foreach (CardInstance spell in damageSpells.OrderBy(ci => ci.CurrentManaCost))
        {
            if (spell.CurrentManaCost > manaLeft)
                continue;

            manaLeft -= spell.CurrentManaCost;
            totalDamage += GetSingleIntFromEffectByPrefix(spell.CurrentEffect.ToLowerInvariant(), "damage");
        }

        return totalDamage;
    }
    private IEnumerator EndEnemyTurnSafely()
    {
        if (TurnManager.Instance == null)
            yield break;

        float timeoutAt = Time.time + EndTurnPhaseTimeoutSeconds;
        while (TurnManager.Instance.CurrentPlayer == PlayerOwner.Enemy &&
               TurnManager.Instance.CurrentPhase != TurnPhase.Main)
        {
            if (Time.time >= timeoutAt)
            {
                Debug.LogWarning("[EnemyAI] Could not reach enemy main phase before ending turn. Forcing a best-effort EndTurn call.");
                break;
            }

            yield return null;
        }

        TurnManager.Instance.EndTurn();
        activeEnemyTurnRoutine = null;
    }

}
