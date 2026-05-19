using System.Collections.Generic;
using UnityEngine;

public static class PathOfPowerRelicEffectService
{
    // Relic IDs
    private const int BoundPhylactery = 0;
    private const int Mango = 1;
    private const int Orichalcum = 2;
    private const int RegalPillow = 3;

    private const int EmptyCoreShieldAmount = 6;

    public static void ApplyCombatStartRelics(PlayerOwner owner, GameManager gameManager)
    {
        if (!GameRunContext.IsPathOfPowerRun || gameManager == null)
            return;

        IReadOnlyList<int> relicIds = GetRelicIdsForOwner(owner, GameRunContext.PathOfPowerData);
        Debug.Log($"[PathOfPower][Relics] Checking {owner} combat-start relics: [{string.Join(", ", relicIds)}].");

        foreach (int relicId in relicIds)
        {
            switch (relicId)
            {
                // TODO(Path Of Power Relics): Add relic id + combat-start effect here.
                default:
                    break;
            }
        }
    }

    public static void ApplyTurnStartRelics(PlayerOwner owner, GameManager gameManager)
    {
        if (!GameRunContext.IsPathOfPowerRun || gameManager == null)
            return;

        IReadOnlyList<int> relicIds = GetRelicIdsForOwner(owner, GameRunContext.PathOfPowerData);
        Debug.Log($"[PathOfPower][Relics] Checking {owner} turn-start relics: [{string.Join(", ", relicIds)}].");

        foreach (int relicId in relicIds)
        {
            switch (relicId)
            {
                // TODO(Path Of Power Relics): Add relic id + turn-start effect here.
                default:
                    break;
            }
        }
    }

    public static int GetStartingHpModifier(PlayerOwner owner)
    {
        if (!GameRunContext.IsPathOfPowerRun)
            return 0;

        IReadOnlyList<int> relicIds = GetRelicIdsForOwner(owner, GameRunContext.PathOfPowerData);
        int bonusHp = 0;

        foreach (int relicId in relicIds)
        {
            switch (relicId)
            {
                // TODO(Path Of Power Relics): Add relic id + starting HP modifier here.
                // Example: case Mango: bonusHp += 10; break;
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
                // TODO(Path Of Power Relics): Add relic id + end-turn effect here.
                default:
                    Debug.Log($"[PathOfPower][Relics] Relic id {relicId} has no implemented end-turn effect yet.");
                    break;
            }
        }
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
}
