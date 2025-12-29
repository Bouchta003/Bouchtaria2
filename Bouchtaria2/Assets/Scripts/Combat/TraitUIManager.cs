using System.Collections.Generic;
using UnityEngine;

public class TraitUIManager : MonoBehaviour
{
    private Dictionary<CardData.Trait, TraitsDisplay> displaysByTrait;

    public void DetectTraitBorder()
    {
        displaysByTrait = new Dictionary<CardData.Trait, TraitsDisplay>();

        TraitsDisplay[] displays = GetComponentsInChildren<TraitsDisplay>(true);

        foreach (var display in displays)
        {
            if (!displaysByTrait.ContainsKey(display.thisTrait))
            {
                displaysByTrait.Add(display.thisTrait, display);
            }
            else
            {
                Debug.LogWarning($"Duplicate TraitDisplay for {display.thisTrait}");
            }
        }
    }
    public TraitsDisplay GetTraitDisplay(CardData.Trait trait)
    {
        displaysByTrait.TryGetValue(trait, out TraitsDisplay display);
        return display;
    }
    public void UpdateTraitProgress(CardData.Trait trait, int progress, int currentCap)
    {
        TraitsDisplay display = GetTraitDisplay(trait);
        if (display == null)
            return;

        display.Progression = progress;
        display.CurrentCap = currentCap;
    }

    public void ActivateTrait(CardData.Trait trait, int tier)
    {
        if (displaysByTrait.TryGetValue(trait, out var display))
        {
            display.Activate(tier);
        }
        else
        {
            Debug.LogWarning($"No TraitDisplay found for {trait}");
        }
    }
}
