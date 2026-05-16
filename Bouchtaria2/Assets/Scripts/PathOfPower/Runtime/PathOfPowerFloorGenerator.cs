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

    public List<PathOfPowerStepData> GenerateFloor(int floor, PathOfPowerPathType pathType, int seed)
    {
        System.Random rng = new System.Random(seed);
        List<PathOfPowerStepData> steps = new List<PathOfPowerStepData>();
        float eventChance = BaseEventChance;
        int eventCount = 0;

        for (int step = 1; step <= StepsPerFloor; step++)
        {
            if (step == WardenStep)
            {
                steps.Add(CreateStep(step, PathOfPowerStepType.Warden, floor, rng));
                continue;
            }

            PathOfPowerStepType stepType = PathOfPowerStepType.Fight;

            if (pathType == PathOfPowerPathType.Simple)
            {
                stepType = step == 3 ? PathOfPowerStepType.Event : PathOfPowerStepType.Fight;
            }
            else if (pathType == PathOfPowerPathType.Challenge && step >= 2 && rng.NextDouble() < 0.35f)
            {
                stepType = PathOfPowerStepType.Elite;
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

            steps.Add(CreateStep(step, stepType, floor, rng));
        }

        EnsureEventBounds(steps, floor, rng);
        return steps;
    }

    private void EnsureEventBounds(List<PathOfPowerStepData> steps, int floor, System.Random rng)
    {
        List<PathOfPowerStepData> nonWardenSteps = steps.Where(step => step.stepIndex < WardenStep).ToList();
        int eventCount = nonWardenSteps.Count(step => step.stepType == PathOfPowerStepType.Event);

        if (eventCount == 0)
        {
            PathOfPowerStepData forced = steps.First(step => step.stepIndex == 4);
            forced.stepType = PathOfPowerStepType.Event;
            forced.eventId = PickEventId(floor, rng);
            forced.encounterId = string.Empty;
            eventCount = 1;
        }

        while (eventCount > 2)
        {
            PathOfPowerStepData extra = nonWardenSteps.Last(step => step.stepType == PathOfPowerStepType.Event);
            extra.stepType = PathOfPowerStepType.Fight;
            extra.eventId = string.Empty;
            extra.encounterId = PickEncounterId(floor, PathOfPowerStepType.Fight, rng);
            eventCount--;
        }
    }

    private PathOfPowerStepData CreateStep(int stepIndex, PathOfPowerStepType stepType, int floor, System.Random rng)
    {
        return new PathOfPowerStepData
        {
            stepIndex = stepIndex,
            stepType = stepType,
            eventId = stepType == PathOfPowerStepType.Event ? PickEventId(floor, rng) : string.Empty,
            encounterId = stepType == PathOfPowerStepType.Event ? string.Empty : PickEncounterId(floor, stepType, rng),
            completed = false
        };
    }

    private string PickEventId(int floor, System.Random rng)
    {
        List<EventDefinition> valid = events
            .Where(evt => evt != null && evt.EnabledInFirstVersion && evt.MinimumFloor <= floor)
            .ToList();

        if (valid.Count == 0)
            return string.Empty;

        return valid[rng.Next(valid.Count)].EventId;
    }

    private string PickEncounterId(int floor, PathOfPowerStepType stepType, System.Random rng)
    {
        bool wantsElite = stepType == PathOfPowerStepType.Elite;
        bool wantsWarden = stepType == PathOfPowerStepType.Warden;
        List<EnemyEncounterDefinition> valid = encounters
            .Where(encounter => encounter != null
                && encounter.MinimumFloor <= floor
                && encounter.Elite == wantsElite
                && encounter.Warden == wantsWarden)
            .ToList();

        if (valid.Count == 0)
            return string.Empty;

        return valid[rng.Next(valid.Count)].EncounterId;
    }
}
