using UnityEngine;

public static class PathOfPowerCombatService
{
    public static int GetEnemyHealth(PathOfPowerRunData runData)
    {
        int floor = Mathf.Max(1, runData?.currentFloor ?? 1);
        int hp = 20 + ((floor - 1) * 5);
        hp = Mathf.Min(80, hp);

        PathOfPowerStepData step = runData?.CurrentStepData;
        if (step != null && step.stepType == PathOfPowerStepType.Warden)
            hp += 20;

        return hp;
    }

    public static int GetPlayerHealth(PathOfPowerRunData runData)
    {
        // First foundation: use the same 30 HP player baseline as dungeon/adventure combat.
        // TODO(Path Of Power): map relic effects to player HP/mana/draw modifiers here.
        return 30;
    }

    public static void MarkCombatLost()
    {
        PathOfPowerRunData data = GameRunContext.PathOfPowerData;
        if (data == null)
            return;

        data.combatActive = false;
        data.activeEnemyRelics?.Clear();
        data.phase = PathOfPowerRunPhase.Defeated;
        PathOfPowerSaveService.Save(data);
    }

    public static void MarkCombatWon()
    {
        PathOfPowerRunData data = GameRunContext.PathOfPowerData;
        if (data == null)
            return;

        PathOfPowerStepData step = data.CurrentStepData;
        if (step != null)
            step.completed = true;

        data.combatActive = false;
        data.activeEnemyRelics?.Clear();
        data.currentStreak++;

        if (step != null && step.stepType == PathOfPowerStepType.Warden)
        {
            data.phase = PathOfPowerRunPhase.AwaitingWardenReward;
        }
        else if (data.currentStep < 5)
        {
            data.currentStep++;
            data.phase = PathOfPowerRunPhase.Lobby;
        }
        else
        {
            data.phase = PathOfPowerRunPhase.AwaitingWardenReward;
        }

        PathOfPowerSaveService.Save(data);
    }
}
