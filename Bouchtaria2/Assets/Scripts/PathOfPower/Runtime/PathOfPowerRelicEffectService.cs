using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

public static class PathOfPowerRelicEffectService
{
    // Relic IDs
    private const int BoundPhylactery = 0;
    private const int Mango = 1;
    private const int Orichalcum = 2;
    private const int RegalPillow = 3;
    private const int RingOfSnakes = 4;
    private const int Strawberry = 5;
    private const int BookOfMastery = 9;
    private const int SliferWish = 10;
    private const int BookOfPolyvalence = 11;
    private const int CheaterScroll = 12;
    private const int VengefulSpirit = 13;
    private const int JuiceHat = 14;
    private const int CardSleeve = 15;
    private const int ForgottenSoul = 16;
    private const int SelfishShellfish = 17;
    private const int GolemCapsule = 18;
    private const int BouchtasGift = 20;
    private const int BurningBlood = 21;
    private const int BloodVial = 22;
    private const int ElementalOrb = 23;
    private const int HotCocoa = 24;
    private const int EchoCrystal = 25;
    private const int RainbowRing = 26;
    private const int LittleShield = 29;
    private const int KingsCompass = 30;
    private const int RedMask = 31;
    private const int SecondChance = 32;
    private const int CursedManaGem = 33;

    private const int EmptyCoreShieldAmount = 6;
    private const int CardSleeveMaxHandSize = 7;

    public static void ApplyCombatStartRelics(PlayerOwner owner)
    {
        if (!GameRunContext.IsPathOfPowerRun || GameManager.Instance == null)
            return;

        IReadOnlyList<int> relicIds = GetRelicIdsForOwner(owner, GameRunContext.PathOfPowerData);
        Debug.Log($"[PathOfPower][Relics] Checking {owner} combat-start relics: [{string.Join(", ", relicIds)}].");
        ApplyHandSizeRelics(owner, relicIds);

        foreach (int relicId in relicIds)
        {
            switch (relicId)
            {
                case RingOfSnakes:
                    Debug.Log("[PathOfPower][Relics] Applied combat-start effect of Ring of Snakes.");
                    break;
                case GolemCapsule:
                    GameManager.Instance.AddCardToHand(owner, 77);
                    break;
                case EchoCrystal:
                    GameManager.Instance.deckManager?.TryDrawLowestCostCard(owner);
                    break;
                case CursedManaGem:
                    GameManager.Instance.GainMaxMana(1, owner);
                    break;
                default:
                    break;
            }
        }
    }

    public static void ApplyTurnStartRelics(PlayerOwner owner)
    {
        if (!GameRunContext.IsPathOfPowerRun || GameManager.Instance == null)
            return;

        IReadOnlyList<int> relicIds = GetRelicIdsForOwner(owner, GameRunContext.PathOfPowerData);
        Debug.Log($"[PathOfPower][Relics] Checking {owner} turn-start relics: [{string.Join(", ", relicIds)}].");

        foreach (int relicId in relicIds)
        {
            switch (relicId)
            {
                // TODO(Path Of Power Relics): Add relic id + turn-start effect here.
                case 0:
                    if (!GameManager.Instance.GetBoardForOwner(owner).BoardHasEffect("titos"))
                    {
                        GameManager.Instance.TrySummonForOwner(owner,411);//Summon titos if not already on board
                    }
                    else
                    {
                        GameManager.Instance.BuffAllAlliesEffect(1,1, owner, "titos");
                    }
                    break;
                case JuiceHat:
                    GameManager.Instance.AddCardToHand(owner, 62);
                    break;
                case ForgottenSoul:
                    GameManager.Instance.SetSouls(owner, GameManager.Instance.GetSouls(owner) + 1);
                    break;
                default:
                    break;
            }
        }
    }

    public static int GetStartingHpModifier(PlayerOwner owner)
    {
        if (!GameRunContext.IsPathOfPowerRun)
            return 0;

        PathOfPowerRunData runData = GameRunContext.PathOfPowerData;
        IReadOnlyList<int> relicIds = GetRelicIdsForOwner(owner, runData);
        if (owner == PlayerOwner.Player && runData != null && runData.maxHpFixedAt20)
            return -10;

        int bonusHp = owner == PlayerOwner.Player && runData != null ? runData.maxHpModifier : 0;

        foreach (int relicId in relicIds)
        {
            switch (relicId)
            {
                // TODO(Path Of Power Relics): Add relic id + starting HP modifier here.
                // Example: case Mango: bonusHp += 10; break;
                case Mango: 
                    bonusHp += 20;
                    Debug.Log($"[PathOfPower][Relics] Relic {Mango} (Mango) gives +20 starting HP for {owner}.");
                    break;
                case Strawberry:
                    bonusHp += 10;
                    Debug.Log($"[PathOfPower][Relics] Relic {Strawberry} (Strawberry) gives +10 starting HP for {owner}.");
                    break;
                case VengefulSpirit:
                    bonusHp += 15;
                    break;
                default:
                    break;
            }
        }

        return bonusHp;
    }

