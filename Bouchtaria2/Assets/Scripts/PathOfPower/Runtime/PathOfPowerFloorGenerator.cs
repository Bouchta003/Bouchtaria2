using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathOfPowerFloorGenerator
{
    private const int StepsPerFloor = 5;
    private const int WardenStep = 5;
    private const float BaseEventChance = 0.25f;
    private const float EventChanceDelta = 0.05f;

    private readonly IReadOnlyList<EventDefinition> events;
    private readonly IReadOnlyList<EnemyEncounterDefinition> encounters;

    public PathOfPowerFloorGenerator(IReadOnlyList<EventDefinition> events, IReadOnlyList<EnemyEncounterDefinition> encounters)
    {
        this.events = events ?? Array.Empty<EventDefinition>();
        this.encounters = encounters ?? Array.Empty<EnemyEncounterDefinition>();
    }

    public List<PathOfPowerStepData> GenerateFloor(int floor, PathOfPowerPathType pathType, int seed, IReadOnlyCollection<string> excludedEventIds = null, IReadOnlyDictionary<PathOfPowerStepType, IReadOnlyCollection<string>> excludedEncounterIdsByCategory = null)
    {
        System.Random rng = new System.Random(seed);
        HashSet<string> blockedEventIds = new HashSet<string>(excludedEventIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        Dictionary<PathOfPowerStepType, HashSet<string>> blockedEncounterIdsByCategory = BuildEncounterBlockLists(excludedEncounterIdsByCategory);
        List<PathOfPowerStepData> steps = new List<PathOfPowerStepData>();
        float eventChance = BaseEventChance;
        int eventCount = 0;

        for (int step = 1; step <= StepsPerFloor; step++)
        {
            if (step == WardenStep)
            {
                steps.Add(CreateStep(step, PathOfPowerStepType.Warden, floor, rng, blockedEventIds, PathOfPowerStepType.Warden, blockedEncounterIdsByCategory));
                continue;
            }

            PathOfPowerStepType stepType = pathType == PathOfPowerPathType.Challenge
                ? PathOfPowerStepType.Elite
                : PathOfPowerStepType.Fight;

            if (pathType == PathOfPowerPathType.Simple)
            {
                stepType = step == 3 ? PathOfPowerStepType.Event : PathOfPowerStepType.Fight;
            }
            else if (eventCount < 2)
            {
                bool forceEvent = step == 4 && eventCount == 0;
                bool canRollEvent = eventCount < 2;
                bool eventTriggered = forceEvent || (canRollEvent && rng.NextDouble() < eventChance);

                if (eventTriggered)
                {
                    stepType = PathOfPowerStepType.Event;
                    eventChance = Mathf.Max(0f, eventChance - EventChanceDelta);
                }
                else
                {
                    eventChance += EventChanceDelta;
                }
            }

            if (stepType == PathOfPowerStepType.Event)
                eventCount++;

            PathOfPowerStepType fallbackCombatStepType = pathType == PathOfPowerPathType.Challenge
                ? PathOfPowerStepType.Elite
                : PathOfPowerStepType.Fight;
            PathOfPowerStepData createdStep = CreateStep(step, stepType, floor, rng, blockedEventIds, fallbackCombatStepType, blockedEncounterIdsByCategory);
            if (createdStep.stepType == PathOfPowerStepType.Event)
                blockedEventIds.Add(createdStep.eventId);
            else
                BlockEncounterForThisFloor(createdStep, blockedEncounterIdsByCategory);
            if (step == 1 && createdStep.stepType != PathOfPowerStepType.Event && floor == 1)
            {
                // First Path Of Power fight in a run is fixed to encounter id 0.
                createdStep.encounterId = "0";
            }

            steps.Add(createdStep);
        }

        EnsureEventBounds(steps, floor, pathType, rng, blockedEventIds, blockedEncounterIdsByCategory);
        return steps;
    }

    private void EnsureEventBounds(
        List<PathOfPowerStepData> steps,
        int floor,
        PathOfPowerPathType pathType,
        System.Random rng,
        HashSet<string> blockedEventIds,
        IDictionary<PathOfPowerStepType, HashSet<string>> blockedEncounterIdsByCategory)
    {
        List<PathOfPowerStepData> nonWardenSteps = steps.Where(step => step.stepIndex < WardenStep).ToList();
        int eventCount = nonWardenSteps.Count(step => step.stepType == PathOfPowerStepType.Event);

        if (eventCount == 0)
        {
            PathOfPowerStepData forced = steps.First(step => step.stepIndex == 4);
            forced.stepType = PathOfPowerStepType.Event;
            string forcedEventId = PickEventId(floor, rng, blockedEventIds);
            if (!string.IsNullOrWhiteSpace(forcedEventId))
            {
                forced.eventId = forcedEventId;
                forced.encounterId = string.Empty;
                blockedEventIds.Add(forcedEventId);
                eventCount = 1;
            }
            else
            {
                forced.stepType = pathType == PathOfPowerPathType.Challenge ? PathOfPowerStepType.Elite : PathOfPowerStepType.Fight;
                forced.eventId = string.Empty;
                forced.encounterId = PickEncounterId(floor, forced.stepType, rng, blockedEncounterIdsByCategory);
                BlockEncounterForThisFloor(forced, blockedEncounterIdsByCategory);
            }
        }

        while (eventCount > 2)
        {
            PathOfPowerStepData extra = nonWardenSteps.Last(step => step.stepType == PathOfPowerStepType.Event);
            extra.stepType = pathType == PathOfPowerPathType.Challenge ? PathOfPowerStepType.Elite : PathOfPowerStepType.Fight;
            extra.eventId = string.Empty;
            extra.encounterId = PickEncounterId(floor, extra.stepType, rng, blockedEncounterIdsByCategory);
            BlockEncounterForThisFloor(extra, blockedEncounterIdsByCategory);
            eventCount--;
        }
    }

    private PathOfPowerStepData CreateStep(
        int stepIndex,
        PathOfPowerStepType stepType,
        int floor,
        System.Random rng,
        HashSet<string> blockedEventIds,
        PathOfPowerStepType fallbackCombatStepType,
        IDictionary<PathOfPowerStepType, HashSet<string>> blockedEncounterIdsByCategory)
    {
        string eventId = stepType == PathOfPowerStepType.Event ? PickEventId(floor, rng, blockedEventIds) : string.Empty;
        if (stepType == PathOfPowerStepType.Event && string.IsNullOrWhiteSpace(eventId))
            stepType = fallbackCombatStepType;

        return new PathOfPowerStepData
        {
            stepIndex = stepIndex,
            stepType = stepType,
            eventId = stepType == PathOfPowerStepType.Event ? eventId : string.Empty,
            encounterId = stepType == PathOfPowerStepType.Event ? string.Empty : PickEncounterId(floor, stepType, rng, blockedEncounterIdsByCategory),
            completed = false
        };
    }

    private void BlockEncounterForThisFloor(PathOfPowerStepData step, IDictionary<PathOfPowerStepType, HashSet<string>> blockedEncounterIdsByCategory)
    {
        if (step == null || string.IsNullOrWhiteSpace(step.encounterId) || blockedEncounterIdsByCategory == null)
            return;

        if (!blockedEncounterIdsByCategory.TryGetValue(step.stepType, out HashSet<string> blockedIds))
        {
            blockedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            blockedEncounterIdsByCategory[step.stepType] = blockedIds;
        }

        blockedIds.Add(step.encounterId);
    }

    private string PickEventId(int floor, System.Random rng, HashSet<string> blockedEventIds)
    {
        List<EventDefinition> valid = events
            .Where(evt => evt != null && evt.EnabledInFirstVersion && evt.MinimumFloor <= floor && !blockedEventIds.Contains(evt.EventId))
            .ToList();

        if (valid.Count == 0)
            return string.Empty;

        return valid[rng.Next(valid.Count)].EventId;
    }

    private Dictionary<PathOfPowerStepType, HashSet<string>> BuildEncounterBlockLists(IReadOnlyDictionary<PathOfPowerStepType, IReadOnlyCollection<string>> excludedEncounterIdsByCategory)
    {
        Dictionary<PathOfPowerStepType, HashSet<string>> result = new Dictionary<PathOfPowerStepType, HashSet<string>>();
        if (excludedEncounterIdsByCategory == null)
            return result;

        foreach (KeyValuePair<PathOfPowerStepType, IReadOnlyCollection<string>> entry in excludedEncounterIdsByCategory)
            result[entry.Key] = new HashSet<string>(entry.Value ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        return result;
    }

    private HashSet<string> GetBlockedEncounterIds(
        PathOfPowerStepType stepType,
        IDictionary<PathOfPowerStepType, HashSet<string>> blockedEncounterIdsByCategory)
    {
        if (blockedEncounterIdsByCategory == null)
            return null;

        return blockedEncounterIdsByCategory.TryGetValue(stepType, out HashSet<string> blockedIds) ? blockedIds : null;
    }

    private string PickEncounterId(
        int floor,
        PathOfPowerStepType stepType,
        System.Random rng,
        IDictionary<PathOfPowerStepType, HashSet<string>> blockedEncounterIdsByCategory)
    {
        bool wantsElite = stepType == PathOfPowerStepType.Elite;
        bool wantsWarden = stepType == PathOfPowerStepType.Warden;
        if (wantsWarden)
        {
            EnemyEncounterDefinition forcedWarden = encounters.FirstOrDefault(encounter =>
                encounter != null && encounter.Warden && encounter.SpecificFloorEncounter == floor);
            HashSet<string> blockedWardenIds = GetBlockedEncounterIds(stepType, blockedEncounterIdsByCategory);
            if (forcedWarden != null && (blockedWardenIds == null || !blockedWardenIds.Contains(forcedWarden.EncounterId)))
                return forcedWarden.EncounterId;
        }

        List<EnemyEncounterDefinition> valid = encounters
            .Where(encounter => encounter != null
                && encounter.Elite == wantsElite
                && encounter.Warden == wantsWarden
                && (!wantsWarden || encounter.SpecificFloorEncounter == 0 || encounter.SpecificFloorEncounter == floor))
            .ToList();

        if (!wantsWarden)
            valid = valid.Where(encounter => encounter.EncounterId != "0").ToList();

        if (valid.Count == 0 && wantsWarden)
        {
            valid = encounters
                .Where(encounter => encounter != null
                    && encounter.Warden)
                .ToList();
        }

        HashSet<string> blockedIds = GetBlockedEncounterIds(stepType, blockedEncounterIdsByCategory);
        List<EnemyEncounterDefinition> unblocked = valid
            .Where(encounter => blockedIds == null || !blockedIds.Contains(encounter.EncounterId))
            .ToList();

        if (unblocked.Count == 0 && wantsWarden)
        {
            valid = encounters
                .Where(encounter => encounter != null && encounter.Warden)
                .ToList();
            unblocked = valid
                .Where(encounter => blockedIds == null || !blockedIds.Contains(encounter.EncounterId))
                .ToList();
        }

        if (unblocked.Count > 0)
            valid = unblocked;

        if (valid.Count == 0)
            return string.Empty;

        return valid[rng.Next(valid.Count)].EncounterId;
    }
}
