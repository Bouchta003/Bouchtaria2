using System.Collections.Generic;
using UnityEngine;

public static class PathOfPowerRelicEffectService
{
    private const int EmptyCoreShieldRelicId = 1;
    private const int EmptyCoreShieldAmount = 6;

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
                case EmptyCoreShieldRelicId:
                    ApplyEmptyCoreShield(owner, gameManager);
                    break;
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
            return runData.activeEnemyRelics ?? System.Array.Empty<int>();

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
            Debug.LogWarning($"[PathOfPower][Relics] Relic {EmptyCoreShieldRelicId} could not find a {owner} core.");
            return;
        }

        if (core.Shield > 0)
        {
            Debug.Log($"[PathOfPower][Relics] Relic {EmptyCoreShieldRelicId} skipped for {owner}: core already has {core.Shield} shield.");
            return;
        }

        core.AddShield(EmptyCoreShieldAmount);
        Debug.Log($"[PathOfPower][Relics] Relic {EmptyCoreShieldRelicId} gave {owner} core +{EmptyCoreShieldAmount} shield because it had no shield.");
    }
}