    public static void ApplyEndTurnRelics(PlayerOwner owner, GameManager gameManager)
    {
        if (!GameRunContext.IsPathOfPowerRun)
            return;

        if (gameManager == null)
        {
            Debug.LogWarning($"[PathOfPower][Relics] Cannot apply end-turn relics for {owner} because GameManager is missing.");
            return;
        }

        IReadOnlyList<int> relicIds = GetRelicIdsForOwner(owner, GameRunContext.PathOfPowerData);
        Debug.Log($"[PathOfPower][Relics] Checking {owner} end-turn relics: [{string.Join(", ", relicIds)}].");

        foreach (int relicId in relicIds)
        {
            switch (relicId)
            {
                case Orichalcum:
                    ApplyEmptyCoreShield(owner, gameManager);
                    Debug.Log("[PathOfPower][Relics] Applied end-turn effect of Orichalcum.");
                    break;
                case CardSleeve:
                    HandManager hand = owner == PlayerOwner.Player ? gameManager.allyHand : gameManager.enemyHand;
                    int cardsInHand = hand != null ? hand.handCards.Count : 0;
                    CoreInstance core = owner == PlayerOwner.Player ? gameManager.PlayerCore : gameManager.EnemyCore;
                    core?.AddShield(cardsInHand);
                    break;
                case ElementalOrb:
                    gameManager.Praise(owner);
                    break;
                case RainbowRing:
                    ApplyRainbowRing(owner, gameManager);
                    break;
                case RedMask:
                    ApplyRedMask(owner, gameManager);
                    break;
                // TODO(Path Of Power Relics): Add relic id + end-turn effect here.
                default:
                    Debug.Log($"[PathOfPower][Relics] Relic id {relicId} has no implemented end-turn effect yet.");
                    break;
            }
        }
    }

    private static void ApplyRedMask(PlayerOwner owner, GameManager gameManager)
    {
        List<GameObject> allies = owner == PlayerOwner.Player
            ? gameManager.allyDropArea?.GetCards()
            : gameManager.enemyDropArea?.GetCards();

        if (allies == null)
            return;

        foreach (GameObject allyObject in allies)
        {
            CardInstance ally = allyObject != null ? allyObject.GetComponent<CardInstance>() : null;
            if (ally == null || ally.IsDead)
                continue;

            if ((ally.CurrentAttack + ally.CurrentHealth) % 2 == 0)
                ally.ModifyStats(1, 0);
        }
    }

    public static void ApplyTraitTierActivatedRelics(PlayerOwner owner, int tier, GameManager gameManager)
    {
        if (!GameRunContext.IsPathOfPowerRun || tier != 3 || gameManager == null || !PlayerHasRelic(owner, KingsCompass))
            return;

        gameManager.GainMana(3, owner);
        if (gameManager.deckManager != null)
            gameManager.StartCoroutine(gameManager.deckManager.Draw(1, owner));
    }

    private static void ApplyRainbowRing(PlayerOwner owner, GameManager gameManager)
    {
        if (gameManager?.deckManager == null)
            return;

        Dictionary<CardData.Trait, int> traits = owner == PlayerOwner.Player
            ? gameManager.deckManager.AllyTraitsUnlockable
            : gameManager.deckManager.EnemyTraitsUnlockable;

        int detectedTraitCount = traits != null ? traits.Count(pair => pair.Key != CardData.Trait.None && pair.Value > 0) : 0;
        if (detectedTraitCount < 3)
            return;

        HandManager hand = owner == PlayerOwner.Player ? gameManager.allyHand : gameManager.enemyHand;
        if (hand == null || hand.handCards == null || hand.handCards.Count == 0)
            return;

        List<CardInstance> cards = hand.handCards
            .Select(cardObject => cardObject != null ? cardObject.GetComponent<CardInstance>() : null)
            .Where(card => card != null)
            .ToList();
        if (cards.Count == 0)
            return;

        CardInstance selected = cards[UnityEngine.Random.Range(0, cards.Count)];
        selected.AddTemporaryManaModifier(-1);
        selected.GetComponent<CardView>()?.UpdateMode();
        Debug.Log($"[PathOfPower][Relics] Rainbow Ring discounted {selected.Data?.name ?? "a card"} for {owner}.");
    }

    private static void ApplyHandSizeRelics(PlayerOwner owner, IReadOnlyList<int> relicIds)
    {
        if (relicIds == null || !relicIds.Contains(CardSleeve) || GameManager.Instance == null)
            return;

        HandManager hand = owner == PlayerOwner.Player ? GameManager.Instance.allyHand : GameManager.Instance.enemyHand;
        if (hand == null)
            return;

        hand.maxHandSize = hand.maxHandSize > 0 ? Mathf.Min(hand.maxHandSize, CardSleeveMaxHandSize) : CardSleeveMaxHandSize;
        hand.UpdateCardPositions();
        Debug.Log($"[PathOfPower][Relics] Card Sleeve limits {owner} hand size to {CardSleeveMaxHandSize}.");
    }

