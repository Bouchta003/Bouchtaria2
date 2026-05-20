using System;
using System.Collections.Generic;

public enum PathOfPowerStepType
{
    Fight,
    Event,
    Elite,
    Warden
}

public enum PathOfPowerPathType
{
    Simple,
    Normal,
    Challenge
}

public enum PathOfPowerRunPhase
{
    None,
    StarterRelicChoice,
    StartingDeckDiscovery,
    Lobby,
    PathSelection,
    Combat,
    Event,
    AwaitingWardenReward,
    Completed,
    Defeated
}

[Serializable]
public class PathOfPowerStepData
{
    public int stepIndex;
    public PathOfPowerStepType stepType;
    public string eventId;
    public string encounterId;
    public bool completed;
}

[Serializable]
public class PathOfPowerRunData
{
    public int currentFloor = 1;
    public int currentStep = 1;
    public List<int> currentDeck = new List<int>();
    public List<string> currentRelics = new List<string>();
    public int currentFloorSeed;
    public PathOfPowerPathType currentPathType = PathOfPowerPathType.Simple;
    public int currentStreak;
    public PathOfPowerRunPhase phase = PathOfPowerRunPhase.None;
    public List<PathOfPowerStepData> currentFloorSteps = new List<PathOfPowerStepData>();
    public List<string> pendingStarterRelicChoices = new List<string>();
    public List<string> pendingWardenRelicRewards = new List<string>();
    public List<int> pendingCardChoices = new List<int>();
    public List<int> activeEnemyRelics = new List<int>();
    public List<string> activeEnemyRelicTexts = new List<string>();
    public CardData.Trait starterDeckTrait = CardData.Trait.Neutral;
    public bool combatActive;
    public int currentDeckSize = 20;

    public PathOfPowerStepData CurrentStepData
    {
        get
        {
            if (currentFloorSteps == null)
                return null;

            return currentFloorSteps.Find(step => step.stepIndex == currentStep);
        }
    }

    public void Reset()
    {
        currentFloor = 1;
        currentStep = 1;
        currentDeck ??= new List<int>();
        currentRelics ??= new List<string>();
        currentFloorSteps ??= new List<PathOfPowerStepData>();
        pendingStarterRelicChoices ??= new List<string>();
        pendingWardenRelicRewards ??= new List<string>();
        pendingCardChoices ??= new List<int>();
        activeEnemyRelics ??= new List<int>();
        activeEnemyRelicTexts ??= new List<string>();

        currentDeck.Clear();
        currentRelics.Clear();
        currentFloorSteps.Clear();
        pendingStarterRelicChoices.Clear();
        pendingWardenRelicRewards.Clear();
        pendingCardChoices.Clear();
        activeEnemyRelics.Clear();
        activeEnemyRelicTexts.Clear();
        currentFloorSeed = 0;
        currentPathType = PathOfPowerPathType.Simple;
        currentStreak = 0;
        starterDeckTrait = CardData.Trait.Neutral;
        phase = PathOfPowerRunPhase.None;
        combatActive = false;
        currentDeckSize = 20;
    }
}