    private static IReadOnlyList<int> GetRelicIdsForOwner(PlayerOwner owner, PathOfPowerRunData runData)
    {
        if (runData == null)
            return System.Array.Empty<int>();

        if (owner == PlayerOwner.Enemy)
            return runData.activeEnemyRelics ?? (IReadOnlyList<int>)System.Array.Empty<int>();

        List<int> parsedPlayerRelics = new List<int>();
        if (runData.currentRelics == null)
            return parsedPlayerRelics;

        foreach (string relicId in runData.currentRelics)
        {
            if (int.TryParse(relicId, out int parsedId))
                parsedPlayerRelics.Add(parsedId);
            else
                Debug.Log($"[PathOfPower][Relics] Player relic id '{relicId}' is not numeric, so numeric relic effects ignore it for now.");
        }

        return parsedPlayerRelics;
    }

    private static void ApplyEmptyCoreShield(PlayerOwner owner, GameManager gameManager)
    {
        CoreInstance core = owner == PlayerOwner.Player ? gameManager.PlayerCore : gameManager.EnemyCore;
        if (core == null)
        {
            Debug.LogWarning($"[PathOfPower][Relics] Relic {Orichalcum} could not find a {owner} core.");
            return;
        }

        if (core.Shield > 0)
        {
            Debug.Log($"[PathOfPower][Relics] Relic {Orichalcum} skipped for {owner}: core already has {core.Shield} shield.");
            return;
        }

        core.AddShield(EmptyCoreShieldAmount);
        Debug.Log($"[PathOfPower][Relics] Relic {Orichalcum} gave {owner} core +{EmptyCoreShieldAmount} shield because it had no shield.");
    }

    public static bool PlayerHasRelic(PlayerOwner owner, int id)
    {
        IReadOnlyList<int> relicIds = GetRelicIdsForOwner(owner, GameRunContext.PathOfPowerData);
        foreach(int relicId in relicIds)
        {
            if (relicId == id)
            {
                return true;
            }
        }
        return false;
    }

    private static IEnumerator DrawAfterInitialStart(PlayerOwner owner, int count, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (GameManager.Instance != null && GameManager.Instance.deckManager != null)
            yield return GameManager.Instance.deckManager.Draw(count, owner);
    }

    public static int GetStartOfCombatDrawBonus(PlayerOwner owner)
    {
        int drawBonus = PlayerHasRelic(owner, RingOfSnakes) ? 2 : 0;
        if (PlayerHasRelic(owner, SelfishShellfish) && GameRunContext.PathOfPowerData?.CurrentStepData?.stepType == PathOfPowerStepType.Warden)
            drawBonus += 2;
        return drawBonus;
    }

    public static int GetGlobalEnemyAttackBonus(PlayerOwner owner)
    {
        return owner == PlayerOwner.Enemy && PlayerHasRelic(PlayerOwner.Player, VengefulSpirit) ? 1 : 0;
    }

    public static int GetGlobalEnemyHealthBonus(PlayerOwner owner)
    {
        return owner == PlayerOwner.Enemy && PlayerHasRelic(PlayerOwner.Player, CursedManaGem) ? 1 : 0;
    }

    public static bool HasBloodVial(PlayerOwner owner)
    {
        return GameRunContext.IsPathOfPowerRun && PlayerHasRelic(owner, BloodVial);
    }

    public static bool HasHotCocoa(PlayerOwner owner)
    {
        return GameRunContext.IsPathOfPowerRun && PlayerHasRelic(owner, HotCocoa);
    }

    public static bool TryPreventCoreDeath(PlayerOwner owner, CoreInstance core)
    {
        if (!GameRunContext.IsPathOfPowerRun || owner != PlayerOwner.Player || core == null)
            return false;

        PathOfPowerRunData runData = GameRunContext.PathOfPowerData;
        if (runData == null || runData.secondChanceConsumed || runData.currentRelics == null || !runData.currentRelics.Contains(SecondChance.ToString()))
            return false;

        runData.secondChanceConsumed = true;
        runData.currentRelics.Remove(SecondChance.ToString());
        core.PreventDeathWithShield(30);
        CombatLog.Instance?.AddAnonymousAction(owner, "Second Chance prevented death and granted 30 shield.");
        PathOfPowerSaveService.Save(runData);
        return true;
    }

    public static void ApplyUnitKilledRelics(CardInstance deadCard, GameManager gameManager)
    {
        if (!GameRunContext.IsPathOfPowerRun || deadCard == null || gameManager == null)
            return;

        PlayerOwner deadCardOwner = deadCard.Owner;
        if (PlayerHasRelic(deadCardOwner, LittleShield))
        {
            CoreInstance ownerCore = deadCardOwner == PlayerOwner.Player ? gameManager.PlayerCore : gameManager.EnemyCore;
            ownerCore?.AddShield(2);
        }

        PlayerOwner killerOwner = deadCard.Owner == PlayerOwner.Player ? PlayerOwner.Enemy : PlayerOwner.Player;
        if (PlayerHasRelic(killerOwner, BurningBlood))
        {
            CoreInstance killerCore = killerOwner == PlayerOwner.Player ? gameManager.PlayerCore : gameManager.EnemyCore;
            killerCore?.Heal(2);
        }
    }
}
